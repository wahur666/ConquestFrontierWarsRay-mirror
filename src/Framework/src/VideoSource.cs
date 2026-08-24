using Vortice.MediaFoundation;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// One disposable video playback source session with immutable metadata.
/// </summary>
public sealed class VideoSource : IDisposable {
	private bool _disposed;

	internal VideoSource(
		string sourcePath,
		IMFSourceReader reader,
		AudioStreamResource? embeddedAudio,
		int width,
		int height,
		int stride,
		double frameRate,
		double durationSeconds,
		MediaFoundationRuntime.Lease runtimeLease) {
		SourcePath = sourcePath;
		Reader = reader;
		EmbeddedAudio = embeddedAudio;
		Width = width;
		Height = height;
		Stride = stride;
		FrameRate = frameRate;
		DurationSeconds = durationSeconds;
		RuntimeLease = runtimeLease;
	}

	public string SourcePath { get; }

	public int Width { get; }

	public int Height { get; }

	public int Stride { get; }

	public bool SourceIsTopDown => Stride < 0;

	public double FrameRate { get; }

	public double DurationSeconds { get; }

	public AudioStreamResource? EmbeddedAudio { get; }

	internal IMFSourceReader Reader { get; }

	internal MediaFoundationRuntime.Lease RuntimeLease { get; private set; }

	public void Dispose() {
		if (_disposed) {
			return;
		}

		_disposed = true;
		EmbeddedAudio?.Dispose();
		Reader.Dispose();
		RuntimeLease.Dispose();
		RuntimeLease = default;
	}
}
