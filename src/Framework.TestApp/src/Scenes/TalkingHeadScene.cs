using System.Numerics;
using ConquestFrontierWarsRay.Framework.TestApp.TalkingHead;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.Scenes;

internal sealed class TalkingHeadScene : ShowcaseScene {
	private readonly PanelNode _panel = new("TalkingHeadPanel") {
		Position = new Vector2(364f, 20f),
		Size = new Vector2(896f, 680f),
		Fill = new Color(14, 18, 30, 255),
		Outline = new Color(68, 84, 110, 255),
		OutlineThickness = 2f
	};
	private readonly TalkingHeadPlayer _player = new("TalkingHeadPlayer") {
		Position = new Vector2(850f, 196f),
		Scale = new Vector2(4f, 4f)
	};
	private readonly ButtonNode _playButton = new("PlayButton") {
		Position = new Vector2(438f, 194f),
		Size = new Vector2(92f, 38f),
		Text = "Play",
		FontSize = 17f
	};
	private readonly ButtonNode _pauseButton = new("PauseButton") {
		Position = new Vector2(546f, 194f),
		Size = new Vector2(92f, 38f),
		Text = "Pause",
		FontSize = 17f
	};
	private readonly ButtonNode _stopButton = new("StopButton") {
		Position = new Vector2(654f, 194f),
		Size = new Vector2(92f, 38f),
		Text = "Stop",
		FontSize = 17f
	};
	private readonly SliderNode _positionSlider = new("TalkingHeadPosition") {
		Position = new Vector2(438f, 278f),
		Size = new Vector2(360f, 28f),
		Step = 0.01f
	};
	private readonly TextNode _stateLabel = new("TalkingHeadState") {
		Position = new Vector2(438f, 244f),
		FontSize = 16f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly TextNode _clipLabel = new("TalkingHeadClip") {
		Position = new Vector2(438f, 116f),
		FontSize = 18f,
		Tint = Color.RayWhite,
		TextStyle = UiTextStyle.Title
	};
	private readonly TextNode _toggleLabel = new("TalkingHeadToggle") {
		Position = new Vector2(438f, 340f),
		FontSize = 16f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly TalkingHeadClip _clip;
	private bool _wasDragging;

	public TalkingHeadScene() : base("TalkingHeadScene", "Talking Head") {
		_clip = TalkingHeadClipLoader.LoadBlackwellDemo();

		AddChild(_panel);
		AddChild(_clipLabel);
		AddChild(_stateLabel);
		AddChild(_toggleLabel);
		AddChild(_playButton);
		AddChild(_pauseButton);
		AddChild(_stopButton);
		AddChild(_positionSlider);
		AddChild(_player);
	}

	protected override void OnInitialize() {
		_player.SetClip(_clip);
		_clipLabel.Text = "Blackwell demo clip";
	}

	protected override void OnUpdate(float deltaTime) {
		_ = deltaTime;

		if (_playButton.HandleInput()) {
			_player.Play();
		}

		if (_pauseButton.HandleInput()) {
			_player.Pause();
		}

		if (_stopButton.HandleInput()) {
			_player.Stop();
		}

		if (Raylib.IsKeyPressed(KeyboardKey.F)) {
			_player.FuzzEnabled = !_player.FuzzEnabled;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.B)) {
			_player.BorderEnabled = !_player.BorderEnabled;
		}

		if (_positionSlider.HandleInput()) {
		} else if (!_positionSlider.IsDragging) {
			var length = _player.PlaybackLength;
			_positionSlider.Value = length <= 0f ? 0f : _player.PlaybackPosition / length;
		}

		if (_wasDragging && !_positionSlider.IsDragging) {
			var length = _player.PlaybackLength;
			if (length > 0f) {
				_player.Seek(length * _positionSlider.Value);
			}
		}

		_wasDragging = _positionSlider.IsDragging;
		_stateLabel.Text = $"State: {(_player.IsPlaying ? "Playing" : "Stopped / Paused")}    Position: {FormatSeconds(_player.PlaybackPosition)} / {FormatSeconds(_player.PlaybackLength)}";
		_toggleLabel.Text = $"Fuzz: {(_player.FuzzEnabled ? "On" : "Off")} [F]    Border: {(_player.BorderEnabled ? "On" : "Off")} [B]";
	}

	protected override void OnDraw() {
		UiText.Draw("Talking head composite", 438f, 50f, 26f, Color.RayWhite, UiTextStyle.Title);
		UiText.Draw("This scene drives a game-level TalkingHeadPlayer from atlas frames, a txt timeline, additive fuzz, border layout constants, and AudioPlayer transport.", 438f, 82f, 17f, new Color(188, 200, 218, 255));
		UiText.Draw("Transport", 438f, 160f, 18f, new Color(230, 236, 246, 255));
		UiText.Draw("Seek", 438f, 250f, 15f, new Color(206, 216, 232, 255));
	}

	protected override void OnDispose() {
		_player.SetClip(null);
		_clip.Dispose();
	}

	private static string FormatSeconds(float seconds) {
		if (seconds <= 0f) {
			return "00:00";
		}

		var time = TimeSpan.FromSeconds(seconds);
		return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
	}
}
