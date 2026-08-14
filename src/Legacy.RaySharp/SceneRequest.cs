namespace RaySharp;

internal readonly record struct SceneRequest(SceneId Id) {
	public static readonly SceneRequest Main = new(SceneId.Main);
	public static readonly SceneRequest IntroVideo = new(SceneId.IntroVideo);
	public static readonly SceneRequest MeshViewer = new(SceneId.MeshViewer);
	public static readonly SceneRequest ParticleEditor = new(SceneId.ParticleEditor);
}