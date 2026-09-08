using System;
using System.Threading;
using System.Threading.Tasks;
using Networking;

namespace ConquestFrontierWarsRay;

/// <summary>
/// Conquest-facing server component. It forwards protocol events without making
/// the Networking project aware of game message formats or handlers.
/// </summary>
public sealed class LanGameServerComponent : IAsyncDisposable {
	private readonly WebSocketGameServer _server;

	public LanGameServerComponent(WebSocketGameServerOptions options) {
		_server = new WebSocketGameServer(options);
		_server.ClientConnected += ForwardClientConnected;
		_server.ClientDisconnected += ForwardClientDisconnected;
		_server.TextMessageReceived += ForwardTextMessageReceived;
	}

	public event EventHandler<WebSocketClientConnectedEventArgs>? ClientConnected;
	public event EventHandler<WebSocketClientDisconnectedEventArgs>? ClientDisconnected;
	public event EventHandler<WebSocketTextMessageReceivedEventArgs>? TextMessageReceived;

	public string Address => _server.Address;
	public int ConnectedClientCount => _server.ConnectedClientCount;

	public void Start() => _server.Start();

	public Task BroadcastTextAsync(string message, CancellationToken cancellationToken = default) =>
		_server.BroadcastTextAsync(message, cancellationToken);

	public Task<bool> SendTextAsync(Guid clientId, string message, CancellationToken cancellationToken = default) =>
		_server.SendTextAsync(clientId, message, cancellationToken);

	public Task ShutdownAsync(string reason, CancellationToken cancellationToken = default) {
		Console.WriteLine($"LAN game shutdown requested ({reason}). Game shutdown message handling still needs an implementation.");
		return _server.CloseAllClientsAsync(reason, cancellationToken);
	}

	public async ValueTask DisposeAsync() {
		_server.ClientConnected -= ForwardClientConnected;
		_server.ClientDisconnected -= ForwardClientDisconnected;
		_server.TextMessageReceived -= ForwardTextMessageReceived;
		await _server.DisposeAsync().ConfigureAwait(false);
	}

	private void ForwardClientConnected(object? sender, WebSocketClientConnectedEventArgs args) => ClientConnected?.Invoke(this, args);
	private void ForwardClientDisconnected(object? sender, WebSocketClientDisconnectedEventArgs args) => ClientDisconnected?.Invoke(this, args);
	private void ForwardTextMessageReceived(object? sender, WebSocketTextMessageReceivedEventArgs args) => TextMessageReceived?.Invoke(this, args);
}
