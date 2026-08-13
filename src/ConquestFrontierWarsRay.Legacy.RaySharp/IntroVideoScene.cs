using Raylib_cs;

namespace RaySharp;

internal sealed class IntroVideoScene : IScene
{
    private readonly SceneContext context;
    private readonly VideoPlayer videoPlayer;

    internal IntroVideoScene(SceneContext context)
        : this(context, VideoLibrary.TryLoadStartupVideo(context.Ui) ?? throw new InvalidOperationException("Startup video is not available."))
    {
    }

    internal IntroVideoScene(SceneContext context, VideoPlayer videoPlayer)
    {
        this.context = context;
        this.videoPlayer = videoPlayer;
        this.videoPlayer.Play();
    }

    public SceneRequest? Update(float deltaTime)
    {
        AppWindow.UpdateMouseConfinement(context.WindowState);
        videoPlayer.Update(deltaTime);

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);
        VideoPresenter.DrawStartupVideo(videoPlayer);
        Raylib.EndDrawing();

        return Raylib.IsKeyPressed(KeyboardKey.Space) || videoPlayer.IsFinished
            ? SceneRequest.Main
            : null;
    }

    public void Dispose()
    {
        videoPlayer.Dispose();
    }
}
