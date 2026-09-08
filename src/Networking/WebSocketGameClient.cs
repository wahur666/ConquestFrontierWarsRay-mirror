using System.Net.WebSockets;
using System.Text;

namespace Networking;

/// <summary>WebSocket transport client. Application message interpretation is exposed through events.</summary>
public sealed class WebSocketGameClient(WebSocketGameClientOptions options) : IAsyncDisposable {
	private readonly ClientWebSocket _socket = new();
	private readonly CancellationTokenSource _shutdown = new();
	private readonly SemaphoreSlim _sendLock = new(1, 1);
	private readonly Lock _receiveLoopSync = new();
	private Task? _receiveLoopTask;
	private bool _disposeStarted;

	public bool IsConnected => _socket.State == WebSocketState.Open;
	public event EventHandler<WebSocketClientTextMessageReceivedEventArgs>? TextMessageReceived;
	public event EventHandler<WebSocketClientClosedEventArgs>? Closed;

	public async Task<string> ConnectAsync(CancellationToken cancellationToken = default) {
		using CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdown.Token);
		string address = await ResolveAddressAsync(linkedCancellation.Token).ConfigureAwait(false);
		if (_socket.State == WebSocketState.Open) return address;
		await _socket.ConnectAsync(new Uri(address), linkedCancellation.Token).ConfigureAwait(false);
		return address;
	}

	public Task StartBackgroundReceiveLoopAsync(CancellationToken cancellationToken = default) => EnsureReceiveLoopAsync(cancellationToken);

	public async Task SendTextAsync(string text, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(text);
		byte[] bytes = Encoding.UTF8.GetBytes(text);
		await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
		try {
			if (_socket.State != WebSocketState.Open) throw new InvalidOperationException("The WebSocket client is not connected.");
			await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
		} finally { _sendLock.Release(); }
	}

	private async Task<string> ResolveAddressAsync(CancellationToken cancellationToken) {
		if (!string.IsNullOrWhiteSpace(options.Address)) return options.Address;
		string addressFilePath = options.AddressFilePath ?? Path.Combine(AppContext.BaseDirectory, "game-server-address.txt");
		return (await File.ReadAllTextAsync(addressFilePath, cancellationToken).ConfigureAwait(false)).Trim();
	}

	private async Task ReceiveLoopAsync(CancellationToken cancellationToken) {
		byte[] buffer = new byte[8192];
		while (_socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested) {
			using MemoryStream messageStream = new();
			WebSocketReceiveResult result;
			do {
				result = await _socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
				if (result.MessageType == WebSocketMessageType.Close) {
					Closed?.Invoke(this, new WebSocketClientClosedEventArgs(_socket.CloseStatus, _socket.CloseStatusDescription));
					return;
				}
				messageStream.Write(buffer, 0, result.Count);
			} while (!result.EndOfMessage);

			if (result.MessageType == WebSocketMessageType.Text)
				TextMessageReceived?.Invoke(this, new WebSocketClientTextMessageReceivedEventArgs(Encoding.UTF8.GetString(messageStream.ToArray())));
		}
	}

	public async ValueTask DisposeAsync() {
		_disposeStarted = true;
		_shutdown.Cancel();
		Task? receiveLoopTask;
		lock (_receiveLoopSync) receiveLoopTask = _receiveLoopTask;
		if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived) {
			try { await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client shutting down", CancellationToken.None).ConfigureAwait(false); }
			catch (WebSocketException) when (_disposeStarted) { }
		}
		if (receiveLoopTask is not null) {
			try { await receiveLoopTask.ConfigureAwait(false); }
			catch (OperationCanceledException) { }
			catch (WebSocketException) when (_disposeStarted) { }
		}
		_socket.Dispose();
		_sendLock.Dispose();
		_shutdown.Dispose();
	}

	private Task EnsureReceiveLoopAsync(CancellationToken cancellationToken) {
		lock (_receiveLoopSync) {
			if (_receiveLoopTask is null || _receiveLoopTask.IsCompleted) _receiveLoopTask = ReceiveLoopAsync(cancellationToken);
			return _receiveLoopTask;
		}
	}
}

public sealed class WebSocketGameClientOptions {
	public string? Address { get; init; }
	public string? AddressFilePath { get; init; }
}

public sealed class WebSocketClientTextMessageReceivedEventArgs(string message) : EventArgs {
	public string Message { get; } = message;
}

public sealed class WebSocketClientClosedEventArgs(WebSocketCloseStatus? status, string? description) : EventArgs {
	public WebSocketCloseStatus? Status { get; } = status;
	public string? Description { get; } = description;
}
