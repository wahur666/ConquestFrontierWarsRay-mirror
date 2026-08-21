namespace ConquestFrontierWarsRay.Framework.TestApp.TalkingHead;

internal sealed class TalkingHeadClip : IDisposable {
	private readonly IReadOnlyList<IDisposable> _ownedResources;

	public TalkingHeadClip(
		SpriteFrames faceFrames,
		AudioStreamResource voiceAudio,
		IReadOnlyList<TalkingHeadTimingRow> timeline,
		SpriteFrames? fuzzFrames = null,
		SpriteFrames? borderFrames = null,
		IReadOnlyList<IDisposable>? ownedResources = null,
		int fuzzFps = 30,
		float legacyPortraitWidth = 62f,
		float legacyPortraitHeight = 79f,
		float borderLeftInset = 5f,
		float borderTopInset = 8f,
		float borderRightInset = 13f,
		float borderBottomInset = 2f) {
		FaceFrames = faceFrames ?? throw new ArgumentNullException(nameof(faceFrames));
		VoiceAudio = voiceAudio ?? throw new ArgumentNullException(nameof(voiceAudio));
		Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
		FuzzFrames = fuzzFrames;
		BorderFrames = borderFrames;
		_ownedResources = ownedResources ?? [];
		FuzzFps = fuzzFps;
		LegacyPortraitWidth = legacyPortraitWidth;
		LegacyPortraitHeight = legacyPortraitHeight;
		BorderLeftInset = borderLeftInset;
		BorderTopInset = borderTopInset;
		BorderRightInset = borderRightInset;
		BorderBottomInset = borderBottomInset;
	}

	public SpriteFrames FaceFrames { get; }
	public AudioStreamResource VoiceAudio { get; }
	public IReadOnlyList<TalkingHeadTimingRow> Timeline { get; }
	public SpriteFrames? FuzzFrames { get; }
	public SpriteFrames? BorderFrames { get; }
	public int FuzzFps { get; }
	public float LegacyPortraitWidth { get; }
	public float LegacyPortraitHeight { get; }
	public float BorderLeftInset { get; }
	public float BorderTopInset { get; }
	public float BorderRightInset { get; }
	public float BorderBottomInset { get; }

	public bool HasFuzz => FuzzFrames is not null && FuzzFrames.Count > 0;
	public bool HasBorder => BorderFrames is not null && BorderFrames.Count > 0;

	public void Dispose() {
		for (var i = _ownedResources.Count - 1; i >= 0; i--) {
			_ownedResources[i].Dispose();
		}
	}
}

internal readonly record struct TalkingHeadTimingRow(int SourceFrameIndex);
