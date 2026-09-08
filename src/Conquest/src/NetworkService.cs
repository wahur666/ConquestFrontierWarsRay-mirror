using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ConquestFrontierWarsRay.Framework;
using Networking;

namespace ConquestFrontierWarsRay;

public sealed record NetworkAddressInfo(string PublicIp, IReadOnlyList<string> LocalIpv4Addresses);

public sealed record LanSessionAnnouncement(
	string Key,
	string SessionName,
	string Address,
	string HostIp,
	int Port,
	string Path,
	DateTimeOffset AnnouncedAt,
	bool IsLocalHost,
	string? LobbyCode = null,
	int PlayerCount = 0,
	int MaxPlayers = 0);

public sealed record LanLobbyPlayerSlot(
	int SlotIndex,
	string Name,
	bool IsHost,
	bool IsConnected,
	bool IsLocal,
	DateTimeOffset JoinedAt);

public sealed record LanLobbyState(
	string LobbyCode,
	string SessionName,
	string Address,
	int MaxPlayers,
	IReadOnlyList<LanLobbyPlayerSlot> Slots);

public sealed record LanLobbyChannelMessage(
	string Channel,
	string FromPlayerName,
	int FromSlotIndex,
	string? Text,
	JsonElement? Data,
	DateTimeOffset SentAt,
	bool IsLocal);

public static class NetworkService {
	private const int DefaultLanUdpPort = 7778;
	private const int DefaultLanWebSocketPort = 7777;
	private const int MaxLobbyPlayers = 8;
	private const string DefaultLanWebSocketPath = "/game";
	private const string DefaultPlayerName = "Commander";
	private static readonly TimeSpan LanAnnouncementTtl = TimeSpan.FromSeconds(6);
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
	private static readonly Lock Sync = new();
	private static readonly Dictionary<string, LanSessionAnnouncement> LanAnnouncements = new(StringComparer.OrdinalIgnoreCase);
	private static readonly char[] LobbyCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();
	private static CancellationTokenSource? _lanDiscoveryCts;
	private static Task? _lanDiscoveryTask;
	private static LanGameServerComponent? _lanServer;
	private static UdpWebSocketAnnouncer? _lanAnnouncer;
	private static LanGameClientComponent? _lanClient;
	private static Task? _lanClientReceiveLoopTask;
	private static TaskCompletionSource<LobbyJoinHandshake>? _pendingLobbyJoin;
	private static TaskCompletionSource<bool>? _pendingLobbyStateSync;
	private static LanSessionAnnouncement? _currentLanSession;
	private static LanLobbyState? _currentLanLobbyState;
	private static HostLobbyState? _hostLobbyState;
	private static string? _localLobbyPlayerName;
	private static int? _localLobbySlotIndex;

	public static event Action<LanLobbyState>? LanLobbyStateChanged;
	public static event Action<LanLobbyChannelMessage>? LanLobbyChannelMessageReceived;

	public static bool IsHostingLanSession {
		get {
			lock (Sync) {
				return _lanServer is not null || _lanAnnouncer is not null;
			}
		}
	}

	public static async Task<NetworkAddressInfo> GetNetworkAddressesAsync(bool fakePublicIp = true) {
		using var httpClient = new HttpClient();
		var publicIp = fakePublicIp ? "10.0.0.1" : (await httpClient.GetStringAsync("https://api.ipify.org")).Trim();
		var addresses = NetworkInterface.GetAllNetworkInterfaces()
			.Where(x =>
				x.OperationalStatus == OperationalStatus.Up &&
				x.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
				!x.Description.Contains("VirtualBox", StringComparison.OrdinalIgnoreCase) &&
				!x.Description.Contains("Docker", StringComparison.OrdinalIgnoreCase) &&
				!x.Description.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase))
			.SelectMany(x => x.GetIPProperties().UnicastAddresses)
			.Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork)
			.Select(x => x.Address)
			.Select(x => x.ToString())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		return new NetworkAddressInfo(publicIp, addresses);
	}

	public static async Task GetNetworkAddresses() {
		var addresses = await GetNetworkAddressesAsync();
		AppLog.Info("Menu", $"My public IP Address is: {addresses.PublicIp}");
		foreach (var ip in addresses.LocalIpv4Addresses) {
			AppLog.Info("Menu", $"Local ip address {ip}");
		}
	}

	public static void StartLanSessionDiscovery() {
		lock (Sync) {
			if (_lanDiscoveryTask is not null || _lanAnnouncer is not null || _lanServer is not null) {
				return;
			}

			_lanDiscoveryCts = new CancellationTokenSource();
			_lanDiscoveryTask = Task.Run(() => RunLanDiscoveryLoopAsync(_lanDiscoveryCts.Token));
		}
	}

	public static void StopLanSessionDiscovery() {
		CancellationTokenSource? cts;
		Task? task;
		lock (Sync) {
			cts = _lanDiscoveryCts;
			task = _lanDiscoveryTask;
			_lanDiscoveryCts = null;
			_lanDiscoveryTask = null;
		}

		if (cts is null) {
			return;
		}

		cts.Cancel();
		try {
			task?.Wait(TimeSpan.FromSeconds(1));
		} catch (AggregateException ex) when (ex.InnerExceptions.All(static inner => inner is OperationCanceledException)) {
		} finally {
			cts.Dispose();
		}
	}

	public static IReadOnlyList<LanSessionAnnouncement> GetLanSessionAnnouncements() {
		lock (Sync) {
			PruneExpiredAnnouncements_NoLock(DateTimeOffset.UtcNow);
			return LanAnnouncements.Values
				.OrderByDescending(x => x.IsLocalHost)
				.ThenBy(x => x.SessionName, StringComparer.OrdinalIgnoreCase)
				.ThenBy(x => x.HostIp, StringComparer.OrdinalIgnoreCase)
				.ToArray();
		}
	}

	public static LanSessionAnnouncement? GetCurrentLanSession() {
		lock (Sync) {
			return _currentLanSession;
		}
	}

	public static LanLobbyState? GetCurrentLanLobbyState() {
		lock (Sync) {
			return _currentLanLobbyState;
		}
	}

	public static Task<LanSessionAnnouncement> HostLanSessionAsync(string sessionName, CancellationToken cancellationToken = default) {
		return HostLanSessionAsync(sessionName, DefaultPlayerName, cancellationToken);
	}

	public static async Task<LanSessionAnnouncement> HostLanSessionAsync(string sessionName, string playerName, CancellationToken cancellationToken = default) {
		if (string.IsNullOrWhiteSpace(sessionName)) {
			throw new ArgumentException("Session name is required.", nameof(sessionName));
		}

		StopLanSessionDiscovery();
		await ShutdownLanSessionAsync().ConfigureAwait(false);

		var addresses = await GetNetworkAddressesAsync().ConfigureAwait(false);
		var hostIp = addresses.LocalIpv4Addresses.FirstOrDefault(static ip => !string.IsNullOrWhiteSpace(ip)) ?? IPAddress.Loopback.ToString();
		var normalizedPath = NormalizeWebSocketPath(DefaultLanWebSocketPath);
		var address = BuildWebSocketAddress(hostIp, DefaultLanWebSocketPort, normalizedPath);
		var normalizedPlayerName = NormalizePlayerName(playerName);
		var lobbyCode = GenerateLobbyCode();

		var server = new LanGameServerComponent(new WebSocketGameServerOptions {
			Host = hostIp,
			Port = DefaultLanWebSocketPort,
			Path = normalizedPath
		});
		server.ClientDisconnected += HandleLanServerClientDisconnected;
		server.TextMessageReceived += HandleLanServerTextMessageReceived;
		server.Start();

		var announcer = new UdpWebSocketAnnouncer(new UdpWebSocketAnnouncerOptions {
			Name = sessionName.Trim(),
			LobbyCode = lobbyCode,
			PlayerCount = 1,
			MaxPlayers = MaxLobbyPlayers,
			UdpPort = DefaultLanUdpPort,
			WebSocketIp = hostIp,
			WebSocketPort = DefaultLanWebSocketPort,
			WebSocketPath = normalizedPath
		});
		announcer.Start();

		var hostLobby = new HostLobbyState(sessionName.Trim(), address, lobbyCode);
		hostLobby.Slots[0] = new MutableLobbyPlayerSlot {
			SlotIndex = 0,
			Name = normalizedPlayerName,
			IsHost = true,
			IsConnected = true,
			IsLocal = true,
			JoinedAt = DateTimeOffset.UtcNow
		};

		var announcement = new LanSessionAnnouncement(
			Key: address,
			SessionName: sessionName.Trim(),
			Address: address,
			HostIp: hostIp,
			Port: DefaultLanWebSocketPort,
			Path: normalizedPath,
			AnnouncedAt: DateTimeOffset.UtcNow,
			IsLocalHost: true,
			LobbyCode: lobbyCode,
			PlayerCount: 1,
			MaxPlayers: MaxLobbyPlayers);

		LanLobbyState? publishedState;
		lock (Sync) {
			_lanServer = server;
			_lanAnnouncer = announcer;
			_lanClient = null;
			_lanClientReceiveLoopTask = null;
			_pendingLobbyJoin = null;
			_pendingLobbyStateSync = null;
			_hostLobbyState = hostLobby;
			_localLobbyPlayerName = normalizedPlayerName;
			_localLobbySlotIndex = 0;
			_currentLanSession = announcement;
			_currentLanLobbyState = CreatePublicLobbyState_NoLock(hostLobby);
			LanAnnouncements[announcement.Key] = announcement;
			publishedState = _currentLanLobbyState;
		}

		if (publishedState is not null) {
			LanLobbyStateChanged?.Invoke(publishedState);
		}

		AppLog.Info("Network", $"Hosting LAN session '{announcement.SessionName}' at {announcement.Address} with lobby code {announcement.LobbyCode}");
		return announcement;
	}

	public static Task<LanSessionAnnouncement> JoinLanSessionAsync(LanSessionAnnouncement announcement, CancellationToken cancellationToken = default) {
		return JoinLanSessionAsync(announcement, DefaultPlayerName, cancellationToken);
	}

	public static async Task<LanSessionAnnouncement> JoinLanSessionAsync(LanSessionAnnouncement announcement, string playerName, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(announcement);
		if (IsHostingLanSession) {
			throw new InvalidOperationException("Cannot join a LAN session while this process is hosting one.");
		}

		LanGameClientComponent? existingClient;
		Task? existingReceiveLoop;
		lock (Sync) {
			existingClient = _lanClient;
			existingReceiveLoop = _lanClientReceiveLoopTask;
			_lanClient = null;
			_lanClientReceiveLoopTask = null;
			_currentLanSession = null;
			_currentLanLobbyState = null;
			_localLobbySlotIndex = null;
			_pendingLobbyJoin = null;
			_pendingLobbyStateSync = null;
		}

		if (existingClient is not null) {
			await existingClient.DisposeAsync().ConfigureAwait(false);
		}

		if (existingReceiveLoop is not null) {
			try {
				await existingReceiveLoop.ConfigureAwait(false);
			} catch (OperationCanceledException) {
			} catch (WebSocketException) {
			}
		}

		var normalizedPlayerName = NormalizePlayerName(playerName);
		var client = new LanGameClientComponent(new WebSocketGameClientOptions {
			Address = announcement.Address
		});
		client.TextMessageReceived += HandleLanClientTextMessageReceived;
		await client.ConnectAsync(cancellationToken).ConfigureAwait(false);

		TaskCompletionSource<LobbyJoinHandshake> handshake = new(TaskCreationOptions.RunContinuationsAsynchronously);
		TaskCompletionSource<bool> lobbyStateSync = new(TaskCreationOptions.RunContinuationsAsynchronously);
		lock (Sync) {
			_lanClient = client;
			_lanClientReceiveLoopTask = client.StartBackgroundReceiveLoopAsync();
			_pendingLobbyJoin = handshake;
			_pendingLobbyStateSync = lobbyStateSync;
			_localLobbyPlayerName = normalizedPlayerName;
		}

		await client.SendTextAsync(JsonSerializer.Serialize(new {
			type = "lobbyHello",
			payload = new {
				playerName = normalizedPlayerName,
				lobbyCode = announcement.LobbyCode
			}
		}, JsonOptions), cancellationToken).ConfigureAwait(false);

		var joinResult = await handshake.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
		await client.SendTextAsync(JsonSerializer.Serialize(new {
			type = "lobbyStateRequest",
			payload = new {
				lobbyCode = joinResult.LobbyCode
			}
		}, JsonOptions), cancellationToken).ConfigureAwait(false);
		await lobbyStateSync.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
		var joined = announcement with {
			AnnouncedAt = DateTimeOffset.UtcNow,
			IsLocalHost = false,
			LobbyCode = joinResult.LobbyCode,
			PlayerCount = joinResult.PlayerCount,
			MaxPlayers = MaxLobbyPlayers
		};

		lock (Sync) {
			_currentLanSession = joined;
			LanAnnouncements[joined.Key] = joined;
		}

		AppLog.Info("Network", $"Joined LAN session '{joined.SessionName}' at {joined.Address} as '{normalizedPlayerName}' in slot {joinResult.AssignedSlotIndex + 1}");
		return joined;
	}

	public static async Task SendLobbyChatMessageAsync(string message, CancellationToken cancellationToken = default) {
		if (string.IsNullOrWhiteSpace(message)) {
			return;
		}

		await SendLobbyChannelMessageAsync("chat", message.Trim(), null, cancellationToken).ConfigureAwait(false);
	}

	public static async Task SendLobbyChannelMessageAsync(string channel, string? text = null, object? data = null, CancellationToken cancellationToken = default) {
		if (string.IsNullOrWhiteSpace(channel)) {
			throw new ArgumentException("Channel is required.", nameof(channel));
		}

		var normalizedChannel = channel.Trim();
		var dataElement = data is null ? (JsonElement?)null : JsonSerializer.SerializeToElement(data, JsonOptions);

		LanGameClientComponent? client;
		HostLobbyState? hostLobby;
		LanGameServerComponent? server;
		string? senderName;
		int? senderSlot;
		lock (Sync) {
			client = _lanClient;
			hostLobby = _hostLobbyState;
			server = _lanServer;
			senderName = _localLobbyPlayerName;
			senderSlot = _localLobbySlotIndex;
		}

		if (hostLobby is not null && server is not null) {
			if (senderSlot is null) {
				throw new InvalidOperationException("Local host player is not assigned to a lobby slot.");
			}

			await BroadcastChannelMessageAsync(server, hostLobby, senderSlot.Value, senderName ?? DefaultPlayerName, normalizedChannel, text, dataElement, cancellationToken)
				.ConfigureAwait(false);
			return;
		}

		if (client is null || !client.IsConnected) {
			throw new InvalidOperationException("No active LAN lobby connection exists.");
		}

		await client.SendTextAsync(CreateLobbyChannelJson(normalizedChannel, text, dataElement), cancellationToken).ConfigureAwait(false);
	}

	public static async Task ShutdownLanSessionAsync() {
		LanGameClientComponent? client;
		Task? receiveLoop;
		UdpWebSocketAnnouncer? announcer;
		LanGameServerComponent? server;

		lock (Sync) {
			client = _lanClient;
			receiveLoop = _lanClientReceiveLoopTask;
			announcer = _lanAnnouncer;
			server = _lanServer;
			_lanClient = null;
			_lanClientReceiveLoopTask = null;
			_lanAnnouncer = null;
			_lanServer = null;
			_hostLobbyState = null;
			_pendingLobbyJoin = null;
			_pendingLobbyStateSync = null;
			_currentLanSession = null;
			_currentLanLobbyState = null;
			_localLobbyPlayerName = null;
			_localLobbySlotIndex = null;
		}

		if (client is not null) {
			client.TextMessageReceived -= HandleLanClientTextMessageReceived;
			await client.DisposeAsync().ConfigureAwait(false);
		}

		if (receiveLoop is not null) {
			try {
				await receiveLoop.ConfigureAwait(false);
			} catch (OperationCanceledException) {
			} catch (WebSocketException) {
			}
		}

		if (announcer is not null) {
			await announcer.DisposeAsync().ConfigureAwait(false);
		}

		if (server is not null) {
			server.ClientDisconnected -= HandleLanServerClientDisconnected;
			server.TextMessageReceived -= HandleLanServerTextMessageReceived;
			await server.ShutdownAsync("LAN session shutting down").ConfigureAwait(false);
			await server.DisposeAsync().ConfigureAwait(false);
		}
	}

	private static async Task RunLanDiscoveryLoopAsync(CancellationToken cancellationToken) {
		try {
			using var udpClient = CreateLanDiscoveryClient();

			while (!cancellationToken.IsCancellationRequested) {
				var result = await udpClient.ReceiveAsync(cancellationToken).ConfigureAwait(false);
				ProcessLanAnnouncement(result);
			}
		} catch (OperationCanceledException) {
		} catch (ObjectDisposedException) {
		} catch (Exception ex) {
			AppLog.Error("Network", "LAN discovery loop failed.", ex);
		}
	}

	private static UdpClient CreateLanDiscoveryClient() {
		var udpClient = new UdpClient(AddressFamily.InterNetwork);
		udpClient.EnableBroadcast = true;
		udpClient.Client.ExclusiveAddressUse = false;
		udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
		udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, DefaultLanUdpPort));
		return udpClient;
	}

	private static void ProcessLanAnnouncement(UdpReceiveResult result) {
		JsonDocument document;
		try {
			var json = Encoding.UTF8.GetString(result.Buffer);
			document = JsonDocument.Parse(json);
		} catch (Exception ex) when (ex is JsonException or DecoderFallbackException) {
			return;
		}

		using (document) {
			var root = document.RootElement;
			var type = root.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
			if (!string.Equals(type, "webSocketGameServer", StringComparison.OrdinalIgnoreCase)) {
				return;
			}

			var name = root.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
			var ip = root.TryGetProperty("ip", out var ipElement) ? ipElement.GetString() : null;
			var port = root.TryGetProperty("port", out var portElement) && portElement.TryGetInt32(out var parsedPort)
				? parsedPort
				: DefaultLanWebSocketPort;
			var path = root.TryGetProperty("path", out var pathElement) ? pathElement.GetString() : DefaultLanWebSocketPath;
			var address = root.TryGetProperty("address", out var addressElement) ? addressElement.GetString() : null;
			var lobbyCode = root.TryGetProperty("lobbyCode", out var lobbyCodeElement) ? lobbyCodeElement.GetString() : null;
			var playerCount = root.TryGetProperty("playerCount", out var playerCountElement) && playerCountElement.TryGetInt32(out var parsedPlayerCount)
				? parsedPlayerCount
				: 0;
			var maxPlayers = root.TryGetProperty("maxPlayers", out var maxPlayersElement) && maxPlayersElement.TryGetInt32(out var parsedMaxPlayers)
				? parsedMaxPlayers
				: 0;

			if (string.IsNullOrWhiteSpace(name)) {
				return;
			}

			var normalizedPath = NormalizeWebSocketPath(path);
			var resolvedIp = string.IsNullOrWhiteSpace(ip) ? result.RemoteEndPoint.Address.ToString() : ip.Trim();
			var resolvedAddress = string.IsNullOrWhiteSpace(address)
				? BuildWebSocketAddress(resolvedIp, port, normalizedPath)
				: address.Trim();
			var announcement = new LanSessionAnnouncement(
				Key: resolvedAddress,
				SessionName: name.Trim(),
				Address: resolvedAddress,
				HostIp: resolvedIp,
				Port: port,
				Path: normalizedPath,
				AnnouncedAt: DateTimeOffset.UtcNow,
				IsLocalHost: IsLocalAddress(resolvedIp),
				LobbyCode: string.IsNullOrWhiteSpace(lobbyCode) ? null : lobbyCode.Trim(),
				PlayerCount: Math.Max(0, playerCount),
				MaxPlayers: Math.Max(0, maxPlayers));

			lock (Sync) {
				LanAnnouncements[announcement.Key] = announcement;
				PruneExpiredAnnouncements_NoLock(DateTimeOffset.UtcNow);
			}
		}
	}

	private static void PruneExpiredAnnouncements_NoLock(DateTimeOffset utcNow) {
		var expiredKeys = LanAnnouncements
			.Where(x => utcNow - x.Value.AnnouncedAt > LanAnnouncementTtl)
			.Select(x => x.Key)
			.ToArray();
		foreach (var expiredKey in expiredKeys) {
			LanAnnouncements.Remove(expiredKey);
		}
	}

	private static bool IsLocalAddress(string address) {
		if (string.IsNullOrWhiteSpace(address)) {
			return false;
		}

		if (string.Equals(address, IPAddress.Loopback.ToString(), StringComparison.OrdinalIgnoreCase)) {
			return true;
		}

		if (!IPAddress.TryParse(address, out var ipAddress)) {
			return false;
		}

		return NetworkInterface.GetAllNetworkInterfaces()
			.Where(x => x.OperationalStatus == OperationalStatus.Up)
			.SelectMany(x => x.GetIPProperties().UnicastAddresses)
			.Any(x => Equals(x.Address, ipAddress));
	}

	private static string NormalizeWebSocketPath(string? path) {
		if (string.IsNullOrWhiteSpace(path)) {
			return DefaultLanWebSocketPath;
		}

		return path![0] == '/' ? path : "/" + path;
	}

	private static string BuildWebSocketAddress(string hostIp, int port, string path) {
		return $"ws://{hostIp}:{port}{NormalizeWebSocketPath(path)}";
	}

	private static string NormalizePlayerName(string? playerName) {
		var normalized = string.IsNullOrWhiteSpace(playerName) ? DefaultPlayerName : playerName.Trim();
		return normalized.Length <= 32 ? normalized : normalized[..32];
	}

	private static string MakeUniqueLobbyPlayerName_NoLock(HostLobbyState hostLobby, string requestedName, int selectedSlotIndex) {
		if (!hostLobby.Slots.Any(slot =>
			    slot is not null &&
			    slot.SlotIndex != selectedSlotIndex &&
			    slot.IsConnected &&
			    string.Equals(slot.Name, requestedName, StringComparison.OrdinalIgnoreCase))) {
			return requestedName;
		}

		for (var suffix = 2; suffix < 1000; suffix++) {
			var candidate = NormalizePlayerName($"{requestedName}({suffix})");
			if (!hostLobby.Slots.Any(slot =>
				    slot is not null &&
				    slot.SlotIndex != selectedSlotIndex &&
				    slot.IsConnected &&
				    string.Equals(slot.Name, candidate, StringComparison.OrdinalIgnoreCase))) {
				return candidate;
			}
		}

		return NormalizePlayerName($"{requestedName}({Environment.TickCount & 0x7FFF})");
	}

	private static string GenerateLobbyCode() {
		Span<char> buffer = stackalloc char[6];
		for (var index = 0; index < buffer.Length; index++) {
			buffer[index] = LobbyCodeAlphabet[RandomNumberGenerator.GetInt32(LobbyCodeAlphabet.Length)];
		}

		return new string(buffer);
	}

	private static async void HandleLanServerTextMessageReceived(object? sender, WebSocketTextMessageReceivedEventArgs args) {
		try {
			await HandleLanServerTextMessageReceivedAsync(args).ConfigureAwait(false);
		} catch (Exception ex) {
			AppLog.Error("Network", "Failed to handle LAN server message.", ex);
		}
	}

	private static async Task HandleLanServerTextMessageReceivedAsync(WebSocketTextMessageReceivedEventArgs args) {
		JsonDocument document;
		try {
			document = JsonDocument.Parse(args.Message);
		} catch (JsonException) {
			return;
		}

		using (document) {
			var root = document.RootElement;
			var type = root.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
			var payload = root.TryGetProperty("payload", out var payloadElement) ? payloadElement : default;

			switch (type) {
				case "lobbyHello":
					await HandleLobbyHelloAsync(args.ClientId, payload).ConfigureAwait(false);
					break;
				case "lobbyStateRequest":
					await HandleLobbyStateRequestAsync(args.ClientId, payload).ConfigureAwait(false);
					break;
				case "lobbyChannel":
					await HandleRemoteLobbyChannelAsync(args.ClientId, payload).ConfigureAwait(false);
					break;
			}
		}
	}

	private static async void HandleLanServerClientDisconnected(object? sender, WebSocketClientDisconnectedEventArgs args) {
		try {
			await HandleLanServerClientDisconnectedAsync(args.ClientId).ConfigureAwait(false);
		} catch (Exception ex) {
			AppLog.Error("Network", "Failed to update LAN lobby after client disconnect.", ex);
		}
	}

	private static async Task HandleLanServerClientDisconnectedAsync(Guid clientId) {
		LanGameServerComponent? server;
		LanLobbyState? publishedState;
		lock (Sync) {
			server = _lanServer;
			if (server is null || _hostLobbyState is null || !_hostLobbyState.ClientSlots.TryGetValue(clientId, out var slotIndex)) {
				return;
			}

			_hostLobbyState.ClientSlots.Remove(clientId);
			var slot = _hostLobbyState.Slots[slotIndex];
			if (slot is not null) {
				slot.ClientId = null;
				slot.IsConnected = false;
			}

			UpdateLocalLobbyArtifacts_NoLock(_hostLobbyState);
			publishedState = _currentLanLobbyState;
		}

		if (publishedState is not null) {
			LanLobbyStateChanged?.Invoke(publishedState);
		}

		if (server is not null) {
			await BroadcastLobbyStateAsync(server).ConfigureAwait(false);
		}
	}

	private static async Task HandleLobbyHelloAsync(Guid clientId, JsonElement payload) {
		LanGameServerComponent? server;
		string? rejectionReason = null;
		LanLobbyState? publishedState = null;
		string? welcomeMessage = null;

		lock (Sync) {
			server = _lanServer;
			var hostLobby = _hostLobbyState;
			if (server is null || hostLobby is null) {
				rejectionReason = "Lobby is no longer available.";
			} else {
				var lobbyCode = payload.TryGetProperty("lobbyCode", out var lobbyCodeElement) ? lobbyCodeElement.GetString() : null;
				if (!string.IsNullOrWhiteSpace(lobbyCode) &&
				    !string.Equals(lobbyCode, hostLobby.LobbyCode, StringComparison.OrdinalIgnoreCase)) {
					rejectionReason = "Lobby code does not match the active session.";
				} else {
					var requestedName = NormalizePlayerName(payload.TryGetProperty("playerName", out var nameElement) ? nameElement.GetString() : null);
					var occupiedSlotByOther = hostLobby.ClientSlots.TryGetValue(clientId, out var existingSlotIndex)
						? existingSlotIndex
						: -1;

					var selectedSlotIndex = existingSlotIndex >= 0 ? existingSlotIndex : -1;
					if (selectedSlotIndex < 0) {
						for (var index = 0; index < hostLobby.Slots.Length; index++) {
							var candidate = hostLobby.Slots[index];
							if (candidate is not null &&
							    !candidate.IsConnected &&
							    string.Equals(candidate.Name, requestedName, StringComparison.OrdinalIgnoreCase)) {
								selectedSlotIndex = index;
								break;
							}
						}
					}

					if (selectedSlotIndex < 0) {
						for (var index = 0; index < hostLobby.Slots.Length; index++) {
							if (hostLobby.Slots[index] is null) {
								selectedSlotIndex = index;
								break;
							}
						}
					}

					if (selectedSlotIndex < 0) {
						rejectionReason = "Lobby is full.";
					} else {
						var assignedName = existingSlotIndex >= 0
							? requestedName
							: MakeUniqueLobbyPlayerName_NoLock(hostLobby, requestedName, selectedSlotIndex);

						if (hostLobby.ClientSlots.TryGetValue(clientId, out var previousSlotIndex) && previousSlotIndex != selectedSlotIndex) {
							hostLobby.ClientSlots.Remove(clientId);
						}

						if (hostLobby.Slots[selectedSlotIndex] is null) {
							hostLobby.Slots[selectedSlotIndex] = new MutableLobbyPlayerSlot {
								SlotIndex = selectedSlotIndex,
								Name = assignedName,
								IsHost = false,
								IsLocal = false,
								JoinedAt = DateTimeOffset.UtcNow
							};
						}

						var slot = hostLobby.Slots[selectedSlotIndex]!;
						slot.Name = assignedName;
						slot.IsConnected = true;
						slot.ClientId = clientId;
						hostLobby.ClientSlots[clientId] = slot.SlotIndex;

						UpdateLocalLobbyArtifacts_NoLock(hostLobby);
						publishedState = _currentLanLobbyState;
						welcomeMessage = JsonSerializer.Serialize(new {
							type = "lobbyWelcome",
							payload = new {
								lobbyCode = hostLobby.LobbyCode,
								sessionName = hostLobby.SessionName,
								address = hostLobby.Address,
								maxPlayers = MaxLobbyPlayers,
								assignedSlotIndex = slot.SlotIndex,
								playerName = slot.Name,
								slots = hostLobby.Slots.Where(static slotValue => slotValue is not null).Select(static slotValue => ToWireSlot(slotValue!))
							}
						}, JsonOptions);
					}
				}
			}
		}

		if (server is null) {
			return;
		}

		if (rejectionReason is not null) {
			await server.SendTextAsync(clientId, JsonSerializer.Serialize(new {
				type = "lobbyRejected",
				payload = new {
					reason = rejectionReason
				}
			}, JsonOptions)).ConfigureAwait(false);
			return;
		}

		if (welcomeMessage is not null) {
			await server.SendTextAsync(clientId, welcomeMessage).ConfigureAwait(false);
			await BroadcastLobbyStateAsync(server).ConfigureAwait(false);
		}

		if (publishedState is not null) {
			LanLobbyStateChanged?.Invoke(publishedState);
		}
	}

	private static async Task HandleRemoteLobbyChannelAsync(Guid clientId, JsonElement payload) {
		LanGameServerComponent? server;
		HostLobbyState? hostLobby;
		string? senderName;
		int senderSlotIndex;
		string? channel;
		string? text = payload.TryGetProperty("text", out var textElement) ? textElement.GetString() : null;
		JsonElement? data = payload.TryGetProperty("data", out var dataElement) ? dataElement.Clone() : (JsonElement?)null;

		lock (Sync) {
			server = _lanServer;
			hostLobby = _hostLobbyState;
			if (server is null || hostLobby is null || !hostLobby.ClientSlots.TryGetValue(clientId, out senderSlotIndex)) {
				return;
			}

			channel = payload.TryGetProperty("channel", out var channelElement) ? channelElement.GetString() : null;
			senderName = hostLobby.Slots[senderSlotIndex]?.Name;
		}

		if (string.IsNullOrWhiteSpace(channel) || string.IsNullOrWhiteSpace(senderName)) {
			return;
		}

		await BroadcastChannelMessageAsync(server!, hostLobby!, senderSlotIndex, senderName!, channel!.Trim(), text, data, CancellationToken.None)
			.ConfigureAwait(false);
	}

	private static async Task HandleLobbyStateRequestAsync(Guid clientId, JsonElement payload) {
		LanGameServerComponent? server;
		string? message = null;
		lock (Sync) {
			server = _lanServer;
			if (server is null || _hostLobbyState is null || !_hostLobbyState.ClientSlots.ContainsKey(clientId)) {
				return;
			}

			var requestedLobbyCode = payload.TryGetProperty("lobbyCode", out var lobbyCodeElement) ? lobbyCodeElement.GetString() : null;
			if (!string.IsNullOrWhiteSpace(requestedLobbyCode) &&
			    !string.Equals(requestedLobbyCode, _hostLobbyState.LobbyCode, StringComparison.OrdinalIgnoreCase)) {
				return;
			}

			message = CreateLobbyStateJson_NoLock(_hostLobbyState);
		}

		await server!.SendTextAsync(clientId, message!).ConfigureAwait(false);
	}

	private static async Task BroadcastLobbyStateAsync(LanGameServerComponent server) {
		string message;
		lock (Sync) {
			if (_hostLobbyState is null) {
				return;
			}

			message = CreateLobbyStateJson_NoLock(_hostLobbyState);
		}

		await server.BroadcastTextAsync(message).ConfigureAwait(false);
	}

	private static async Task BroadcastChannelMessageAsync(
		LanGameServerComponent server,
		HostLobbyState hostLobby,
		int fromSlotIndex,
		string fromPlayerName,
		string channel,
		string? text,
		JsonElement? data,
		CancellationToken cancellationToken) {
		var sentAt = DateTimeOffset.UtcNow;
		string message = CreateLobbyChannelBroadcastJson(channel, fromPlayerName, fromSlotIndex, text, data, sentAt);
		await server.BroadcastTextAsync(message, cancellationToken).ConfigureAwait(false);

		LanLobbyChannelMessage localMessage;
		lock (Sync) {
			if (!ReferenceEquals(_hostLobbyState, hostLobby)) {
				return;
			}

			localMessage = new LanLobbyChannelMessage(channel, fromPlayerName, fromSlotIndex, text, CloneOrNull(data), sentAt, fromSlotIndex == _localLobbySlotIndex);
		}

		LanLobbyChannelMessageReceived?.Invoke(localMessage);
	}

	private static void HandleLanClientTextMessageReceived(object? sender, WebSocketClientTextMessageReceivedEventArgs args) {
		try {
			HandleLanClientTextMessageReceivedCore(args.Message);
		} catch (Exception ex) {
			AppLog.Error("Network", "Failed to handle LAN client message.", ex);
		}
	}

	private static void HandleLanClientTextMessageReceivedCore(string message) {
		JsonDocument document;
		try {
			document = JsonDocument.Parse(message);
		} catch (JsonException) {
			return;
		}

		LanLobbyState? publishedState = null;
		LanLobbyChannelMessage? publishedChannel = null;
		TaskCompletionSource<LobbyJoinHandshake>? handshake = null;
		Exception? handshakeError = null;

		using (document) {
			var root = document.RootElement;
			var type = root.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
			var payload = root.TryGetProperty("payload", out var payloadElement) ? payloadElement : default;

			lock (Sync) {
				switch (type) {
					case "lobbyWelcome":
						var assignedSlotIndex = payload.TryGetProperty("assignedSlotIndex", out var assignedSlotElement) ? assignedSlotElement.GetInt32() : -1;
						var lobbyCode = payload.TryGetProperty("lobbyCode", out var lobbyCodeElement) ? lobbyCodeElement.GetString() ?? string.Empty : string.Empty;
						_localLobbySlotIndex = assignedSlotIndex >= 0 ? assignedSlotIndex : null;
						_localLobbyPlayerName = NormalizePlayerName(payload.TryGetProperty("playerName", out var playerNameElement)
							? playerNameElement.GetString()
							: _localLobbyPlayerName);
						_currentLanLobbyState = ParseLobbyStatePayload_NoLock(payload, _localLobbySlotIndex, _localLobbyPlayerName);
						if (_currentLanLobbyState is not null && _currentLanSession is not null) {
							_currentLanSession = _currentLanSession with {
								LobbyCode = lobbyCode,
								PlayerCount = _currentLanLobbyState.Slots.Count(static slot => slot.IsConnected),
								MaxPlayers = _currentLanLobbyState.MaxPlayers
							};
						}

						publishedState = _currentLanLobbyState;
						handshake = _pendingLobbyJoin;
						_pendingLobbyJoin = null;
						if (_currentLanLobbyState is not null) {
							handshake?.TrySetResult(new LobbyJoinHandshake(
								_localLobbySlotIndex ?? -1,
								_currentLanLobbyState.LobbyCode,
								_currentLanLobbyState.Slots.Count(static slot => slot.IsConnected)));
						}
						break;

					case "lobbyState":
						_currentLanLobbyState = ParseLobbyStatePayload_NoLock(payload, _localLobbySlotIndex, _localLobbyPlayerName);
						if (_currentLanLobbyState is not null && _currentLanSession is not null) {
							_currentLanSession = _currentLanSession with {
								LobbyCode = _currentLanLobbyState.LobbyCode,
								PlayerCount = _currentLanLobbyState.Slots.Count(static slot => slot.IsConnected),
								MaxPlayers = _currentLanLobbyState.MaxPlayers
							};
						}

						publishedState = _currentLanLobbyState;
						_pendingLobbyStateSync?.TrySetResult(true);
						_pendingLobbyStateSync = null;
						break;

					case "lobbyChannel":
						var channel = payload.TryGetProperty("channel", out var channelElement) ? channelElement.GetString() : null;
						var fromPlayerName = payload.TryGetProperty("fromPlayerName", out var fromPlayerNameElement) ? fromPlayerNameElement.GetString() : null;
						var fromSlotIndex = payload.TryGetProperty("fromSlotIndex", out var fromSlotElement) ? fromSlotElement.GetInt32() : -1;
						var text = payload.TryGetProperty("text", out var textElement) ? textElement.GetString() : null;
						JsonElement? data = payload.TryGetProperty("data", out var dataElement) ? dataElement.Clone() : (JsonElement?)null;
						var sentAt = payload.TryGetProperty("sentAt", out var sentAtElement) && sentAtElement.TryGetDateTimeOffset(out var parsedSentAt)
							? parsedSentAt
							: DateTimeOffset.UtcNow;
						if (!string.IsNullOrWhiteSpace(channel) && !string.IsNullOrWhiteSpace(fromPlayerName) && fromSlotIndex >= 0) {
							publishedChannel = new LanLobbyChannelMessage(
								channel.Trim(),
								fromPlayerName!,
								fromSlotIndex,
								text,
								data,
								sentAt,
								fromSlotIndex == _localLobbySlotIndex);
						}
						break;

					case "lobbyRejected":
						handshake = _pendingLobbyJoin;
						_pendingLobbyJoin = null;
						_pendingLobbyStateSync?.TrySetCanceled();
						_pendingLobbyStateSync = null;
						var reason = payload.TryGetProperty("reason", out var reasonElement)
							? reasonElement.GetString() ?? "Lobby rejected the join request."
							: "Lobby rejected the join request.";
						handshakeError = new InvalidOperationException(reason);
						break;
				}
			}
		}

		if (handshakeError is not null) {
			handshake?.TrySetException(handshakeError);
		}

		if (publishedState is not null) {
			LanLobbyStateChanged?.Invoke(publishedState);
		}

		if (publishedChannel is not null) {
			LanLobbyChannelMessageReceived?.Invoke(publishedChannel);
		}
	}

	private static LanLobbyState CreatePublicLobbyState_NoLock(HostLobbyState hostLobby) {
		return new LanLobbyState(
			hostLobby.LobbyCode,
			hostLobby.SessionName,
			hostLobby.Address,
			MaxLobbyPlayers,
			hostLobby.Slots
				.Where(static slot => slot is not null)
				.Select(slot => new LanLobbyPlayerSlot(
					slot!.SlotIndex,
					slot.Name,
					slot.IsHost,
					slot.IsConnected,
					slot.SlotIndex == _localLobbySlotIndex,
					slot.JoinedAt))
				.OrderBy(static slot => slot.SlotIndex)
				.ToArray());
	}

	private static string CreateLobbyStateJson_NoLock(HostLobbyState hostLobby) {
		return JsonSerializer.Serialize(new {
			type = "lobbyState",
			payload = new {
				lobbyCode = hostLobby.LobbyCode,
				sessionName = hostLobby.SessionName,
				address = hostLobby.Address,
				maxPlayers = MaxLobbyPlayers,
				slots = hostLobby.Slots.Where(static slot => slot is not null).Select(static slot => ToWireSlot(slot!)).ToArray()
			}
		}, JsonOptions);
	}

	private static LanLobbyState? ParseLobbyStatePayload_NoLock(JsonElement payload, int? localSlotIndex, string? localPlayerName) {
		if (!payload.TryGetProperty("lobbyCode", out var lobbyCodeElement) ||
		    !payload.TryGetProperty("sessionName", out var sessionNameElement) ||
		    !payload.TryGetProperty("address", out var addressElement) ||
		    !payload.TryGetProperty("slots", out var slotsElement) ||
		    slotsElement.ValueKind != JsonValueKind.Array) {
			return null;
		}

		var lobbyCode = lobbyCodeElement.GetString();
		var sessionName = sessionNameElement.GetString();
		var address = addressElement.GetString();
		var maxPlayers = payload.TryGetProperty("maxPlayers", out var maxPlayersElement) && maxPlayersElement.TryGetInt32(out var parsedMaxPlayers)
			? parsedMaxPlayers
			: MaxLobbyPlayers;

		if (string.IsNullOrWhiteSpace(lobbyCode) || string.IsNullOrWhiteSpace(sessionName) || string.IsNullOrWhiteSpace(address)) {
			return null;
		}

		var slots = new List<LanLobbyPlayerSlot>();
		foreach (var slotElement in slotsElement.EnumerateArray()) {
			if (!slotElement.TryGetProperty("slotIndex", out var slotIndexElement) ||
			    !slotIndexElement.TryGetInt32(out var slotIndex) ||
			    !slotElement.TryGetProperty("name", out var nameElement)) {
				continue;
			}

			var name = nameElement.GetString() ?? string.Empty;
			var isHost = slotElement.TryGetProperty("isHost", out var isHostElement) && isHostElement.GetBoolean();
			var isConnected = slotElement.TryGetProperty("isConnected", out var isConnectedElement) && isConnectedElement.GetBoolean();
			var joinedAt = slotElement.TryGetProperty("joinedAt", out var joinedAtElement) && joinedAtElement.TryGetDateTimeOffset(out var parsedJoinedAt)
				? parsedJoinedAt
				: DateTimeOffset.UtcNow;
			var isLocal = localSlotIndex == slotIndex ||
			              (!string.IsNullOrWhiteSpace(localPlayerName) && string.Equals(localPlayerName, name, StringComparison.OrdinalIgnoreCase));
			slots.Add(new LanLobbyPlayerSlot(slotIndex, name, isHost, isConnected, isLocal, joinedAt));
		}

		return new LanLobbyState(lobbyCode.Trim(), sessionName.Trim(), address.Trim(), maxPlayers, slots.OrderBy(static slot => slot.SlotIndex).ToArray());
	}

	private static void UpdateLocalLobbyArtifacts_NoLock(HostLobbyState hostLobby) {
		var connectedCount = hostLobby.Slots.Count(static slot => slot is not null && slot.IsConnected);
		_currentLanLobbyState = CreatePublicLobbyState_NoLock(hostLobby);
		if (_currentLanSession is not null) {
			_currentLanSession = _currentLanSession with {
				AnnouncedAt = DateTimeOffset.UtcNow,
				PlayerCount = connectedCount,
				MaxPlayers = MaxLobbyPlayers,
				LobbyCode = hostLobby.LobbyCode
			};
			LanAnnouncements[_currentLanSession.Key] = _currentLanSession;
		}

		if (_lanAnnouncer is not null) {
			_lanAnnouncer.Options.PlayerCount = connectedCount;
			_lanAnnouncer.Options.MaxPlayers = MaxLobbyPlayers;
			_lanAnnouncer.Options.LobbyCode = hostLobby.LobbyCode;
		}
	}

	private static object ToWireSlot(MutableLobbyPlayerSlot slot) {
		return new {
			slotIndex = slot.SlotIndex,
			name = slot.Name,
			isHost = slot.IsHost,
			isConnected = slot.IsConnected,
			joinedAt = slot.JoinedAt
		};
	}

	private static string CreateLobbyChannelJson(string channel, string? text, JsonElement? data) {
		return JsonSerializer.Serialize(new {
			type = "lobbyChannel",
			payload = new {
				channel,
				text,
				data = ConvertJsonElement(data)
			}
		}, JsonOptions);
	}

	private static string CreateLobbyChannelBroadcastJson(string channel, string fromPlayerName, int fromSlotIndex, string? text, JsonElement? data, DateTimeOffset sentAt) {
		return JsonSerializer.Serialize(new {
			type = "lobbyChannel",
			payload = new {
				channel,
				fromPlayerName,
				fromSlotIndex,
				text,
				data = ConvertJsonElement(data),
				sentAt
			}
		}, JsonOptions);
	}

	private static object? ConvertJsonElement(JsonElement? data) {
		if (data is null) {
			return null;
		}

		return JsonSerializer.Deserialize<object>(data.Value.GetRawText(), JsonOptions);
	}

	private static JsonElement? CloneOrNull(JsonElement? data) {
		return data is null ? null : data.Value.Clone();
	}

	private sealed class HostLobbyState(string sessionName, string address, string lobbyCode) {
		public string SessionName { get; } = sessionName;
		public string Address { get; } = address;
		public string LobbyCode { get; } = lobbyCode;
		public MutableLobbyPlayerSlot?[] Slots { get; } = new MutableLobbyPlayerSlot?[MaxLobbyPlayers];
		public Dictionary<Guid, int> ClientSlots { get; } = new();
	}

	private sealed class MutableLobbyPlayerSlot {
		public required int SlotIndex { get; init; }
		public required string Name { get; set; }
		public required bool IsHost { get; init; }
		public required bool IsLocal { get; init; }
		public required DateTimeOffset JoinedAt { get; init; }
		public bool IsConnected { get; set; }
		public Guid? ClientId { get; set; }
	}

	private sealed record LobbyJoinHandshake(int AssignedSlotIndex, string LobbyCode, int PlayerCount);
}
