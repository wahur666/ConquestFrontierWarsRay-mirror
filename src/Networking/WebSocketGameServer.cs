using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Networking;

public sealed class WebSocketGameServer : IAsyncDisposable
{
    private const string WebSocketGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

    private readonly ConcurrentDictionary<Guid, WebSocketClientConnection> _clients = new();
    private readonly string _logPath;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly TcpListener _listener;
    private Task? _acceptLoopTask;
    private bool _clientShutdownStarted;

    public WebSocketGameServer(WebSocketGameServerOptions options)
    {
        Options = options;
        string normalizedPath = NormalizePath(options.Path);
        Address = $"ws://{options.Host}:{options.Port}{normalizedPath}";
        _logPath = options.LogPath;
        _listener = new TcpListener(IPAddress.Parse(options.Host), options.Port);
    }

    public WebSocketGameServerOptions Options { get; }

    public string Address { get; }

    public int ConnectedClientCount => _clients.Count;

    public event EventHandler<WebSocketClientConnectedEventArgs>? ClientConnected;
    public event EventHandler<WebSocketClientDisconnectedEventArgs>? ClientDisconnected;
    public event EventHandler<WebSocketTextMessageReceivedEventArgs>? TextMessageReceived;

    public void Start()
    {
        if (_acceptLoopTask is not null)
        {
            return;
        }

        _listener.Start();
        Log("info", $"WebSocket game server listening at {Address}");
        _acceptLoopTask = Task.Run(() => AcceptLoopAsync(_shutdown.Token));
    }

    public Task BroadcastTextAsync(string message, CancellationToken cancellationToken = default)
    {
        Log("message", $"server broadcast: {message}");
        return Task.WhenAll(_clients.Values.Select(client => SendTextAsync(client, message, cancellationToken)));
    }

    public async Task ShutdownClientsAsync(string reason, CancellationToken cancellationToken = default)
    {
        if (_clientShutdownStarted)
        {
            return;
        }

        _clientShutdownStarted = true;
        string message = JsonSerializer.Serialize(new
        {
            type = "gameShutdown",
            payload = new
            {
                reason,
                requestedAt = DateTimeOffset.UtcNow
            }
        });

        try
        {
            try
            {
                await BroadcastTextAsync(message, cancellationToken);
            }
            catch (Exception ex) when (ex is WebSocketException or IOException or OperationCanceledException)
            {
                Log("warn", $"Shutdown broadcast did not reach every client: {ex.Message}");
            }

            await Task.WhenAll(_clients.Values.Select(client => CloseClientAsync(client, WebSocketCloseStatus.NormalClosure, reason, cancellationToken)));
        }
        finally
        {
            _clientShutdownStarted = false;
        }
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient tcpClient;

            try
            {
                tcpClient = await _listener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Log("error", $"Accept failed: {ex}");
                continue;
            }

            _ = Task.Run(() => HandleTcpClientAsync(tcpClient, cancellationToken), CancellationToken.None);
        }
    }

    private async Task HandleTcpClientAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        using (tcpClient)
        {
            using NetworkStream stream = tcpClient.GetStream();
            string remoteEndpoint = tcpClient.Client.RemoteEndPoint?.ToString() ?? "unknown";
            Guid clientId = Guid.NewGuid();

            try
            {
                HttpLikeRequest request = await ReadUpgradeRequestAsync(stream, cancellationToken);
                if (!IsValidWebSocketUpgrade(request))
                {
                    await WriteHttpErrorAsync(stream, HttpStatusCode.BadRequest, "Expected WebSocket upgrade request.", cancellationToken);
                    return;
                }

                string expectedPath = NormalizePath(Options.Path);
                if (!string.Equals(NormalizePath(request.RequestPath), expectedPath, StringComparison.Ordinal))
                {
                    await WriteHttpErrorAsync(stream, HttpStatusCode.NotFound, "Unknown WebSocket endpoint.", cancellationToken);
                    return;
                }

                await WriteUpgradeResponseAsync(stream, request.WebSocketKey!, cancellationToken);

                using WebSocket webSocket = WebSocket.CreateFromStream(
                    stream,
                    isServer: true,
                    subProtocol: null,
                    keepAliveInterval: Options.KeepAliveInterval);
                WebSocketClientConnection client = new(clientId, remoteEndpoint, webSocket);
                _clients[clientId] = client;
                Log("info", $"Client connected: {remoteEndpoint} ({clientId})");
                ClientConnected?.Invoke(this, new WebSocketClientConnectedEventArgs(clientId, remoteEndpoint));

                try
                {
                    await ReceiveMessagesAsync(client, cancellationToken);
                }
                finally
                {
                    _clients.TryRemove(clientId, out _);
                    Log("info", $"Client disconnected: {remoteEndpoint} ({clientId})");
                    ClientDisconnected?.Invoke(this, new WebSocketClientDisconnectedEventArgs(clientId, remoteEndpoint));
                }
            }
            catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
            {
                Log("info", $"Client {remoteEndpoint} receive canceled during shutdown.");
            }
            catch (WebSocketException ex)
            {
                Log("info", $"Client {remoteEndpoint} disconnected: {ex.Message}");
            }
            catch (Exception ex)
            {
                Log("error", $"Client {remoteEndpoint} failed: {ex}");
            }
        }
    }

    private async Task<HttpLikeRequest> ReadUpgradeRequestAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        List<byte> buffer = [];
        byte[] chunk = new byte[1024];

        while (true)
        {
            int read = await stream.ReadAsync(chunk, cancellationToken);
            if (read <= 0)
            {
                throw new IOException("Connection closed before WebSocket handshake completed.");
            }

            buffer.AddRange(chunk.AsSpan(0, read).ToArray());
            if (buffer.Count >= 4 &&
                buffer[^4] == '\r' &&
                buffer[^3] == '\n' &&
                buffer[^2] == '\r' &&
                buffer[^1] == '\n')
            {
                break;
            }

            if (buffer.Count > 32 * 1024)
            {
                throw new InvalidOperationException("WebSocket handshake exceeded maximum header size.");
            }
        }

        string requestText = Encoding.ASCII.GetString(buffer.ToArray());
        string[] lines = requestText.Split(["\r\n"], StringSplitOptions.None);
        if (lines.Length == 0 || string.IsNullOrWhiteSpace(lines[0]))
        {
            throw new InvalidOperationException("Missing HTTP request line.");
        }

        string[] requestLineParts = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (requestLineParts.Length < 2)
        {
            throw new InvalidOperationException("Invalid HTTP request line.");
        }

        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);
        for (int index = 1; index < lines.Length; index++)
        {
            string line = lines[index];
            if (string.IsNullOrEmpty(line))
            {
                break;
            }

            int separator = line.IndexOf(':');
            if (separator <= 0)
            {
                continue;
            }

            string name = line[..separator].Trim();
            string value = line[(separator + 1)..].Trim();
            headers[name] = value;
        }

        headers.TryGetValue("Sec-WebSocket-Key", out string? webSocketKey);
        return new HttpLikeRequest(requestLineParts[0], requestLineParts[1], headers, webSocketKey);
    }

    private static bool IsValidWebSocketUpgrade(HttpLikeRequest request)
    {
        if (!string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!request.Headers.TryGetValue("Upgrade", out string? upgrade) ||
            !string.Equals(upgrade, "websocket", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!request.Headers.TryGetValue("Connection", out string? connection) ||
            !connection.Split(',').Any(part => string.Equals(part.Trim(), "Upgrade", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (!request.Headers.TryGetValue("Sec-WebSocket-Version", out string? version) ||
            !string.Equals(version, "13", StringComparison.Ordinal))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(request.WebSocketKey);
    }

    private static async Task WriteUpgradeResponseAsync(NetworkStream stream, string webSocketKey, CancellationToken cancellationToken)
    {
        string accept = Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(webSocketKey + WebSocketGuid)));
        string response =
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Connection: Upgrade\r\n" +
            "Upgrade: websocket\r\n" +
            $"Sec-WebSocket-Accept: {accept}\r\n\r\n";
        byte[] bytes = Encoding.ASCII.GetBytes(response);
        await stream.WriteAsync(bytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private static async Task WriteHttpErrorAsync(NetworkStream stream, HttpStatusCode statusCode, string message, CancellationToken cancellationToken)
    {
        string body = message + Environment.NewLine;
        string response =
            $"HTTP/1.1 {(int)statusCode} {statusCode}\r\n" +
            "Content-Type: text/plain; charset=utf-8\r\n" +
            $"Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\n" +
            "Connection: close\r\n\r\n" +
            body;
        byte[] bytes = Encoding.UTF8.GetBytes(response);
        await stream.WriteAsync(bytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private async Task ReceiveMessagesAsync(WebSocketClientConnection client, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[8192];

        while (client.WebSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            using MemoryStream messageStream = new();
            WebSocketReceiveResult result;

            do
            {
                try
                {
                    result = await client.WebSocket.ReceiveAsync(buffer, cancellationToken);
                }
                catch (WebSocketException ex)
                {
                    Log("info", $"Client receive ended: {client.RemoteEndpoint}: {ex.Message}");
                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    Log("info", $"Client receive canceled: {client.RemoteEndpoint}");
                    return;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Log("info", $"Client closing: {client.RemoteEndpoint}");
                    await client.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    return;
                }

                messageStream.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            string message = Encoding.UTF8.GetString(messageStream.ToArray());
            Log("message", $"{client.RemoteEndpoint}: {message}");
            TextMessageReceived?.Invoke(this, new WebSocketTextMessageReceivedEventArgs(client.Id, client.RemoteEndpoint, message));
        }
    }

    private async Task SendTextAsync(WebSocketClientConnection client, string message, CancellationToken cancellationToken)
    {
        if (client.WebSocket.State != WebSocketState.Open)
        {
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(message);

        await client.SendLock.WaitAsync(cancellationToken);
        try
        {
            if (client.WebSocket.State == WebSocketState.Open)
            {
                await client.WebSocket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
            }
        }
        finally
        {
            client.SendLock.Release();
        }
    }

    private async Task CloseClientAsync(WebSocketClientConnection client, WebSocketCloseStatus status, string reason, CancellationToken cancellationToken)
    {
        await client.SendLock.WaitAsync(cancellationToken);
        try
        {
            if (client.WebSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await client.WebSocket.CloseAsync(status, reason, cancellationToken);
            }
        }
        catch (WebSocketException ex)
        {
            Log("warn", $"Client close failed for {client.RemoteEndpoint}: {ex.Message}");
        }
        finally
        {
            client.SendLock.Release();
        }
    }

    private void Log(string level, string message)
    {
        string line = $"{DateTimeOffset.Now:O} [{level.ToUpperInvariant()}] {message}{Environment.NewLine}";
        File.AppendAllText(_logPath, line);
        Console.Write(line);
    }

    public async ValueTask DisposeAsync()
    {
        await ShutdownClientsAsync("Server shutting down", CancellationToken.None);
        _shutdown.Cancel();
        _listener.Stop();

        if (_acceptLoopTask is not null)
        {
            try
            {
                await _acceptLoopTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        _shutdown.Dispose();
        Log("info", "WebSocket game server stopped.");
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        string trimmed = path.Trim();
        return trimmed[0] == '/' ? trimmed : "/" + trimmed;
    }

    private sealed class WebSocketClientConnection(Guid id, string remoteEndpoint, WebSocket webSocket)
    {
        public Guid Id { get; } = id;
        public string RemoteEndpoint { get; } = remoteEndpoint;
        public WebSocket WebSocket { get; } = webSocket;
        public SemaphoreSlim SendLock { get; } = new(1, 1);
    }

    private sealed record HttpLikeRequest(
        string Method,
        string RequestPath,
        IReadOnlyDictionary<string, string> Headers,
        string? WebSocketKey);
}

public sealed class WebSocketClientConnectedEventArgs(Guid clientId, string remoteEndpoint) : EventArgs
{
    public Guid ClientId { get; } = clientId;
    public string RemoteEndpoint { get; } = remoteEndpoint;
}

public sealed class WebSocketClientDisconnectedEventArgs(Guid clientId, string remoteEndpoint) : EventArgs
{
    public Guid ClientId { get; } = clientId;
    public string RemoteEndpoint { get; } = remoteEndpoint;
}

public sealed class WebSocketTextMessageReceivedEventArgs(Guid clientId, string remoteEndpoint, string message) : EventArgs
{
    public Guid ClientId { get; } = clientId;
    public string RemoteEndpoint { get; } = remoteEndpoint;
    public string Message { get; } = message;
}

public sealed class WebSocketGameServerOptions
{
    public string Host { get; init; } = "127.0.0.1";
    public int Port { get; init; } = 7777;
    public string Path { get; init; } = "/game";
    public TimeSpan KeepAliveInterval { get; init; } = TimeSpan.FromSeconds(30);
    public string LogPath { get; init; } = System.IO.Path.Combine(AppContext.BaseDirectory, "websocket-server.log");
}
