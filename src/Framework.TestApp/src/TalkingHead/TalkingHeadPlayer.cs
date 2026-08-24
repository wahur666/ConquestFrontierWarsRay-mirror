using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.TalkingHead;

internal sealed class TalkingHeadPlayer : Node2D {
	private const int TimingFps = 30;

	private readonly AudioPlayer _voiceAudio;
	private Sprite? _face;
	private Sprite? _fuzz;
	private Sprite? _border;
	private TalkingHeadClip? _clip;
	private int _currentFaceFrame;
	private int _lastTimingFrame = -1;
	private int _lastFuzzFrame = -1;

	public TalkingHeadPlayer(string? name = null) : base(name ?? "TalkingHeadPlayer") {
		_voiceAudio = AddChild(new AudioPlayer("VoiceAudio"));
	}

	public bool BorderEnabled { get; set; } = true;
	public bool FuzzEnabled { get; set; } = true;

	public bool IsPlaying => _voiceAudio.IsPlaying;
	public float PlaybackPosition => _voiceAudio.PlaybackPosition;
	public float PlaybackLength => _voiceAudio.PlaybackLength;
	public TalkingHeadClip? Clip => _clip;

	public void SetClip(TalkingHeadClip? clip, bool disposeCurrent = false) {
		Stop();

		if (disposeCurrent) {
			_clip?.Dispose();
		}

		_clip = clip;
		_lastTimingFrame = -1;
		_lastFuzzFrame = -1;
		_currentFaceFrame = 0;

		if (_clip is null) {
			_voiceAudio.ClearAudio();
			if (_face is not null) {
				_face.Visible = false;
			}

			if (_fuzz is not null) {
				_fuzz.Visible = false;
			}

			if (_border is not null) {
				_border.Visible = false;
			}

			return;
		}

		EnsureVisualChildren();
		_voiceAudio.SetAudio(_clip.VoiceAudio);
		_face!.SetFrame(_clip.FaceFrames, 0);
		_face.Visible = true;

		if (_clip.HasFuzz) {
			_fuzz!.SetFrame(_clip.FuzzFrames!, 0);
		}

		if (_clip.HasBorder) {
			_border!.SetFrame(_clip.BorderFrames!, 0);
		}

		SyncOverlayVisibility();
		ApplyTime(0f);
	}

	public void Play() {
		if (_clip is null) {
			return;
		}

		_voiceAudio.Play();
	}

	public void Pause() {
		_voiceAudio.Pause();
	}

	public void Stop() {
		_voiceAudio.Stop();
		ApplyTime(0f, forceReset: true);
	}

	public void Seek(float positionSeconds) {
		_voiceAudio.Seek(positionSeconds);
		ApplyTime(positionSeconds, forceReset: true);
	}

	protected override void OnUpdate(float deltaTime) {
		_ = deltaTime;

		if (_clip is null || _face is null) {
			return;
		}

		SyncOverlayVisibility();
		UpdateBorderLayout();
		ApplyTime(_voiceAudio.PlaybackPosition);
	}

	private void ApplyTime(float positionSeconds, bool forceReset = false) {
		if (_clip is null || _face is null) {
			return;
		}

		if (forceReset) {
			_lastTimingFrame = -1;
			_lastFuzzFrame = -1;
		}

		var timingFrame = Math.Max(0, (int)MathF.Floor(positionSeconds * TimingFps));
		var fuzzFrame = Math.Max(0, (int)MathF.Floor(positionSeconds * _clip.FuzzFps));

		ApplyTimingFrame(timingFrame);
		ApplyFuzzFrame(fuzzFrame);
	}

	private void ApplyTimingFrame(int timingFrame) {
		if (_clip is null || _face is null || timingFrame == _lastTimingFrame) {
			return;
		}

		_lastTimingFrame = timingFrame;
		if (timingFrame >= _clip.Timeline.Count) {
			return;
		}

		var sourceIndex = _clip.Timeline[timingFrame].SourceFrameIndex;
		if (sourceIndex < 0) {
			return;
		}

		_currentFaceFrame = Math.Clamp(sourceIndex, 0, _clip.FaceFrames.Count - 1);
		_face.SetFrame(_clip.FaceFrames, _currentFaceFrame);
	}

	private void ApplyFuzzFrame(int fuzzFrame) {
		if (_clip is null || _fuzz is null || !_clip.HasFuzz || fuzzFrame == _lastFuzzFrame) {
			return;
		}

		_lastFuzzFrame = fuzzFrame;
		var fuzzIndex = fuzzFrame % _clip.FuzzFrames!.Count;
		_fuzz.SetFrame(_clip.FuzzFrames, fuzzIndex);
	}

	private void EnsureVisualChildren() {
		_face ??= AddChild(new Sprite(_clip!.FaceFrames, 0, "TalkingFace") {
			Pivot = Vector2.Zero,
			ZIndex = 0
		});

		if (_fuzz is null && _clip!.HasFuzz) {
			_fuzz = AddChild(new Sprite(_clip.FuzzFrames!, 0, "FuzzOverlay") {
				Pivot = Vector2.Zero,
				ZIndex = 1
			});
			_fuzz.BlendMode = BlendMode.Additive;
		}

		if (_border is null && _clip!.HasBorder) {
			_border = AddChild(new Sprite(_clip.BorderFrames!, 0, "BorderOverlay") {
				Pivot = Vector2.Zero,
				ZIndex = 2
			});
		}
	}

	private void SyncOverlayVisibility() {
		if (_clip is null) {
			return;
		}

		if (_fuzz is not null) {
			_fuzz.Visible = FuzzEnabled && _clip.HasFuzz;
		}

		if (_border is not null) {
			_border.Visible = BorderEnabled && _clip.HasBorder;
		}
	}

	private void UpdateBorderLayout() {
		if (_clip is null || _face is null) {
			return;
		}

		var faceRegion = _clip.FaceFrames.GetFrameRegion(_currentFaceFrame);
		var faceWidth = faceRegion.Width * _face.Scale.X;
		var faceHeight = faceRegion.Height * _face.Scale.Y;

		if (_fuzz is not null && _clip.HasFuzz) {
			var fuzzRegion = _clip.FuzzFrames!.GetFrameRegion(0);
			_fuzz.Position = _face.Position;
			_fuzz.Scale = new Vector2(faceWidth / fuzzRegion.Width, faceHeight / fuzzRegion.Height);
		}

		if (_border is null || !_clip.HasBorder || !_border.Visible) {
			return;
		}

		var borderRegion = _clip.BorderFrames!.GetFrameRegion(0);
		var scaleX = faceWidth / _clip.LegacyPortraitWidth;
		var scaleY = faceHeight / _clip.LegacyPortraitHeight;
		var borderWidth = faceWidth + ((_clip.BorderLeftInset + _clip.BorderRightInset) * scaleX);
		var borderHeight = faceHeight + ((_clip.BorderTopInset + _clip.BorderBottomInset) * scaleY);

		_border.Position = new Vector2(
			_face.Position.X - (_clip.BorderLeftInset * scaleX) - 1,
			_face.Position.Y - (_clip.BorderTopInset * scaleY));
		_border.Scale = new Vector2(borderWidth / borderRegion.Width, borderHeight / borderRegion.Height);
	}
}
