namespace RaySharp;

internal interface IScene : IDisposable
{
    SceneRequest? Update(float deltaTime);
}
