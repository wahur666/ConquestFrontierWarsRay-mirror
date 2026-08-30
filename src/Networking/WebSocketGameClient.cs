using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Networking;

public sealed class WebSocketGameClient(WebSocketGameClientOptions options) : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ClientWebSocket _socket = new();
    private readonly CancellationTokenSource _shutdown = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly Lock _receiveLoopSync = new();
    private CancellationTokenSource? _udpDiscoveryTimeout;
    private Task? _receiveLoopTask;
    private string? _pollCommandId;
    private string? _moveCommandId;
    private string? _moveCommandJson;
    private bool _pollSent;
    private bool _loadGameHandled;
    private bool _clientReadySent;
    private bool _closingReported;
    private bool _disposeStarted;

    public bool IsConnected => _socket.State == WebSocketState.Open;

    public event EventHandler<WebSocketClientTextMessageReceivedEventArgs>? TextMessageReceived;

    public async Task<string> ConnectAsync(CancellationToken cancellationToken = default)
    {
        using CancellationTokenSource linkedShutdown = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdown.Token);
        string address = await ResolveAddressAsync(linkedShutdown.Token);
        if (_socket.State == WebSocketState.Open)
        {
            return address;
        }

        ReportStatus($"Connecting WebSocket: {address}");
        Log("info", $"Connecting to {address}");
        await _socket.ConnectAsync(new Uri(address), linkedShutdown.Token);
        ReportStatus("WebSocket connected.");
        Log("info", "Connected.");
        return address;
    }

    public async Task<WebSocketGameClientResult> RunAutomatedAsync(CancellationToken cancellationToken = default)
    {
        await ConnectAsync(cancellationToken);
        ReportStatus("WebSocket connected. Waiting for game load signal.");

        using CancellationTokenSource linkedShutdown = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdown.Token);
        await SendClientReadyIfHeadlessAsync(linkedShutdown.Token);
        Log("info", "Waiting for gameStarted.");

        try
        {
            await EnsureReceiveLoopAsync(linkedShutdown.Token);
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
            Log("info", "Client receive loop canceled during shutdown.");
        }
        catch (WebSocketException ex) when (_shutdown.IsCancellationRequested || _disposeStarted)
        {
            Log("info", $"Client receive loop ended during shutdown: {ex.Message}");
        }

        return new WebSocketGameClientResult(_socket.CloseStatus ?? WebSocketCloseStatus.Empty, _socket.CloseStatusDescription ?? string.Empty);
    }

    public Task StartBackgroundReceiveLoopAsync(CancellationToken cancellationToken = default)
    {
        return EnsureReceiveLoopAsync(cancellationToken);
    }

    private async Task<string> ResolveAddressAsync(CancellationToken cancellationToken)
    {
        if (options.UdpDiscover)
        {
            ReportStatus($"UDP discovery listening on 0.0.0.0:{options.UdpPort}.");
            return await DiscoverAddressAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(options.Address))
        {
            return options.Address;
        }

        string addressFilePath = options.AddressFilePath ?? Path.Combine(AppContext.BaseDirectory, "game-server-address.txt");
        return (await File.ReadAllTextAsync(addressFilePath, cancellationToken)).Trim();
    }

    private async Task<string> DiscoverAddressAsync(CancellationToken cancellationToken)
    {
        using UdpClient udpClient = new(new IPEndPoint(IPAddress.Any, options.UdpPort))
        {
            EnableBroadcast = true
        };

        Log("info", $"Listening for UDP discovery on 0.0.0.0:{options.UdpPort}");

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _udpDiscoveryTimeout = timeout;
        timeout.CancelAfter(options.UdpTimeout);

        while (true)
        {
            UdpReceiveResult result;
            try
            {
                result = await udpClient.ReceiveAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"Timed out waiting for UDP WebSocket announcement on 0.0.0.0:{options.UdpPort}.");
            }

            JsonDocument document;
            try
            {
                string json = Encoding.UTF8.GetString(result.Buffer);
                document = JsonDocument.Parse(json);
            }
            catch (JsonException)
            {
                continue;
            }

            using (document)
            {
                JsonElement root = document.RootElement;
                string? type = root.TryGetProperty("type", out JsonElement typeElement) ? typeElement.GetString() : null;
                if (type != "webSocketGameServer")
                {
                    continue;
                }

                string? name = root.TryGetProperty("name", out JsonElement nameElement) ? nameElement.GetString() : null;
                if (!string.IsNullOrWhiteSpace(options.UdpName) && name != options.UdpName)
                {
                    continue;
                }

                string? address = BuildAddressFromAnnouncement(root);
                if (address is null)
                {
                    Log("warn", $"Ignored invalid announcement from {result.RemoteEndPoint}.");
                    continue;
                }

                Log("info", $"Discovered {name ?? "server"} at {address}");
                ReportStatus($"UDP discovery complete. Found {address}.");
                _udpDiscoveryTimeout = null;
                return address;
            }
        }
    }

    private static string? BuildAddressFromAnnouncement(JsonElement announcement)
    {
        if (announcement.TryGetProperty("address", out JsonElement addressElement))
        {
            string? address = addressElement.GetString();
            if (!string.IsNullOrWhiteSpace(address) && address.StartsWith("ws://", StringComparison.OrdinalIgnoreCase))
            {
                return address;
            }
        }

        string? ip = announcement.TryGetProperty("ip", out JsonElement ipElement) ? ipElement.GetString() : null;
        int? port = announcement.TryGetProperty("port", out JsonElement portElement) && portElement.TryGetInt32(out int parsedPort)
            ? parsedPort
            : null;
        string? path = announcement.TryGetProperty("path", out JsonElement pathElement) ? pathElement.GetString() : null;

        if (string.IsNullOrWhiteSpace(ip) || port is null or < 1 or > 65535 || string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return $"ws://{ip}:{port}{(path.StartsWith('/') ? path : "/" + path)}";
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[8192];

        while (_socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            using MemoryStream messageStream = new();
            WebSocketReceiveResult result;

            do
            {
                result = await _socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Log("info", $"Server closed code={_socket.CloseStatus} reason={_socket.CloseStatusDescription}");
                    return;
                }

                messageStream.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            if (result.MessageType != WebSocketMessageType.Text)
            {
                continue;
            }

            string message = Encoding.UTF8.GetString(messageStream.ToArray());
            LogCommand("client-received", message);
            Log("message", $"recv {message}");
            TextMessageReceived?.Invoke(this, new WebSocketClientTextMessageReceivedEventArgs(message));
            await HandleMessageAsync(message, cancellationToken);
        }
    }

    public Task SendTextAsync(string text, CancellationToken cancellationToken = default)
    {
        return SendJsonTextAsync(text, cancellationToken);
    }

    private async Task HandleMessageAsync(string text, CancellationToken cancellationToken)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            return;
        }

        using (document)
        {
            JsonElement root = document.RootElement;
            string? type = root.TryGetProperty("type", out JsonElement typeElement) ? typeElement.GetString() : null;
            JsonElement payload = root.TryGetProperty("payload", out JsonElement payloadElement) ? payloadElement : default;

            switch (type)
            {
                case "loadGame":
                    await HandleLoadGameAsync(cancellationToken);
                    break;

                case "gameStarted":
                    await HandleGameStartedAsync(cancellationToken);
                    break;

                case "gameCommandResult":
                    await HandleGameCommandResultAsync(payload, cancellationToken);
                    break;

                case "gameCommand":
                    await ReplayAcceptedGameCommandAsync(payload);
                    break;

                case "gameMovementFinished":
                    await HandleGameMovementFinishedAsync(payload, cancellationToken);
                    break;

                case "gameMovementSimulationFinished":
                    bool passed = payload.TryGetProperty("passed", out JsonElement passedElement) && passedElement.GetBoolean();
                    Log("info", $"simulation {(passed ? "passed" : "failed")} {payload.GetRawText()}");
                    break;

                case "gameShutdown":
                    await HandleGameShutdownAsync(payload, cancellationToken);
                    break;
            }
        }
    }

    private async Task HandleGameShutdownAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        string reason = payload.TryGetProperty("reason", out JsonElement reasonElement)
            ? reasonElement.GetString() ?? "Game session ended."
            : "Game session ended.";
        ReportStatus(reason);

        if (options.OnSessionEndedAsync is not null)
        {
            await options.OnSessionEndedAsync(reason);
        }

        await CloseAsync(WebSocketCloseStatus.NormalClosure, reason, cancellationToken);
    }

    private async Task HandleLoadGameAsync(CancellationToken cancellationToken)
    {
        if (_loadGameHandled)
        {
            return;
        }

        _loadGameHandled = true;
        ReportStatus("Starting game.");

        if (options.OnLoadGameAsync is not null)
        {
            await options.OnLoadGameAsync();
        }

        if (options.ReadySignal is not null)
        {
            Log("info", "Waiting for local client game readiness.");
            ReportStatus("Loading game assets. Waiting for local game readiness.");
            await options.ReadySignal.WaitAsync(cancellationToken);
        }

        ReportStatus("Client ready. Waiting for gameStart signal.");
        await SendClientReadyAsync(cancellationToken);
    }

    private async Task HandleGameStartedAsync(CancellationToken cancellationToken)
    {
        await SendJsonAsync(new
        {
            type = "gameStartedAck",
            status = "OK",
            receivedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        await PollOwnerUnitsAsync(cancellationToken);
    }

    private Task SendClientReadyAsync(CancellationToken cancellationToken)
    {
        if (_clientReadySent)
        {
            return Task.CompletedTask;
        }

        _clientReadySent = true;
        return SendJsonAsync(new
        {
            type = "clientReady",
            status = "OK",
            readyAt = DateTimeOffset.UtcNow,
            headless = options.ReadySignal is null
        }, cancellationToken);
    }

    private Task SendClientReadyIfHeadlessAsync(CancellationToken cancellationToken)
    {
        if (options.ReadySignal is not null)
        {
            return Task.CompletedTask;
        }

        return SendClientReadyAsync(cancellationToken);
    }

    private async Task PollOwnerUnitsAsync(CancellationToken cancellationToken)
    {
        if (_pollSent)
        {
            return;
        }

        _pollSent = true;
        _pollCommandId = $"poll-owner-{options.ControlledOwner}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        await SendJsonAsync(new
        {
            type = "pollUnits",
            commandId = _pollCommandId,
            payload = new
            {
                owner = options.ControlledOwner
            }
        }, cancellationToken);
    }

    private async Task HandleGameCommandResultAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        Log("info", $"result {payload.GetRawText()}");

        string? commandId = payload.TryGetProperty("commandId", out JsonElement commandIdElement) ? commandIdElement.GetString() : null;
        if (commandId == _pollCommandId)
        {
            await MoveFirstPolledUnitAsync(payload, cancellationToken);
            return;
        }

        if (commandId == _moveCommandId &&
            payload.TryGetProperty("accepted", out JsonElement acceptedElement) &&
            acceptedElement.ValueKind == JsonValueKind.False)
        {
            string reason = payload.TryGetProperty("message", out JsonElement messageElement)
                ? messageElement.GetString() ?? "move rejected"
                : "move rejected";
            await CloseAsync(WebSocketCloseStatus.InternalServerError, reason, cancellationToken);
            return;
        }

        if (commandId == _moveCommandId &&
            payload.TryGetProperty("accepted", out acceptedElement) &&
            acceptedElement.ValueKind == JsonValueKind.True)
        {
            Log("info", "move accepted; waiting for authoritative gameCommand replay.");
        }
    }

    private async Task MoveFirstPolledUnitAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        if (!payload.TryGetProperty("units", out JsonElement unitsElement) ||
            unitsElement.ValueKind != JsonValueKind.Array ||
            unitsElement.GetArrayLength() == 0)
        {
            await CloseAsync(WebSocketCloseStatus.InternalServerError, $"no units for owner {options.ControlledOwner}", cancellationToken);
            return;
        }

        JsonElement unit = unitsElement.EnumerateArray().First();
        string? unitId = unit.TryGetProperty("id", out JsonElement idElement) ? idElement.GetString() : null;
        JsonElement position = unit.TryGetProperty("position", out JsonElement positionElement) ? positionElement : default;

        if (string.IsNullOrWhiteSpace(unitId) ||
            !TryReadFiniteNumber(position, "x", out double x) ||
            !TryReadFiniteNumber(position, "z", out double z))
        {
            await CloseAsync(WebSocketCloseStatus.InternalServerError, "polled unit has invalid position", cancellationToken);
            return;
        }

        _moveCommandId = $"move-owner-{options.ControlledOwner}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        object moveCommand = new
        {
            type = "moveUnit",
            commandId = _moveCommandId,
            payload = new
            {
                owner = options.ControlledOwner,
                unitIds = new[] { unitId },
                x,
                y = 0,
                z = z + options.MoveDeltaZ
            }
        };

        _moveCommandJson = JsonSerializer.Serialize(moveCommand, JsonOptions);
        await SendJsonTextAsync(_moveCommandJson, cancellationToken);
    }

    private async Task ReplayAcceptedGameCommandAsync(JsonElement command)
    {
        if (options.OnAcceptedGameCommandAsync is null)
        {
            return;
        }

        await options.OnAcceptedGameCommandAsync(command.Clone());
    }

    public async Task<bool> SendGameCommandAsync(JsonElement command, CancellationToken cancellationToken = default)
    {
        if (_socket.State != WebSocketState.Open)
        {
            return false;
        }

        await SendJsonTextAsync(command.GetRawText(), cancellationToken);
        return true;
    }

    public async Task NotifyClosingAsync(string reason, CancellationToken cancellationToken = default)
    {
        if (_closingReported || _socket.State != WebSocketState.Open)
        {
            return;
        }

        _closingReported = true;
        await SendJsonAsync(new
        {
            type = "clientClosing",
            reason,
            closingAt = DateTimeOffset.UtcNow
        }, cancellationToken);
    }

    private async Task HandleGameMovementFinishedAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        Log("info", $"movement finished {payload.GetRawText()}");

        string? commandId = payload.TryGetProperty("commandId", out JsonElement commandIdElement) ? commandIdElement.GetString() : null;
        if (commandId == _moveCommandId)
        {
            ReportStatus("Automated move confirmed. Client remains connected for manual control.");
            LogCommand("client-keepalive", JsonSerializer.Serialize(new
            {
                reason = "owner unit movement confirmed",
                commandId
            }, JsonOptions));
        }
    }

    private static bool TryReadFiniteNumber(JsonElement element, string propertyName, out double value)
    {
        value = 0;
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out JsonElement property))
        {
            return false;
        }

        value = property.GetDouble();
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private async Task SendJsonAsync(object message, CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(message, JsonOptions);
        await SendJsonTextAsync(json, cancellationToken);
    }

    private async Task SendJsonTextAsync(string json, CancellationToken cancellationToken)
    {
        LogCommand("client-send", json);
        Log("message", $"send {json}");
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await _socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task CloseAsync(WebSocketCloseStatus status, string reason, CancellationToken cancellationToken)
    {
        if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            await _socket.CloseAsync(status, reason, cancellationToken);
        }
    }

    private static void Log(string level, string message)
    {
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{DateTimeOffset.Now:O} [{level.ToUpperInvariant()}] {message}"));
    }

    private void LogCommand(string direction, string json)
    {
        string line = $"{DateTimeOffset.Now:O} [{direction}] {json}{Environment.NewLine}";
        AppendLogLine(options.CommandLogPath, line);
    }

    private static void AppendLogLine(string path, string line)
    {
        const int maxAttempts = 5;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                File.AppendAllText(path, line);
                return;
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                Thread.Sleep(10 * attempt);
            }
        }
    }

    private void ReportStatus(string message)
    {
        options.StatusChanged?.Invoke(message);
    }

    public void StopUdpDiscovery()
    {
        _udpDiscoveryTimeout?.Cancel();
        _udpDiscoveryTimeout = null;
    }

    public async ValueTask DisposeAsync()
    {
        _disposeStarted = true;
        _shutdown.Cancel();

        Task? receiveLoopTask;
        lock (_receiveLoopSync) {
            receiveLoopTask = _receiveLoopTask;
        }

        if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            await NotifyClosingAsync("Client shutting down", CancellationToken.None);
            try
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client shutting down", CancellationToken.None);
            }
            catch (WebSocketException ex)
            {
                Log("info", $"Client socket close ended during shutdown: {ex.Message}");
            }
        }

        if (receiveLoopTask is not null)
        {
            try
            {
                await receiveLoopTask;
            }
            catch (OperationCanceledException)
            {
            }
            catch (WebSocketException) when (_shutdown.IsCancellationRequested || _disposeStarted)
            {
            }
        }

        _socket.Dispose();
        _sendLock.Dispose();
        _shutdown.Dispose();
    }

    private Task EnsureReceiveLoopAsync(CancellationToken cancellationToken)
    {
        lock (_receiveLoopSync) {
            if (_receiveLoopTask is null || _receiveLoopTask.IsCompleted) {
                _receiveLoopTask = ReceiveLoopAsync(cancellationToken);
            }

            return _receiveLoopTask;
        }
    }
}

public sealed class WebSocketGameClientOptions
{
    public string? Address { get; init; }
    public string? AddressFilePath { get; init; }
    public bool UdpDiscover { get; init; }
    public int UdpPort { get; init; } = 7778;
    public string? UdpName { get; init; }
    public TimeSpan UdpTimeout { get; init; } = TimeSpan.FromSeconds(15);
    public int ControlledOwner { get; init; } = 1;
    public double MoveDeltaZ { get; init; } = -10;
    public Task? ReadySignal { get; init; }
    public Func<Task>? OnLoadGameAsync { get; init; }
    public Func<JsonElement, Task>? OnAcceptedGameCommandAsync { get; init; }
    public Func<string, Task>? OnSessionEndedAsync { get; init; }
    public Action<string>? StatusChanged { get; init; }
    public string CommandLogPath { get; init; } = Path.Combine(AppContext.BaseDirectory, "client-commands.log");
}

public sealed record WebSocketGameClientResult(WebSocketCloseStatus CloseStatus, string CloseStatusDescription);

public sealed class WebSocketClientTextMessageReceivedEventArgs(string message) : EventArgs
{
    public string Message { get; } = message;
}
