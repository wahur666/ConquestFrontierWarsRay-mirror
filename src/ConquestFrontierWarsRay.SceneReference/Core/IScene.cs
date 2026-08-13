namespace ConquestFrontierWarsRay.SceneReference.Core;

/// <summary>
///     A screen/state the game can be in. Game.ChangeScene swaps the active one.
/// </summary>
public interface IScene {
	void Load(ISceneController controller);

	/// <summary>Read input devices and react (change scene, trigger actions, etc).</summary>
	void HandleInput(float deltaTime);

	void Update(float deltaTime);

	/// <summary>Called between BeginDrawing/EndDrawing.</summary>
	void Draw();

	void Unload();
}
