using System.Text;

namespace ConquestFrontierWarsRay.Framework;

public enum AppLogLevel {
	Debug,
	Info,
	Warning,
	Error
}

public sealed class AppLogSession : IDisposable {
	private readonly object _sync = new();
	private readonly StreamWriter _writer;
	private bool _disposed;

	internal AppLogSession(string filePath, bool mirrorToConsole) {
		FilePath = filePath;
		MirrorToConsole = mirrorToConsole;
		Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? AppContext.BaseDirectory);
		_writer = new StreamWriter(new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.UTF8) {
			AutoFlush = true
		};
	}

	public string FilePath { get; }

	public bool MirrorToConsole { get; }

	internal void Write(AppLogLevel level, string category, string message, Exception? exception = null) {
		ArgumentException.ThrowIfNullOrWhiteSpace(category);
		ArgumentException.ThrowIfNullOrWhiteSpace(message);

		var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz");
		var line = $"{timestamp} [{level}] [{category}] {message}";

		lock (_sync) {
			ObjectDisposedException.ThrowIf(_disposed, this);

			_writer.WriteLine(line);
			if (exception is not null) {
				_writer.WriteLine(exception);
			}
		}

		if (!MirrorToConsole) {
			return;
		}

		if (level >= AppLogLevel.Error) {
			Console.Error.WriteLine(line);
			if (exception is not null) {
				Console.Error.WriteLine(exception);
			}
		} else {
			Console.WriteLine(line);
			if (exception is not null) {
				Console.WriteLine(exception);
			}
		}
	}

	public void Dispose() {
		lock (_sync) {
			if (_disposed) {
				return;
			}

			_disposed = true;
			_writer.Dispose();
		}
	}
}

public static class AppLog {
	private static readonly object Sync = new();
	private static AppLogSession? _session;

	public static bool IsConfigured {
		get {
			lock (Sync) {
				return _session is not null;
			}
		}
	}

	public static string? FilePath {
		get {
			lock (Sync) {
				return _session?.FilePath;
			}
		}
	}

	public static AppLogSession ConfigureFile(string filePath, bool mirrorToConsole = true) {
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

		lock (Sync) {
			_session?.Dispose();
			_session = new AppLogSession(filePath, mirrorToConsole);
			_session.Write(AppLogLevel.Info, "AppLog", $"Logging started. File='{filePath}'.");
			return _session;
		}
	}

	public static void Close() {
		lock (Sync) {
			_session?.Write(AppLogLevel.Info, "AppLog", "Logging stopped.");
			_session?.Dispose();
			_session = null;
		}
	}

	public static void Debug(string category, string message) {
		Write(AppLogLevel.Debug, category, message);
	}

	public static void Info(string category, string message) {
		Write(AppLogLevel.Info, category, message);
	}

	public static void Warning(string category, string message) {
		Write(AppLogLevel.Warning, category, message);
	}

	public static void Error(string category, string message, Exception? exception = null) {
		Write(AppLogLevel.Error, category, message, exception);
	}

	private static void Write(AppLogLevel level, string category, string message, Exception? exception = null) {
		lock (Sync) {
			if (_session is not null) {
				_session.Write(level, category, message, exception);
				return;
			}
		}

		var fallback = $"[{level}] [{category}] {message}";
		if (level >= AppLogLevel.Error) {
			Console.Error.WriteLine(fallback);
			if (exception is not null) {
				Console.Error.WriteLine(exception);
			}
		} else {
			Console.WriteLine(fallback);
			if (exception is not null) {
				Console.WriteLine(exception);
			}
		}
	}
}
