namespace RaySharp;

internal sealed class UtfTextureImage(
	string name,
	int width,
	int height,
	byte[] rgba,
	bool hasAlpha) {
	public string Name { get; } = name;
	public int Width { get; } = width;
	public int Height { get; } = height;
	public byte[] Rgba { get; } = rgba;
	public bool HasAlpha { get; } = hasAlpha;
}
