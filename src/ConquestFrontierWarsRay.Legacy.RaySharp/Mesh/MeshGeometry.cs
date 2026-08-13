using System.Numerics;
using Raylib_cs;

namespace RaySharp.Mesh;

internal sealed class MeshGeometry(
	string name,
	IReadOnlyList<MeshTriangle> triangles,
	BoundingBox bounds,
	int vertexCount,
	int faceCount,
	int materialCount,
	IReadOnlyDictionary<string, UtfTextureImage> textureImages,
	MeshLoadDiagnostics diagnostics,
	IReadOnlyList<MeshPartGeometry>? parts = null,
	IReadOnlyList<MeshPartHierarchyLink>? hierarchy = null,
	IReadOnlyList<MeshAnimationClip>? animations = null,
	IReadOnlyList<MeshHardpoint>? hardpoints = null,
	IReadOnlyList<MeshParticleEmitter>? particleEmitters = null) {
	public string Name { get; } = name;
	public IReadOnlyList<MeshTriangle> Triangles { get; } = triangles;
	public BoundingBox Bounds { get; } = bounds;
	public int VertexCount { get; } = vertexCount;
	public int FaceCount { get; } = faceCount;
	public int MaterialCount { get; } = materialCount;
	public IReadOnlyDictionary<string, UtfTextureImage> TextureImages { get; } = textureImages;
	public MeshLoadDiagnostics Diagnostics { get; } = diagnostics;
	public IReadOnlyList<MeshPartGeometry> Parts { get; } = parts ?? [];
	public IReadOnlyList<MeshPartHierarchyLink> Hierarchy { get; } = hierarchy ?? [];
	public IReadOnlyList<MeshAnimationClip> Animations { get; } = animations ?? [];
	public IReadOnlyList<MeshHardpoint> Hardpoints { get; } = hardpoints ?? [];
	public IReadOnlyList<MeshParticleEmitter> ParticleEmitters { get; } = particleEmitters ?? [];
	public int TextureCount => TextureImages.Count;
}

internal readonly record struct MeshPartGeometry(
	string Name,
	string FileName,
	Matrix4x4 LocalTransform,
	IReadOnlyList<MeshTriangle> Triangles,
	BoundingBox LocalBounds);

internal readonly record struct MeshPartHierarchyLink(string ParentName, string ChildName);

internal readonly record struct MeshHardpoint(
	string Name,
	string GroupName,
	string? ParentName,
	Vector3 Position,
	Matrix3x3 Orientation);

internal sealed class MeshAnimationClip(
	string name,
	float duration,
	IReadOnlyList<MeshAnimationTrack> tracks) {
	public string Name { get; } = name;
	public float Duration { get; } = duration;
	public IReadOnlyList<MeshAnimationTrack> Tracks { get; } = tracks;
}

internal readonly record struct MeshAnimationTrack(
	MeshAnimationTargetType TargetType,
	string TargetName,
	MeshCompoundJoint? Joint,
	MeshAnimationChannel Channel);

internal enum MeshAnimationTargetType {
	Joint,
	Object
}

internal sealed class MeshAnimationChannel(
	MeshAnimationChannelKind kind,
	float duration,
	IReadOnlyList<MeshFloatKeyframe>? floatFrames = null,
	IReadOnlyList<MeshVectorKeyframe>? vectorFrames = null,
	IReadOnlyList<MeshQuaternionKeyframe>? quaternionFrames = null,
	IReadOnlyList<MeshTransformKeyframe>? transformFrames = null) {
	public MeshAnimationChannelKind Kind { get; } = kind;
	public float Duration { get; } = duration;
	public IReadOnlyList<MeshFloatKeyframe> FloatFrames { get; } = floatFrames ?? [];
	public IReadOnlyList<MeshVectorKeyframe> VectorFrames { get; } = vectorFrames ?? [];
	public IReadOnlyList<MeshQuaternionKeyframe> QuaternionFrames { get; } = quaternionFrames ?? [];
	public IReadOnlyList<MeshTransformKeyframe> TransformFrames { get; } = transformFrames ?? [];
}

internal enum MeshAnimationChannelKind {
	Float,
	Vector,
	Quaternion,
	FullTransform
}

internal readonly record struct MeshFloatKeyframe(float Time, float Value);

internal readonly record struct MeshVectorKeyframe(float Time, Vector3 Position);

internal readonly record struct MeshQuaternionKeyframe(float Time, Quaternion Rotation);

internal readonly record struct MeshTransformKeyframe(float Time, Vector3 Position, Quaternion Rotation);

internal enum MeshCompoundJointType {
	Fixed,
	Translational,
	Loose,
	Revolute,
	Prismatic,
	Spherical
}

internal readonly record struct MeshCompoundJoint(
	MeshCompoundJointType Type,
	string Parent,
	string Child,
	Vector3 ParentPoint,
	Vector3 ChildPoint,
	Matrix3x3 RelativeOrientation,
	Vector3 Axis);

internal readonly record struct Matrix3x3(
	float E00,
	float E01,
	float E02,
	float E10,
	float E11,
	float E12,
	float E20,
	float E21,
	float E22);

internal readonly record struct MeshTriangle(
	Vector3 A,
	Vector3 B,
	Vector3 C,
	Vector2 UvA,
	Vector2 UvB,
	Vector2 UvC,
	Vector2 Uv2A,
	Vector2 Uv2B,
	Vector2 Uv2C,
	Vector3 NormalA,
	Vector3 NormalB,
	Vector3 NormalC,
	string? TextureName,
	MeshTextureAddress TextureAddress,
	string? SecondDiffuseTextureName,
	MeshTextureAddress SecondDiffuseTextureAddress,
	string? EmissiveTextureName,
	MeshTextureAddress EmissiveTextureAddress,
	byte EmissiveBlend,
	Color Tint,
	bool UsesAlphaBlend,
	bool WritesDepth);

internal readonly record struct MeshTextureAddress(int U, int V, int CoordinateSet) {
	public static MeshTextureAddress Repeat { get; } = new(0, 0, 0);
}

internal readonly record struct MeshParticleEmitter(
	string Name,
	string FileName,
	string? UnifiedPath,
	Matrix4x4 LocalTransform,
	float SimulationScale);
