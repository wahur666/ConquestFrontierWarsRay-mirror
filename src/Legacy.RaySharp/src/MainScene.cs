using System.Numerics;
using RaySharp.MeshViewer;
using Raylib_cs;

namespace RaySharp;

internal sealed class MainScene : IScene
{
    private readonly SceneContext context;
    private readonly RtsCamera rtsCamera = new()
    {
        Target = Vector3.Zero,
        Distance = 37.0f,
        Yaw = 0.0f,
        Pitch = -0.86f
    };

    private readonly List<Unit> units =
    [
        new(new Vector3(-10.0f, 0.0f, 0.0f)),
        new(new Vector3(0.0f, 0.0f, 0.0f)),
        new(new Vector3(10.0f, 0.0f, 0.0f))
    ];
    private readonly List<RenderableObject> renderables;
    private readonly MeshViewerModelRenderer modelRenderer = new();
    private readonly VfxAudioPlayer? vfxAudioPlayer;
    private VideoPlayer? videoPlayer;

    public MainScene(SceneContext context)
    {
        this.context = context;
        renderables = units.Cast<RenderableObject>().ToList();
        videoPlayer = VideoLibrary.TryLoadDemoVideo(context.Ui);
        vfxAudioPlayer = VfxAudioPlayer.TryLoad();
        foreach (Unit unit in units)
        {
            unit.SetScale(0.5f);
        }

        context.Ui.Status = units.Count > 0 ? $"{units[0].ModelStatus} Units: {units.Count}." : "No units loaded.";
    }

    public SceneRequest? Update(float deltaTime)
    {
        bool stopVideoAfterFrame = false;

        AppWindow.HandleShortcuts(context.WindowState, context.Ui);
        AppWindow.UpdateMouseConfinement(context.WindowState);
        modelRenderer.ShowWireframe = context.Ui.ShowTestSphereWire;
        rtsCamera.Update(deltaTime, context.Ui.MenuOpen);
        Camera3D camera = rtsCamera.ToCamera();

        MainSceneInput.Handle(camera, units, context.Ui);
        RenderUpdateContext updateContext = new(deltaTime, context.Ui);
        foreach (RenderableObject renderable in renderables)
        {
            renderable.Update(updateContext);
        }
        videoPlayer?.Update(deltaTime);
        vfxAudioPlayer?.Update();

        Raylib.BeginDrawing();
        Raylib.ClearBackground(AppTheme.Background);

        Raylib.BeginMode3D(camera);
        MainSceneHud.DrawCommandPlane();
        RenderContext renderContext = new(modelRenderer);
        foreach (RenderableObject renderable in renderables)
        {
            renderable.Render(renderContext);
        }
        Raylib.EndMode3D();

        MainSceneHud.DrawUi(context.Ui, context.UiTextures, context.UiFont);
        MainSceneHud.DrawSelectionRectangle(context.Ui);
        if (videoPlayer is not null)
        {
            MainSceneHud.DrawVideoOverlay(videoPlayer, context.UiFont, MainSceneHud.DefaultVideoOverlayPosition(videoPlayer, false));
        }

        MainSceneHud.DrawVfxAudioOverlay(vfxAudioPlayer, context.UiFont, MainSceneHud.DefaultVfxAudioOverlayPosition(true));
        MainSceneHud.DrawMediaControls(ref videoPlayer, vfxAudioPlayer, context.UiFont, context.Ui, ref stopVideoAfterFrame);
        Raylib.EndDrawing();

        if (stopVideoAfterFrame)
        {
            videoPlayer?.Dispose();
            videoPlayer = null;
        }

        return null;
    }

    public void Dispose()
    {
        foreach (RenderableObject renderable in renderables)
        {
            renderable.Dispose();
        }

        modelRenderer.Dispose();
        vfxAudioPlayer?.Dispose();
        videoPlayer?.Dispose();
    }
}
