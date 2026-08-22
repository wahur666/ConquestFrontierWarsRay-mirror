namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Backward-compatible alias for the framework text control.
/// </summary>
public sealed class TextNode(string? name = null) : UiText(name);
