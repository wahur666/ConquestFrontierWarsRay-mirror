namespace RaySharp;

internal sealed class SceneContext(
    WindowState windowState,
    UiState ui,
    UiTextures uiTextures,
    UiFont uiFont)
{
    public WindowState WindowState { get; } = windowState;
    public UiState Ui { get; } = ui;
    public UiTextures UiTextures { get; } = uiTextures;
    public UiFont UiFont { get; } = uiFont;
}
