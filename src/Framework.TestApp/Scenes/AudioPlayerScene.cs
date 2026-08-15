using System.Numerics;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.Scenes;

internal sealed class AudioPlayerScene : ShowcaseScene {
	private readonly PanelNode _panel = new("AudioPanel") {
		Position = new Vector2(364f, 20f),
		Size = new Vector2(896f, 680f),
		Fill = new Color(14, 18, 30, 255),
		Outline = new Color(68, 84, 110, 255),
		OutlineThickness = 2f
	};
	private readonly PanelNode _listPanel = new("TrackListPanel") {
		Position = new Vector2(396f, 112f),
		Size = new Vector2(360f, 428f),
		Fill = new Color(22, 28, 42, 255),
		Outline = new Color(72, 90, 120, 255),
		OutlineThickness = 1.5f
	};
	private readonly PanelNode _playerPanel = new("PlayerPanel") {
		Position = new Vector2(786f, 112f),
		Size = new Vector2(432f, 428f),
		Fill = new Color(20, 26, 40, 255),
		Outline = new Color(72, 90, 120, 255),
		OutlineThickness = 1.5f
	};
	private readonly TextNode _trackTitle = new("TrackTitle") {
		Position = new Vector2(816f, 148f),
		FontSize = 20f,
		Tint = Color.RayWhite,
		TextStyle = UiTextStyle.Title
	};
	private readonly TextNode _trackMeta = new("TrackMeta") {
		Position = new Vector2(816f, 178f),
		FontSize = 16f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly TextNode _stateLabel = new("StateLabel") {
		Position = new Vector2(816f, 210f),
		FontSize = 16f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly TextNode _positionLabel = new("PositionLabel") {
		Position = new Vector2(816f, 266f),
		FontSize = 15f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly TextNode _volumeLabel = new("VolumeLabel") {
		Position = new Vector2(816f, 356f),
		FontSize = 15f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly TextNode _panLabel = new("PanLabel") {
		Position = new Vector2(816f, 426f),
		FontSize = 15f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly ButtonNode _playButton = new("PlayButton") {
		Position = new Vector2(816f, 482f),
		Size = new Vector2(92f, 38f),
		Text = "Play",
		FontSize = 17f
	};
	private readonly ButtonNode _pauseButton = new("PauseButton") {
		Position = new Vector2(922f, 482f),
		Size = new Vector2(92f, 38f),
		Text = "Pause",
		FontSize = 17f
	};
	private readonly ButtonNode _stopButton = new("StopButton") {
		Position = new Vector2(1028f, 482f),
		Size = new Vector2(92f, 38f),
		Text = "Stop",
		FontSize = 17f
	};
	private readonly AudioPlayer _player = new("DemoAudioPlayer");
	private readonly SliderNode _positionSlider = new("TrackPositionSlider") {
		Position = new Vector2(816f, 296f),
		Size = new Vector2(360f, 28f),
		Step = 0.01f
	};
	private readonly SliderNode _volumeSlider = new("VolumeSlider") {
		Position = new Vector2(816f, 386f),
		Size = new Vector2(360f, 28f),
		Value = 0.8f,
		Step = 0.05f
	};
	private readonly SliderNode _panSlider = new("PanSlider") {
		Position = new Vector2(816f, 456f),
		Size = new Vector2(360f, 28f),
		Value = 0.5f,
		Step = 0.05f
	};
	private readonly List<TrackEntry> _tracks;
	private int _selectedTrackIndex;

	public AudioPlayerScene() : base("AudioPlayerScene", "Audio Player") {
		var ostRoot = Path.Combine(AppContext.BaseDirectory, "Assets", "conquest_frontier_wars_ost");
		var speechRoot = Path.Combine(AppContext.BaseDirectory, "Assets", "mspeech");

		_tracks = [
			new TrackEntry("Main Menu Screen Music", Path.Combine(ostRoot, "Conquest Frontier Wars soundtrack - Main Menu Screen Music.mp3")),
			new TrackEntry("Terran Game Music", Path.Combine(ostRoot, "Conquest Frontier Wars soundtrack - Terran Game Music.mp3")),
			new TrackEntry("Danger Music", Path.Combine(ostRoot, "Conquest Frontier Wars soundtrack - Danger Music.mp3")),
			new TrackEntry("Broadcast 01", Path.Combine(speechRoot, "bcast_01.wav")),
			new TrackEntry("Blackwell 01", Path.Combine(speechRoot, "comm_blackwell_01.wav"))
		];

		AddChild(_panel);
		AddChild(_listPanel);
		AddChild(_playerPanel);
		AddChild(_trackTitle);
		AddChild(_trackMeta);
		AddChild(_stateLabel);
		AddChild(_positionLabel);
		AddChild(_volumeLabel);
		AddChild(_panLabel);
		AddChild(_playButton);
		AddChild(_pauseButton);
		AddChild(_stopButton);
		AddChild(_positionSlider);
		AddChild(_volumeSlider);
		AddChild(_panSlider);
		AddChild(_player);
	}

	protected override void OnInitialize() {
		_player.Volume = _volumeSlider.Value;
		_player.Pan = _panSlider.Value;
		LoadSelectedTrack();
	}

	public override bool HandleNavigation(MenuNavigation navigation) {
		var consumed = false;

		if (navigation.Up) {
			_selectedTrackIndex = (_selectedTrackIndex - 1 + _tracks.Count) % _tracks.Count;
			LoadSelectedTrack();
			consumed = true;
		}

		if (navigation.Down) {
			_selectedTrackIndex = (_selectedTrackIndex + 1) % _tracks.Count;
			LoadSelectedTrack();
			consumed = true;
		}

		if (navigation.Confirm) {
			PlaySelectedTrack();
			consumed = true;
		}

		return consumed;
	}

	protected override void OnUpdate(float deltaTime) {
		_ = deltaTime;

		if (_playButton.HandleInput()) {
			PlaySelectedTrack();
		}

		if (_pauseButton.HandleInput()) {
			_player.Pause();
		}

		if (_stopButton.HandleInput()) {
			_player.Stop();
		}

		if (_positionSlider.HandleInput()) {
			var length = _player.PlaybackLength;
			if (length > 0f) {
				_player.Seek(length * _positionSlider.Value);
			}
		} else if (!_positionSlider.IsDragging) {
			var length = _player.PlaybackLength;
			_positionSlider.Value = length <= 0f ? 0f : _player.PlaybackPosition / length;
		}

		if (_volumeSlider.HandleInput()) {
			_player.Volume = _volumeSlider.Value;
		}

		if (_panSlider.HandleInput()) {
			_player.Pan = _panSlider.Value;
		}

		var selectedTrack = _tracks[_selectedTrackIndex];
		_trackTitle.Text = selectedTrack.Label;
		_trackMeta.Text = $"{Path.GetExtension(selectedTrack.Path).TrimStart('.').ToUpperInvariant()}    {selectedTrack.Path}";
		_stateLabel.Text = $"State: {GetPlaybackState()}";
		_positionLabel.Text = $"Position: {FormatSeconds(_player.PlaybackPosition)} / {FormatSeconds(_player.PlaybackLength)}    slider={_positionSlider.Value:0.00}";
		_volumeLabel.Text = $"Volume: {_player.Volume:0.00}";
		_panLabel.Text = $"Pan: {_player.Pan:0.00}";
	}

	protected override void OnDraw() {
		UiText.Draw("Audio player demo", 396f, 48f, 26f, Color.RayWhite, UiTextStyle.Title);
		UiText.Draw("Up/Down changes the selected track. Enter or the Play button starts it. The player node is using MusicAudioResource underneath, so both mp3 OST files and wav clips run through the same node path.", 396f, 82f, 17f, new Color(188, 200, 218, 255));
		UiText.Draw("Track list", 424f, 128f, 20f, new Color(230, 236, 246, 255));
		UiText.Draw("Player", 816f, 128f, 20f, new Color(230, 236, 246, 255));
		UiText.Draw("Seek", 816f, 244f, 15f, new Color(206, 216, 232, 255));
		UiText.Draw("Volume", 816f, 334f, 15f, new Color(206, 216, 232, 255));
		UiText.Draw("Pan", 816f, 404f, 15f, new Color(206, 216, 232, 255));
		UiText.Draw("Mouse: drag sliders and click transport buttons.", 816f, 538f, 16f, new Color(188, 200, 218, 255));
		UiText.Draw("Keyboard: Up/Down select, Enter plays the highlighted track.", 816f, 562f, 16f, new Color(188, 200, 218, 255));
		DrawTrackList();
	}

	private void DrawTrackList() {
		for (var i = 0; i < _tracks.Count; i++) {
			var selected = i == _selectedTrackIndex;
			var y = 166f + (i * 68f);
			var row = new Rectangle(418f, y, 316f, 52f);
			Raylib.DrawRectangleRec(row, selected ? new Color(74, 108, 170, 255) : new Color(30, 38, 56, 255));
			Raylib.DrawRectangleLinesEx(row, 1.25f, selected ? Color.Gold : new Color(82, 98, 126, 255));
			UiText.Draw(_tracks[i].Label, row.X + 14f, row.Y + 11f, 17f, Color.RayWhite);
			UiText.Draw(Path.GetExtension(_tracks[i].Path).TrimStart('.').ToUpperInvariant(), row.X + 14f, row.Y + 30f, 14f, new Color(196, 206, 224, 255));
		}
	}

	private void LoadSelectedTrack() {
		var selectedTrack = _tracks[_selectedTrackIndex];
		if (!File.Exists(selectedTrack.Path)) {
			_player.DisposeAudio();
			return;
		}

		var resumePlayback = _player.IsPlaying;
		_player.SetAudioFile(selectedTrack.Path);
		_player.Volume = _volumeSlider.Value;
		_player.Pan = _panSlider.Value;
		_positionSlider.Value = 0f;

		if (resumePlayback) {
			_player.Play();
		}
	}

	private void PlaySelectedTrack() {
		if (_player.Audio is null) {
			LoadSelectedTrack();
		}

		_player.Play();
	}

	private string GetPlaybackState() {
		if (_player.Audio is null) {
			return "No track loaded";
		}

		return _player.IsPlaying ? "Playing" : "Paused / Stopped";
	}

	private static string FormatSeconds(float seconds) {
		if (seconds <= 0f) {
			return "00:00";
		}

		var time = TimeSpan.FromSeconds(seconds);
		return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
	}

	private sealed record TrackEntry(string Label, string Path);
}
