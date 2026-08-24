using SharpGen.Runtime;
using Vortice.MediaFoundation;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Opens Media Foundation-backed video sources from file paths.
/// </summary>
public sealed class VideoResourceManager : IVideoResourceManager {
	private readonly IResourceLocator _locator;

	public VideoResourceManager(IResourceLocator locator) {
		_locator = locator ?? throw new ArgumentNullException(nameof(locator));
	}

	/// <inheritdoc />
	public VideoSource OpenFile(string path) {
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		var fullPath = Path.GetFullPath(path);
		var lease = MediaFoundationRuntime.Acquire();
		IMFSourceReader? reader = null;
		AudioStreamResource? embeddedAudio = null;

		try {
			using var attributes = MediaFactory.MFCreateAttributes(1);
			attributes.Set(SourceReaderAttributeKeys.EnableVideoProcessing, true).CheckError();

			reader = MediaFactory.MFCreateSourceReaderFromURL(fullPath, attributes);
			reader.SetStreamSelection(SourceReaderIndex.AllStreams, false);
			reader.SetStreamSelection(SourceReaderIndex.FirstVideoStream, true);

			using var outputType = MediaFactory.MFCreateMediaType();
			outputType.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Video).CheckError();
			outputType.Set(MediaTypeAttributeKeys.Subtype, VideoFormatGuids.Rgb32).CheckError();
			reader.SetCurrentMediaType(SourceReaderIndex.FirstVideoStream, outputType);

			using var currentType = reader.GetCurrentMediaType(SourceReaderIndex.FirstVideoStream);
			var packedFrameSize = currentType.GetUInt64(MediaTypeAttributeKeys.FrameSize);
			var width = (int)(packedFrameSize >> 32);
			var height = (int)(packedFrameSize & 0xffffffff);
			var stride = width * 4;
			var frameRate = ReadPackedRatio(currentType, MediaTypeAttributeKeys.FrameRate);

			var strideResult = currentType.GetUInt32(MediaTypeAttributeKeys.DefaultStride, out var mediaStride);
			if (strideResult.Success) {
				stride = unchecked((int)mediaStride);
			}

			if (width <= 0 || height <= 0) {
				throw new InvalidOperationException($"Could not read a valid video size from '{fullPath}'.");
			}

			embeddedAudio = TryCreateEmbeddedAudio(fullPath);
			return new VideoSource(
				fullPath,
				reader,
				embeddedAudio,
				width,
				height,
				stride,
				frameRate,
				ReadDurationSeconds(reader),
				lease);
		} catch {
			embeddedAudio?.Dispose();
			reader?.Dispose();
			lease.Dispose();
			throw;
		}
	}

	/// <inheritdoc />
	public VideoSource OpenMovie(string moviePath) {
		ArgumentException.ThrowIfNullOrWhiteSpace(moviePath);
		return OpenFile(_locator.ResolveMoviePath(moviePath));
	}

	private static double ReadPackedRatio(IMFAttributes attributes, Guid key) {
		var result = attributes.GetUInt64(key, out var packedRatio);
		if (result.Failure) {
			return 0d;
		}

		var numerator = (uint)(packedRatio >> 32);
		var denominator = (uint)(packedRatio & 0xffffffff);
		return denominator == 0 ? 0d : (double)numerator / denominator;
	}

	private static double ReadDurationSeconds(IMFSourceReader reader) {
		try {
			var duration = reader.GetPresentationAttribute(SourceReaderIndex.MediaSource, PresentationDescriptionAttributeKeys.Duration);
			var ticks = Convert.ToInt64(duration.Value);
			return ticks <= 0 ? 0d : ticks / 10_000_000d;
		} catch {
			return 0d;
		}
	}

	private static AudioStreamResource? TryCreateEmbeddedAudio(string path) {
		try {
			var audio = new MediaFoundationAudioStreamResource(path);
			_ = audio.TimeLength;
			return audio;
		} catch {
			return null;
		}
	}
}
