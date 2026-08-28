using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
	bool IsLocalHost);

public static class NetworkService {
	private const int DefaultLanUdpPort = 7778;
	private const int DefaultLanWebSocketPort = 7777;
	private const string DefaultLanWebSocketPath = "/game";
	private static readonly TimeSpan LanAnnouncementTtl = TimeSpan.FromSeconds(6);
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
	private static readonly Lock Sync = new();
	private static readonly Dictionary<string, LanSessionAnnouncement> LanAnnouncements = new(StringComparer.OrdinalIgnoreCase);
	private static CancellationTokenSource? _lanDiscoveryCts;
	private static Task? _lanDiscoveryTask;
	private static WebSocketGameServer? _lanServer;
	private static UdpWebSocketAnnouncer? _lanAnnouncer;
	private static WebSocketGameClient? _lanClient;
	private static LanSessionAnnouncement? _currentLanSession;

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

	public static async Task<LanSessionAnnouncement> HostLanSessionAsync(string sessionName, CancellationToken cancellationToken = default) {
		if (string.IsNullOrWhiteSpace(sessionName)) {
			throw new ArgumentException("Session name is required.", nameof(sessionName));
		}

		StopLanSessionDiscovery();
		await ShutdownLanSessionAsync().ConfigureAwait(false);

		var addresses = await GetNetworkAddressesAsync().ConfigureAwait(false);
		var hostIp = addresses.LocalIpv4Addresses.FirstOrDefault(static ip => !string.IsNullOrWhiteSpace(ip)) ?? IPAddress.Loopback.ToString();
		var normalizedPath = NormalizeWebSocketPath(DefaultLanWebSocketPath);
		var address = BuildWebSocketAddress(hostIp, DefaultLanWebSocketPort, normalizedPath);

		var server = new WebSocketGameServer(new WebSocketGameServerOptions {
			Host = hostIp,
			Port = DefaultLanWebSocketPort,
			Path = normalizedPath
		});
		server.Start();

		var announcer = new UdpWebSocketAnnouncer(new UdpWebSocketAnnouncerOptions {
			Name = sessionName.Trim(),
			UdpPort = DefaultLanUdpPort,
			WebSocketIp = hostIp,
			WebSocketPort = DefaultLanWebSocketPort,
			WebSocketPath = normalizedPath
		});
		announcer.Start();

		var announcement = new LanSessionAnnouncement(
			Key: address,
			SessionName: sessionName.Trim(),
			Address: address,
			HostIp: hostIp,
			Port: DefaultLanWebSocketPort,
			Path: normalizedPath,
			AnnouncedAt: DateTimeOffset.UtcNow,
			IsLocalHost: true);

		lock (Sync) {
			_lanServer = server;
			_lanAnnouncer = announcer;
			_lanClient = null;
			_currentLanSession = announcement;
			LanAnnouncements[announcement.Key] = announcement;
		}

		AppLog.Info("Network", $"Hosting LAN session '{announcement.SessionName}' at {announcement.Address}");
		return announcement;
	}

	public static async Task<LanSessionAnnouncement> JoinLanSessionAsync(LanSessionAnnouncement announcement, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(announcement);
		if (IsHostingLanSession) {
			throw new InvalidOperationException("Cannot join a LAN session while this process is hosting one.");
		}

		WebSocketGameClient? existingClient;
		lock (Sync) {
			existingClient = _lanClient;
			_lanClient = null;
			_currentLanSession = null;
		}

		if (existingClient is not null) {
			await existingClient.DisposeAsync().ConfigureAwait(false);
		}

		var client = new WebSocketGameClient(new WebSocketGameClientOptions {
			Address = announcement.Address,
			UdpDiscover = false
		});
		await client.ConnectAsync(cancellationToken).ConfigureAwait(false);

		var joined = announcement with { AnnouncedAt = DateTimeOffset.UtcNow, IsLocalHost = false };
		lock (Sync) {
			_lanClient = client;
			_currentLanSession = joined;
			LanAnnouncements[joined.Key] = joined;
		}

		AppLog.Info("Network", $"Joined LAN session '{joined.SessionName}' at {joined.Address}");
		return joined;
	}

	public static async Task ShutdownLanSessionAsync() {
		WebSocketGameClient? client;
		UdpWebSocketAnnouncer? announcer;
		WebSocketGameServer? server;

		lock (Sync) {
			client = _lanClient;
			announcer = _lanAnnouncer;
			server = _lanServer;
			_lanClient = null;
			_lanAnnouncer = null;
			_lanServer = null;
			_currentLanSession = null;
		}

		if (client is not null) {
			await client.DisposeAsync().ConfigureAwait(false);
		}

		if (announcer is not null) {
			await announcer.DisposeAsync().ConfigureAwait(false);
		}

		if (server is not null) {
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
				IsLocalHost: IsLocalAddress(resolvedIp));

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
}
