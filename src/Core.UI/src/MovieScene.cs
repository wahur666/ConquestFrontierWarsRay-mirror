using System.Numerics;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Core.UI;

/// <summary>
/// Full-window movie playback root that can return to another scene on exit.
/// </summary>
public sealed class MovieScene : Node2D {
	private readonly Func<Node> _returnSceneFactory;
	private readonly string _moviePath;
	private readonly VideoPlayer _videoPlayer = new("MovieSceneVideoPlayer") {
		Background = Color.Black,
		AutoPlay = true
	};
	private readonly TextNode _statusText = new("MovieSceneStatus") {
		Position = new Vector2(24f, 24f),
		FontSize = 18f,
		Tint = new Color(228, 232, 240, 255)
	};
	private bool _advanceRequested;

	public MovieScene(string moviePath, Func<Node> returnSceneFactory) : base("MovieScene") {
		ArgumentException.ThrowIfNullOrWhiteSpace(moviePath);
		ArgumentNullException.ThrowIfNull(returnSceneFactory);

		_moviePath = moviePath;
		_returnSceneFactory = returnSceneFactory;

		AddChild(_videoPlayer);
		AddChild(_statusText);
	}

	protected override void OnInitialize() {
		var resolvedPath = ResolveMoviePath(_moviePath);
		if (!File.Exists(resolvedPath)) {
			_statusText.Text = $"Missing movie: {resolvedPath}";
			_advanceRequested = true;
			return;
		}

		_videoPlayer.SetSourceFile(resolvedPath, autoPlay: true);
		_statusText.Text = string.Empty;
	}

	protected override void OnExitTree() {
		_videoPlayer.Stop();
		_videoPlayer.DisposeSource();
		base.OnExitTree();
	}

	protected override void OnUpdate(float deltaTime) {
		_ = deltaTime;

		_videoPlayer.Position = Vector2.Zero;
		_videoPlayer.Size = new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

		if (!_advanceRequested && (Input.UiEsc || Raylib.IsKeyPressed(KeyboardKey.Space))) {
			_advanceRequested = true;
		}

		if (!_advanceRequested && _videoPlayer.IsFinished) {
			_advanceRequested = true;
		}

		if (_advanceRequested) {
			Tree.ChangeRoot(_returnSceneFactory());
		}
	}

	protected override void Draw() {
		Raylib.ClearBackground(Color.Black);
	}

	private static string ResolveMoviePath(string moviePath) {
		if (Path.IsPathRooted(moviePath)) {
			return moviePath;
		}

		var outputPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, moviePath));
		if (File.Exists(outputPath)) {
			return outputPath;
		}

		return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", moviePath));
	}
}
