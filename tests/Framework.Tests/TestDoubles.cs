using System.Numerics;

namespace ConquestFrontierWarsRay.Framework.Tests;

internal sealed class TestNode : Node {
	public List<string> LifecycleEvents { get; } = [];
	public List<float> UpdateDeltas { get; } = [];
	public int DrawCount { get; private set; }
	public bool WasDisposed { get; private set; }

	public TestNode(string? name = null) : base(name) {
	}

	public InputManager ExposedInput => Input;

	public SceneTree ExposedTree => Tree;

	public void ExposedRequestQuit() {
		RequestQuit();
	}

	protected override void OnInitialize() {
		LifecycleEvents.Add($"{Name}:Initialize");
	}

	protected override void OnEnterTree() {
		LifecycleEvents.Add($"{Name}:EnterTree");
	}

	protected override void OnExitTree() {
		LifecycleEvents.Add($"{Name}:ExitTree");
	}

	protected override void OnUpdate(float deltaTime) {
		UpdateDeltas.Add(deltaTime);
	}

	protected override void OnDraw() {
		DrawCount++;
	}

	protected override void OnDispose() {
		WasDisposed = true;
	}
}

internal sealed class TestResource : Resource {
	public int LoadCount { get; private set; }
	public int UnloadCount { get; private set; }

	public TestResource(string? resourcePath = null) : base(resourcePath) {
	}

	public void Load() {
		EnsureLoaded();
	}

	protected override void LoadCore() {
		LoadCount++;
	}

	protected override void UnloadCore() {
		UnloadCount++;
	}
}

internal sealed class FakeTexture : Texture2D {
	private readonly TextureSlice _slice;

	public int GetSliceCount { get; private set; }
	public int LoadCount { get; private set; }

	public FakeTexture(TextureSlice slice, string? resourcePath = null) : base(resourcePath) {
		_slice = slice;
	}

	public override Vector2 Size => new(_slice.Source.Width, _slice.Source.Height);

	public override TextureSlice GetSlice() {
		EnsureLoaded();
		GetSliceCount++;
		return _slice;
	}

	protected override void LoadCore() {
		LoadCount++;
	}
}

internal sealed class FakeAudioStreamResource : AudioStreamResource {
	private readonly float _timeLength;
	private bool _isPlaying;
	private float _timePlayed;

	public FakeAudioStreamResource(string? resourcePath = null, float timeLength = 10f) : base(resourcePath) {
		_timeLength = timeLength;
	}

	public int LoadCount { get; private set; }
	public int UnloadCount { get; private set; }
	public int PlayCount { get; private set; }
	public int PauseCount { get; private set; }
	public int StopCount { get; private set; }
	public int SeekCount { get; private set; }
	public int UpdateCount { get; private set; }

	public override bool Looping { get; set; }

	public override float Volume { get; set; } = 1f;

	public override float Pitch { get; set; } = 1f;

	public override float Pan { get; set; } = 0.5f;

	public override bool IsPlaying => _isPlaying;

	public override float TimePlayed => _timePlayed;

	public override float TimeLength {
		get {
			EnsureLoaded();
			return _timeLength;
		}
	}

	public override void Play() {
		EnsureLoaded();
		PlayCount++;
		_isPlaying = true;
	}

	public override void Pause() {
		EnsureLoaded();
		PauseCount++;
		_isPlaying = false;
	}

	public override void Stop() {
		EnsureLoaded();
		StopCount++;
		_isPlaying = false;
		_timePlayed = 0f;
	}

	public override void Seek(float positionSeconds) {
		EnsureLoaded();
		SeekCount++;
		_timePlayed = Math.Clamp(positionSeconds, 0f, _timeLength);
	}

	public override void Update() {
		EnsureLoaded();
		UpdateCount++;
	}

	protected override void LoadCore() {
		LoadCount++;
	}

	protected override void UnloadCore() {
		UnloadCount++;
	}
}
