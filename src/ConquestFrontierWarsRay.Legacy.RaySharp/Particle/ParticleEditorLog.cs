namespace RaySharp.Particle;

internal sealed class ParticleEditorLog {
	private const int MaxEntries = 80;
	private readonly List<string> _entries = [];
	private readonly string _snapshotLogPath = Path.Combine(AppContext.BaseDirectory, "logs", "particle-snapshots.log");

	public IReadOnlyList<string> Entries => _entries;

	public void Info(string message) {
		Add("INFO", message);
	}

	public void Warning(string message) {
		Add("WARN", message);
	}

	public void Error(string message) {
		Add("ERR", message);
	}

	public void Snapshot(ParticleSceneSnapshot snapshot) {
		string json = snapshot.ToJson();
		Directory.CreateDirectory(Path.GetDirectoryName(_snapshotLogPath)!);
		File.AppendAllText(_snapshotLogPath, json + Environment.NewLine + Environment.NewLine);
		Add("SNAP", $"{snapshot.Reason} at {snapshot.CapturedAt:O}; file='{snapshot.DisplayName}'; particles={snapshot.System.ActiveParticles}; written to {_snapshotLogPath}");
		Console.WriteLine("[ParticleEditorSnapshot]");
		Console.WriteLine(json);
	}

	private void Add(string level, string message) {
		string line = $"{DateTime.Now:HH:mm:ss} {level} {message}";
		_entries.Add(line);
		if (_entries.Count > MaxEntries) {
			_entries.RemoveAt(0);
		}

		Console.WriteLine($"[ParticleEditor] {line}");
	}
}
