using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.Scenes;

internal sealed class VideoPlayerScene : ShowcaseScene {
	private readonly UiEventSource _eventSource = new("VideoControlsEventSource");
	private readonly PanelNode _panel = new("VideoPanel") {
		Position = new Vector2(364f, 20f),
		Size = new Vector2(896f, 680f),
		Fill = new Color(14, 18, 30, 255),
		Outline = new Color(68, 84, 110, 255),
		OutlineThickness = 2f
	};
	private readonly VideoPlayer _video = new("DemoVideoPlayer") {
		Position = new Vector2(396f, 124f),
		Size = new Vector2(832f, 404f),
		Background = Color.Black,
		AutoPlay = true,
		Volume = 0.8f
	};
	private readonly ButtonNode _playButton = new("PlayVideoButton") {
		Position = new Vector2(396f, 548f),
		Size = new Vector2(92f, 38f),
		Text = "Play",
		FontSize = 17f
	};
	private readonly ButtonNode _pauseButton = new("PauseVideoButton") {
		Position = new Vector2(502f, 548f),
		Size = new Vector2(92f, 38f),
		Text = "Pause",
		FontSize = 17f
	};
	private readonly ButtonNode _stopButton = new("StopVideoButton") {
		Position = new Vector2(608f, 548f),
		Size = new Vector2(92f, 38f),
		Text = "Stop",
		FontSize = 17f
	};
	private readonly TextNode _status = new("VideoStatus") {
		Position = new Vector2(396f, 590f),
		FontSize = 16f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly TextNode _positionLabel = new("VideoPositionLabel") {
		Position = new Vector2(396f, 624f),
		FontSize = 15f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly TextNode _volumeLabel = new("VideoVolumeLabel") {
		Position = new Vector2(396f, 664f),
		FontSize = 15f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly SliderNode _positionSlider = new("VideoPositionSlider") {
		Position = new Vector2(860f, 624f),
		Size = new Vector2(368f, 28f),
		Step = 0.01f
	};
	private readonly SliderNode _volumeSlider = new("VideoVolumeSlider") {
		Position = new Vector2(860f, 664f),
		Size = new Vector2(368f, 28f),
		Value = 0.8f,
		Step = 0.05f
	};
	private bool _wasPositionDragging;

	public VideoPlayerScene() : base("VideoPlayerScene", "Video Player") {
		_eventSource.ScopeRoot = this;
		_playButton.Clicked += _ => _video.Play();
		_pauseButton.Clicked += _ => _video.Pause();
		_stopButton.Clicked += _ => _video.Stop();
		_volumeSlider.ValueChanged += _ => _video.Volume = _volumeSlider.Value;

		AddChild(_eventSource);
		AddChild(_panel);
		AddChild(_video);
		AddChild(_playButton);
		AddChild(_pauseButton);
		AddChild(_stopButton);
		AddChild(_status);
		AddChild(_positionLabel);
		AddChild(_volumeLabel);
		AddChild(_positionSlider);
		AddChild(_volumeSlider);
	}

	protected override void OnInitialize() {
		var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Movies", "cq_intro.mp4");
		if (File.Exists(path)) {
			_video.SetSourceFile(path, autoPlay: true);
			_video.Volume = _volumeSlider.Value;
		} else {
			_status.Text = $"Missing video asset: {path}";
		}
	}

	protected override void OnUpdate(float deltaTime) {
		_ = deltaTime;

		if (_positionSlider.HandleInput()) {
		} else if (!_positionSlider.IsDragging) {
			var length = _video.PlaybackLengthSeconds;
			_positionSlider.Value = length <= 0d ? 0f : (float)(_video.PlaybackPositionSeconds / length);
		}

		if (_wasPositionDragging && !_positionSlider.IsDragging) {
			var length = _video.PlaybackLengthSeconds;
			if (length > 0d) {
				_video.Seek(length * _positionSlider.Value);
			}
		}

		_wasPositionDragging = _positionSlider.IsDragging;

		_status.Text = !_video.HasVideo
			? "No video loaded"
			: $"{_video.FileName}   {FormatFrameRate(_video.FrameRate)}   {GetPlaybackState()}";
		_positionLabel.Text = $"Position: {FormatSeconds(_video.PlaybackPositionSeconds)} / {FormatSeconds(_video.PlaybackLengthSeconds)}";
		_volumeLabel.Text = $"Volume: {_video.Volume:0.00}";
	}

	protected override void OnDraw() {
		UiText.Draw("Video player demo", 396f, 48f, 26f, Color.RayWhite, UiTextStyle.Title);
		UiText.Draw("This node is now framework-owned. It draws as a normal Control and decodes mp4 video through Media Foundation without pulling in Legacy.RaySharp.", 396f, 82f, 17f, new Color(188, 200, 218, 255));
		UiText.Draw("Viewport", 396f, 100f, 20f, new Color(230, 236, 246, 255));
		UiText.Draw("Seek", 796f, 622f, 15f, new Color(206, 216, 232, 255));
		UiText.Draw("Volume", 796f, 662f, 15f, new Color(206, 216, 232, 255));
		UiText.Draw("Mouse: transport buttons and sliders. Seek drag now stays captured even after the pointer leaves the slider bounds. Stop pauses on frame zero.", 396f, 700f, 16f, new Color(188, 200, 218, 255));
	}

	private string GetPlaybackState() {
		if (!_video.HasVideo) {
			return "not loaded";
		}

		if (_video.IsFinished) {
			return "finished";
		}

		return _video.IsPlaying ? "playing" : "paused";
	}

	private static string FormatFrameRate(double frameRate) {
		return frameRate > 0d ? $"{frameRate:0.##} FPS" : "unknown FPS";
	}

	private static string FormatSeconds(double seconds) {
		if (seconds <= 0d) {
			return "00:00";
		}

		var time = TimeSpan.FromSeconds(seconds);
		return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
	}
}
