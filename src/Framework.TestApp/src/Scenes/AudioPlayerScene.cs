using System.Numerics;
using ConquestFrontierWarsRay.Core.UI;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp.Scenes;

internal sealed class AudioPlayerScene : ShowcaseScene {
	private readonly UiEventSource _eventSource = new("AudioControlsEventSource");
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
	private readonly ListViewNode _trackList = new("TrackList") {
		Position = new Vector2(418f, 166f),
		Size = new Vector2(316f, 344f),
		FontSize = 17f,
		ItemHeight = 52f,
		ItemSpacing = 16f,
		Fill = new Color(30, 38, 56, 255),
		HoverFill = new Color(46, 60, 86, 255),
		SelectedFill = new Color(74, 108, 170, 255),
		Outline = new Color(82, 98, 126, 255),
		SelectedOutline = Color.Gold
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
		Position = new Vector2(816f, 278f),
		FontSize = 15f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly TextNode _volumeLabel = new("VolumeLabel") {
		Position = new Vector2(816f, 368f),
		FontSize = 15f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly TextNode _panLabel = new("PanLabel") {
		Position = new Vector2(816f, 438f),
		FontSize = 15f,
		Tint = new Color(188, 200, 218, 255)
	};
	private readonly DropdownNode _backendDropdown = new("BackendDropdown") {
		Position = new Vector2(1020f, 144f),
		Size = new Vector2(186f, 34f),
		FontSize = 15f,
		Placeholder = "Backend"
	};
	private readonly ButtonNode _playButton = new("PlayButton") {
		Position = new Vector2(816f, 494f),
		Size = new Vector2(92f, 38f),
		Text = "Play",
		FontSize = 17f
	};
	private readonly ButtonNode _pauseButton = new("PauseButton") {
		Position = new Vector2(922f, 494f),
		Size = new Vector2(92f, 38f),
		Text = "Pause",
		FontSize = 17f
	};
	private readonly ButtonNode _stopButton = new("StopButton") {
		Position = new Vector2(1028f, 494f),
		Size = new Vector2(92f, 38f),
		Text = "Stop",
		FontSize = 17f
	};
	private readonly AudioPlayer _player = new("DemoAudioPlayer");
	private readonly SliderNode _positionSlider = new("TrackPositionSlider") {
		Position = new Vector2(816f, 308f),
		Size = new Vector2(360f, 28f),
		Step = 0.01f
	};
	private readonly SliderNode _volumeSlider = new("VolumeSlider") {
		Position = new Vector2(816f, 398f),
		Size = new Vector2(360f, 28f),
		Value = 0.8f,
		Step = 0.05f
	};
	private readonly SliderNode _panSlider = new("PanSlider") {
		Position = new Vector2(816f, 468f),
		Size = new Vector2(360f, 28f),
		Value = 0.5f,
		Step = 0.05f
	};
	private readonly List<TrackEntry> _tracks;
	private int _selectedTrackIndex;
	private bool _wasPositionDragging;

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

		_eventSource.ScopeRoot = this;
		_trackList.SelectionChanged += HandleTrackSelectionChanged;
		_backendDropdown.SelectionChanged += _ => LoadSelectedTrack();

		AddChild(_eventSource);
		AddChild(_panel);
		AddChild(_listPanel);
		AddChild(_trackList);
		AddChild(_playerPanel);
		AddChild(_trackTitle);
		AddChild(_trackMeta);
		AddChild(_stateLabel);
		AddChild(_backendDropdown);
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
		_backendDropdown.SetItems(Enum.GetNames<PlaybackBackend>(), (int)PlaybackBackend.NAudio);
		_trackList.SetItems(_tracks.Select(static track => track.Label), _selectedTrackIndex);
		_player.Volume = _volumeSlider.Value;
		_player.Pan = _panSlider.Value;
		LoadSelectedTrack();
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
		} else if (!_positionSlider.IsDragging) {
			var length = _player.PlaybackLength;
			_positionSlider.Value = length <= 0f ? 0f : _player.PlaybackPosition / length;
		}

		if (_wasPositionDragging && !_positionSlider.IsDragging) {
			var length = _player.PlaybackLength;
			if (length > 0f) {
				_player.Seek(length * _positionSlider.Value);
			}
		}

		_wasPositionDragging = _positionSlider.IsDragging;

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
		UiText.Draw("Track selection now uses a framework list view control, so the showcase sidebar keeps arrow-key navigation while this scene stays mouse-driven.", 396f, 82f, 17f, new Color(188, 200, 218, 255));
		UiText.Draw("Track list", 424f, 128f, 20f, new Color(230, 236, 246, 255));
		UiText.Draw("Player", 816f, 128f, 20f, new Color(230, 236, 246, 255));
		UiText.Draw("Seek", 816f, 256f, 15f, new Color(206, 216, 232, 255));
		UiText.Draw("Volume", 816f, 346f, 15f, new Color(206, 216, 232, 255));
		UiText.Draw("Pan", 816f, 416f, 15f, new Color(206, 216, 232, 255));
		UiText.Draw("Mouse: drag sliders and click transport buttons. Seek commits on slider release.", 816f, 550f, 16f, new Color(188, 200, 218, 255));
		UiText.Draw("Keyboard: sidebar scene selection is active again. Backend selection and track picking now route through the UI event dispatcher.", 816f, 574f, 16f, new Color(188, 200, 218, 255));
	}

	private void HandleTrackSelectionChanged(ListViewNode listView) {
		if (listView.SelectedIndex < 0) {
			return;
		}

		_selectedTrackIndex = listView.SelectedIndex;
		LoadSelectedTrack();
	}

	private void LoadSelectedTrack() {
		_trackList.SetSelectedIndex(_selectedTrackIndex);

		var selectedTrack = _tracks[_selectedTrackIndex];
		if (!File.Exists(selectedTrack.Path)) {
			_player.DisposeAudio();
			return;
		}

		var resumePlayback = _player.IsPlaying;
		_player.SetAudio(CreateAudioResource(selectedTrack.Path), disposeCurrent: true, takeOwnership: true);
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

	private PlaybackBackend CurrentBackend => _backendDropdown.SelectedIndex >= 0
		? (PlaybackBackend)_backendDropdown.SelectedIndex
		: PlaybackBackend.NAudio;

	private AudioStreamResource CreateAudioResource(string path) {
		return CurrentBackend switch {
			PlaybackBackend.NAudio => new NAudioStreamResource(path),
			_ => new MusicAudioResource(path)
		};
	}

	private static string FormatSeconds(float seconds) {
		if (seconds <= 0f) {
			return "00:00";
		}

		var time = TimeSpan.FromSeconds(seconds);
		return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
	}

	private enum PlaybackBackend {
		NAudio,
		Raylib
	}

	private sealed record TrackEntry(string Label, string Path);
}
