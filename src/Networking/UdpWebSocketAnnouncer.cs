using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Networking;

public sealed class UdpWebSocketAnnouncer : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly UdpClient _client = new();
    private readonly CancellationTokenSource _shutdown = new();
    private Task? _announceLoopTask;

    public UdpWebSocketAnnouncer(UdpWebSocketAnnouncerOptions options)
    {
        Options = options;
        _client.EnableBroadcast = true;
    }

    public UdpWebSocketAnnouncerOptions Options { get; }

    public void Start()
    {
        if (_announceLoopTask is not null)
        {
            return;
        }

        _announceLoopTask = Task.Run(() => AnnounceLoopAsync(_shutdown.Token));
    }

    private async Task AnnounceLoopAsync(CancellationToken cancellationToken)
    {
        IPEndPoint endpoint = new(IPAddress.Broadcast, Options.UdpPort);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                string message = JsonSerializer.Serialize(new
                {
                    type = "webSocketGameServer",
                    name = Options.Name,
                    ip = Options.WebSocketIp,
                    port = Options.WebSocketPort,
                    path = Options.WebSocketPath,
                    address = $"ws://{Options.WebSocketIp}:{Options.WebSocketPort}{Options.WebSocketPath}",
                    announcedAt = DateTimeOffset.Now
                }, JsonOptions);

                byte[] bytes = Encoding.UTF8.GetBytes(message);
                await _client.SendAsync(bytes, endpoint, cancellationToken);
                Console.WriteLine($"{DateTimeOffset.Now:O} [INFO] UDP announce sent to {endpoint}: {message}");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTimeOffset.Now:O} [WARN] UDP announce failed: {ex.Message}");
            }

            try
            {
                await Task.Delay(Options.Interval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _shutdown.Cancel();
        _client.Dispose();

        if (_announceLoopTask is not null)
        {
            try
            {
                await _announceLoopTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        _shutdown.Dispose();
    }
}

public sealed class UdpWebSocketAnnouncerOptions
{
    public string Name { get; init; } = "WebView2Test";
    public int UdpPort { get; init; } = 7778;
    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(2);
    public string WebSocketIp { get; init; } = "127.0.0.1";
    public int WebSocketPort { get; init; } = 7777;
    public string WebSocketPath { get; init; } = "/game";
}
