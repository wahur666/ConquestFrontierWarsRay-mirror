using System;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay;

public static class NetworkService {

	public static async Task GetNetworkAddresses() {
		using var httpClient = new HttpClient();
		var publicIp = await httpClient.GetStringAsync("https://api.ipify.org");
		AppLog.Info("Menu", $"My public IP Address is: {publicIp}");
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
			.ToArray();
		foreach (var ip in addresses)
			AppLog.Info("Menu", $"Local ip address ${ip}");
	}
	
}
