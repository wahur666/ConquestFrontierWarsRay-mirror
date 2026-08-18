using System.Reflection;

namespace RaySharp;

internal sealed class SceneRegistry {
	private readonly SceneContext context;
	private readonly Dictionary<SceneId, Func<IScene>> factories = new();

	private SceneRegistry(SceneContext context) {
		this.context = context;
	}

	public static SceneRegistry Discover(SceneContext context) {
		SceneRegistry registry = new(context);
		registry.RegisterDiscoveredScenes();
		return registry;
	}

	public IScene CreateStartupScene(bool skipIntro, bool startMeshViewer, bool startParticleEditor) {
		if (startMeshViewer) {
			return Create(SceneId.MeshViewer);
		}

		if (startParticleEditor) {
			return Create(SceneId.ParticleEditor);
		}

		return skipIntro ? Create(SceneId.Main) : CreateIntroOrMain();
	}

	public IScene Create(SceneRequest request) {
		return Create(request.Id);
	}

	public IScene Create(SceneId id) {
		return factories.TryGetValue(id, out Func<IScene>? factory)
			? factory()
			: throw new InvalidOperationException($"Scene '{id}' is not registered.");
	}

	private IScene CreateIntroOrMain() {
		VideoPlayer? videoPlayer = VideoLibrary.TryLoadStartupVideo(context.Ui);
		return videoPlayer is null
			? Create(SceneId.Main)
			: new IntroVideoScene(context, videoPlayer);
	}

	private void RegisterDiscoveredScenes() {
		foreach (Type sceneType in typeof(IScene).Assembly.GetTypes()) {
			if (sceneType.IsAbstract || !typeof(IScene).IsAssignableFrom(sceneType)) {
				continue;
			}

			if (!TryResolveSceneId(sceneType, out SceneId id)) {
				continue;
			}

			ConstructorInfo? constructor = sceneType.GetConstructor(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				binder: null,
				[typeof(SceneContext)],
				modifiers: null);
			if (constructor is null) {
				continue;
			}

			factories[id] = () => (IScene)constructor.Invoke([context]);
		}
	}

	private static bool TryResolveSceneId(Type sceneType, out SceneId id) {
		string name = sceneType.Name;
		if (name.EndsWith("Scene", StringComparison.Ordinal)) {
			name = name[..^"Scene".Length];
		}

		return Enum.TryParse(name, ignoreCase: false, out id);
	}
}