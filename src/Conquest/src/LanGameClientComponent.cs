using System;
using System.Threading;
using System.Threading.Tasks;
using Networking;

namespace ConquestFrontierWarsRay;

/// <summary>Conquest-facing client component that forwards protocol data to game code.</summary>
public sealed class LanGameClientComponent : IAsyncDisposable {
	private readonly WebSocketGameClient _client;

	public LanGameClientComponent(WebSocketGameClientOptions options) {
		_client = new WebSocketGameClient(options);
		_client.TextMessageReceived += ForwardTextMessageReceived;
		_client.Closed += ForwardClosed;
	}

	public bool IsConnected => _client.IsConnected;
	public event EventHandler<WebSocketClientTextMessageReceivedEventArgs>? TextMessageReceived;
	public event EventHandler<WebSocketClientClosedEventArgs>? Closed;
	public Task<string> ConnectAsync(CancellationToken cancellationToken = default) => _client.ConnectAsync(cancellationToken);
	public Task StartBackgroundReceiveLoopAsync(CancellationToken cancellationToken = default) => _client.StartBackgroundReceiveLoopAsync(cancellationToken);
	public Task SendTextAsync(string message, CancellationToken cancellationToken = default) => _client.SendTextAsync(message, cancellationToken);

	public async ValueTask DisposeAsync() {
		_client.TextMessageReceived -= ForwardTextMessageReceived;
		_client.Closed -= ForwardClosed;
		await _client.DisposeAsync().ConfigureAwait(false);
	}

	private void ForwardTextMessageReceived(object? sender, WebSocketClientTextMessageReceivedEventArgs args) {
		if (TextMessageReceived is null) {
			Console.WriteLine("Received a LAN game message. Conquest message handling still needs an implementation.");
			return;
		}

		TextMessageReceived.Invoke(this, args);
	}

	private void ForwardClosed(object? sender, WebSocketClientClosedEventArgs args) => Closed?.Invoke(this, args);
}
