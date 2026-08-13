namespace ConquestFrontierWarsRay.SceneReference.Core;

/// <summary>
///     The subset of Game a scene is allowed to touch: swap to another scene, or ask to quit.
/// </summary>
public interface ISceneController {
	void ChangeScene(IScene next);

	void RequestExit();
}
