using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay;

public sealed record NetworkAddressInfo(string PublicIp, IReadOnlyList<string> LocalIpv4Addresses);

public static class NetworkService {

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
}
