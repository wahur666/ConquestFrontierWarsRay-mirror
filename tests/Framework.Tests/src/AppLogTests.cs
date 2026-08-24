using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class AppLogTests {
	[Fact]
	public void ConfigureFile_WritesMessagesToLogFile() {
		var logDirectory = Path.Combine(Path.GetTempPath(), "ConquestFrontierWarsRay.Tests", Guid.NewGuid().ToString("N"));
		var logPath = Path.Combine(logDirectory, "app.log");

		AppLog.ConfigureFile(logPath, mirrorToConsole: false);
		AppLog.Info("AppLogTests", "hello world");
		AppLog.Close();

		Assert.True(File.Exists(logPath));
		var contents = File.ReadAllText(logPath);
		Assert.Contains("[Info] [AppLogTests] hello world", contents);
	}
}
