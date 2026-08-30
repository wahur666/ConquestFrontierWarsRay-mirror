using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using ConquestFrontierWarsRay.Framework;
using RaySharp.Mesh;
using Raylib_cs;
using RaylibTexture2D = Raylib_cs.Texture2D;

namespace ConquestFrontierWarsRay.Frontend;

internal sealed class LegacyMeshNode3D : Node3D {
	private const int GlOne = 1;
	private const int GlSrcAlpha = 0x0302;
	private const int GlFuncAdd = 0x8006;
	private readonly string _meshPath;
	private readonly Dictionary<string, Matrix4x4> _partLocalTransforms = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, Node3D> _partNodes = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, List<string>> _childrenByParent = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _childPartNames = new(StringComparer.OrdinalIgnoreCase);
	private readonly List<string> _particleTrace = [];
	private Dictionary<string, MeshTextureResource> _textures = new(StringComparer.OrdinalIgnoreCase);
	private MeshTextureLighting? _textureLighting;
	private LegacyMeshAnimationController? _animation;
	private int _particleEmitterCount;
	private int _particleLoadFailures;
	private int _particleMissingPathCount;

	public LegacyMeshNode3D(string meshPath, string? name = null) : base(name ?? "LegacyMeshNode3D") {
		_meshPath = meshPath;
	}

	public MeshGeometry? Geometry { get; private set; }

	public bool IsLoaded => Geometry is not null;
	public string ParticleDebugSummary => BuildParticleDebugSummary();

	public BoundingBox WorldBounds => Geometry is null
		? new BoundingBox(Vector3.Zero, Vector3.One)
		: TransformBounds(Geometry.Bounds, GlobalTransform);

	protected override void OnInitialize() {
		AppLog.Info(Name, $"Loading legacy mesh '{_meshPath}'.");
		Geometry = MeshGeometryLoader.LoadXml(_meshPath, MeshUvMode.Raw);
		AppLog.Info(Name,
			$"Loaded mesh '{Geometry.Name}': triangles={Geometry.Triangles.Count}, parts={Geometry.Parts.Count}, emitters={Geometry.ParticleEmitters.Count}, textures={Geometry.TextureCount}.");
		_textures = MeshTextureLoader.Load(Geometry.TextureImages);
		_textureLighting = MeshTextureLighting.Load();

		foreach (var part in Geometry.Parts) {
			_partLocalTransforms[part.Name] = part.LocalTransform;
		}

		foreach (var emitter in Geometry.ParticleEmitters) {
			_partLocalTransforms.TryAdd(emitter.Name, emitter.LocalTransform);
		}

		foreach (var link in Geometry.Hierarchy) {
			if (!_childrenByParent.TryGetValue(link.ParentName, out var children)) {
				children = [];
				_childrenByParent[link.ParentName] = children;
			}

			children.Add(link.ChildName);
			_childPartNames.Add(link.ChildName);
		}

		BuildPartNodeHierarchy();
		_animation = Geometry.Animations.Count > 0
			? new LegacyMeshAnimationController(Geometry.Animations, _partLocalTransforms)
			: null;
		_animation?.Play();
		LoadParticleSystems();
		LogParticleLoadSummary();
	}

	protected override void OnUpdate(float deltaTime) {
		if (_animation is not null) {
			_animation.Update(deltaTime);
			ApplyPartNodeTransforms();
		}
	}

	protected override void OnDraw() {
		if (Geometry is null || _textureLighting is null) {
			return;
		}

		foreach (var (triangles, world, normal) in RenderBatches()) {
			DrawTriangles(triangles, world, normal);
		}
	}

	protected override void OnDispose() {
		foreach (var texture in _textures.Values) {
			texture.Dispose();
		}
		_textures.Clear();
		_textureLighting?.Unload();
		_textureLighting = null;
		_animation = null;
		Geometry = null;
		base.OnDispose();
	}

	private void DrawTriangles(IReadOnlyList<MeshTriangle> triangles, Matrix4x4 world, Matrix4x4 normalMatrix) {
		var fill = new Color(146, 155, 166, 255);
		var wire = new Color(35, 42, 52, 145);

		foreach (var triangle in triangles) {
			var texture = MeshTextureLoader.Find(_textures, triangle.TextureName);
			if (texture is not null && texture.IsValid) {
				DrawTexturedTriangle(world, normalMatrix, triangle, texture.Texture);
				continue;
			}

			if (triangle.UsesAlphaBlend) {
				Raylib.BeginBlendMode(BlendMode.Alpha);
			}

			if (!triangle.WritesDepth) {
				Rlgl.DisableDepthMask();
			}

			Raylib.DrawTriangle3D(
				TransformPosition(world, triangle.A),
				TransformPosition(world, triangle.B),
				TransformPosition(world, triangle.C),
				triangle.TextureName is null ? triangle.Tint : fill);

			if (!triangle.WritesDepth) {
				Rlgl.EnableDepthMask();
			}

			if (triangle.UsesAlphaBlend) {
				Raylib.EndBlendMode();
			}
		}

		foreach (var triangle in triangles) {
			var secondDiffuseTexture = MeshTextureLoader.Find(_textures, triangle.SecondDiffuseTextureName);
			if (secondDiffuseTexture is not null && secondDiffuseTexture.IsValid) {
				DrawSecondDiffuseTriangle(world, normalMatrix, triangle, secondDiffuseTexture.Texture);
			}
		}

		foreach (var triangle in triangles) {
			var emissiveTexture = MeshTextureLoader.Find(_textures, triangle.EmissiveTextureName);
			if (emissiveTexture is not null && emissiveTexture.IsValid) {
				DrawEmissiveTriangle(world, normalMatrix, triangle, emissiveTexture.Texture);
			}
		}

		foreach (var triangle in triangles) {
			var a = TransformPosition(world, triangle.A);
			var b = TransformPosition(world, triangle.B);
			var c = TransformPosition(world, triangle.C);
			Raylib.DrawLine3D(a, b, wire);
			Raylib.DrawLine3D(b, c, wire);
			Raylib.DrawLine3D(c, a, wire);
		}
	}

	private IEnumerable<(IReadOnlyList<MeshTriangle> Triangles, Matrix4x4 World, Matrix4x4 Normal)> RenderBatches() {
		if (Geometry is null) {
			yield break;
		}

		if (Geometry.Parts.Count == 0) {
			yield return (Geometry.Triangles, GlobalTransform, NormalMatrix(GlobalTransform));
			yield break;
		}

		foreach (var part in Geometry.Parts) {
			var world = _partNodes.TryGetValue(part.Name, out var partNode)
				? partNode.GlobalTransform
				: part.LocalTransform * GlobalTransform;
			yield return (part.Triangles, world, NormalMatrix(world));
		}
	}

	private void BuildPartNodeHierarchy() {
		_partNodes.Clear();
		foreach (var rootPart in _partLocalTransforms.Keys.Where(name => !_childPartNames.Contains(name))) {
			AttachPartNode(rootPart, null);
		}

		foreach (var partName in _partLocalTransforms.Keys) {
			if (!_partNodes.ContainsKey(partName)) {
				AttachPartNode(partName, null);
			}
		}

		ApplyPartNodeTransforms();
	}

	private void LoadParticleSystems() {
		if (Geometry is null) {
			return;
		}

		_particleEmitterCount = Geometry.ParticleEmitters.Count;
		_particleLoadFailures = 0;
		_particleMissingPathCount = 0;
		_particleTrace.Clear();

		for (var index = 0; index < Geometry.ParticleEmitters.Count; index++) {
			var emitter = Geometry.ParticleEmitters[index];
			var particlePath = emitter.UnifiedPath;
			AppLog.Info(Name,
				$"Emitter[{index}] name='{emitter.Name}' file='{emitter.FileName}' resolved='{particlePath ?? "<null>"}' scale={emitter.SimulationScale:0.###}.");
			if (string.IsNullOrWhiteSpace(particlePath) || !File.Exists(particlePath)) {
				_particleMissingPathCount++;
				_particleTrace.Add($"{emitter.Name}: missing particle path");
				AppLog.Warning(Name,
					$"Emitter[{index}] '{emitter.Name}' skipped because particle file was not resolved.");
				continue;
			}

			try {
				var anchor = _partNodes.GetValueOrDefault(emitter.Name) ?? AddChild(new Node3D($"{emitter.Name}_Anchor"));
				var particleNode = anchor.AddChild(new LegacyParticleSystemNode3D(
					particlePath,
					$"{emitter.Name}_Particle",
					emitter.SimulationScale));
				_particleTrace.Add($"{emitter.Name}: ok nestedUnder='{anchor.Name}' file='{Path.GetFileName(particlePath)}'");
				AppLog.Info(Name,
					$"Emitter[{index}] '{emitter.Name}' attached under node '{anchor.Name}' from '{particlePath}'. Child='{particleNode.Name}', simulationScale={emitter.SimulationScale:0.###}.");
			} catch (Exception ex) {
				_particleLoadFailures++;
				_particleTrace.Add($"{emitter.Name}: load failed ({ex.GetType().Name})");
				AppLog.Error(Name, $"Emitter[{index}] '{emitter.Name}' failed to load from '{particlePath}'.", ex);
			}
		}
	}

	private void AttachPartNode(string partName, Node3D? parentNode) {
		if (_partNodes.ContainsKey(partName)) {
			return;
		}

		var partNode = parentNode is null
			? AddChild(new Node3D(partName))
			: parentNode.AddChild(new Node3D(partName));
		_partNodes[partName] = partNode;
		if (!_childrenByParent.TryGetValue(partName, out var children)) {
			return;
		}

		foreach (var child in children) {
			AttachPartNode(child, partNode);
		}
	}

	private void ApplyPartNodeTransforms() {
		foreach (var (partName, transform) in _partLocalTransforms) {
			if (!_partNodes.TryGetValue(partName, out var partNode)) {
				continue;
			}

			ApplyTransform(partNode, transform);
		}
	}

	private void DrawTexturedTriangle(Matrix4x4 world, Matrix4x4 normalMatrix, MeshTriangle triangle, RaylibTexture2D texture) {
		if (_textureLighting is null) {
			return;
		}

		if (triangle.UsesAlphaBlend) {
			Raylib.BeginBlendMode(BlendMode.Alpha);
		}

		if (!triangle.WritesDepth) {
			Rlgl.DisableDepthMask();
		}

		_textureLighting.Begin();
		var textureAddress = triangle.TextureAddress;
		Raylib.SetTextureWrap(texture, ToRaylibTextureWrap(textureAddress));
		Rlgl.SetTexture(texture.Id);
		Rlgl.Begin((int)DrawMode.Triangles);
		Rlgl.Color4ub(triangle.Tint.R, triangle.Tint.G, triangle.Tint.B, triangle.Tint.A);
		EmitTexturedVertex(world, normalMatrix, triangle.A, SelectUv(triangle.UvA, triangle.Uv2A, textureAddress), triangle.NormalA);
		EmitTexturedVertex(world, normalMatrix, triangle.B, SelectUv(triangle.UvB, triangle.Uv2B, textureAddress), triangle.NormalB);
		EmitTexturedVertex(world, normalMatrix, triangle.C, SelectUv(triangle.UvC, triangle.Uv2C, textureAddress), triangle.NormalC);
		Rlgl.End();
		Rlgl.SetTexture(0);
		_textureLighting.End();

		if (!triangle.WritesDepth) {
			Rlgl.EnableDepthMask();
		}

		if (triangle.UsesAlphaBlend) {
			Raylib.EndBlendMode();
		}
	}

	private static void DrawSecondDiffuseTriangle(Matrix4x4 world, Matrix4x4 normalMatrix, MeshTriangle triangle, RaylibTexture2D texture) {
		Raylib.BeginBlendMode(BlendMode.Multiplied);
		Rlgl.DisableDepthMask();
		var textureAddress = triangle.SecondDiffuseTextureAddress;
		Raylib.SetTextureWrap(texture, ToRaylibTextureWrap(textureAddress));
		Rlgl.SetTexture(texture.Id);
		Rlgl.Begin((int)DrawMode.Triangles);
		Rlgl.Color4ub(255, 255, 255, 255);
		EmitTexturedVertex(world, normalMatrix, triangle.A, SelectUv(triangle.UvA, triangle.Uv2A, textureAddress), triangle.NormalA);
		EmitTexturedVertex(world, normalMatrix, triangle.B, SelectUv(triangle.UvB, triangle.Uv2B, textureAddress), triangle.NormalB);
		EmitTexturedVertex(world, normalMatrix, triangle.C, SelectUv(triangle.UvC, triangle.Uv2C, textureAddress), triangle.NormalC);
		Rlgl.End();
		Rlgl.SetTexture(0);
		Rlgl.EnableDepthMask();
		Raylib.EndBlendMode();
	}

	private static void DrawEmissiveTriangle(Matrix4x4 world, Matrix4x4 normalMatrix, MeshTriangle triangle, RaylibTexture2D texture) {
		Rlgl.SetBlendFactors(GlSrcAlpha, GlOne, GlFuncAdd);
		Raylib.BeginBlendMode(BlendMode.Custom);
		Rlgl.DisableDepthTest();
		Rlgl.DisableDepthMask();
		var textureAddress = triangle.EmissiveTextureAddress;
		Raylib.SetTextureWrap(texture, ToRaylibTextureWrap(textureAddress));
		Rlgl.SetTexture(texture.Id);
		Rlgl.Begin((int)DrawMode.Triangles);
		Rlgl.Color4ub(255, 255, 255, triangle.EmissiveBlend);
		EmitTexturedVertex(world, normalMatrix, triangle.A, SelectUv(triangle.UvA, triangle.Uv2A, textureAddress), triangle.NormalA);
		EmitTexturedVertex(world, normalMatrix, triangle.B, SelectUv(triangle.UvB, triangle.Uv2B, textureAddress), triangle.NormalB);
		EmitTexturedVertex(world, normalMatrix, triangle.C, SelectUv(triangle.UvC, triangle.Uv2C, textureAddress), triangle.NormalC);
		Rlgl.End();
		Rlgl.SetTexture(0);
		Rlgl.EnableDepthMask();
		Rlgl.EnableDepthTest();
		Raylib.EndBlendMode();
	}

	private static TextureWrap ToRaylibTextureWrap(MeshTextureAddress address) {
		if (address.U == 1 || address.V == 1) {
			return TextureWrap.MirrorRepeat;
		}

		if (address.U == 2 || address.V == 2 || address.U == 3 || address.V == 3) {
			return TextureWrap.Clamp;
		}

		return TextureWrap.Repeat;
	}

	private static Vector2 SelectUv(Vector2 uv0, Vector2 uv1, MeshTextureAddress address) {
		return address.CoordinateSet == 1 ? uv1 : uv0;
	}

	private static void EmitTexturedVertex(Matrix4x4 world, Matrix4x4 normalMatrix, Vector3 position, Vector2 uv, Vector3 normal) {
		var transformedPosition = TransformPosition(world, position);
		var transformedNormal = TransformNormal(normalMatrix, normal);
		Rlgl.TexCoord2f(uv.X, uv.Y);
		Rlgl.Normal3f(transformedNormal.X, transformedNormal.Y, transformedNormal.Z);
		Rlgl.Vertex3f(transformedPosition.X, transformedPosition.Y, transformedPosition.Z);
	}

	private static Matrix4x4 NormalMatrix(Matrix4x4 world) {
		return Matrix4x4.Invert(world, out var inverseWorld)
			? Matrix4x4.Transpose(inverseWorld)
			: Matrix4x4.Identity;
	}

	private static Vector3 TransformPosition(Matrix4x4 world, Vector3 position) {
		return Vector3.Transform(position, world);
	}

	private static Vector3 TransformNormal(Matrix4x4 normalMatrix, Vector3 normal) {
		var transformed = Vector3.TransformNormal(normal, normalMatrix);
		return transformed == Vector3.Zero ? normal : Vector3.Normalize(transformed);
	}

	private static BoundingBox TransformBounds(BoundingBox bounds, Matrix4x4 worldMatrix) {
		var min = bounds.Min;
		var max = bounds.Max;
		var corners = new[] {
			new Vector3(min.X, min.Y, min.Z),
			new Vector3(max.X, min.Y, min.Z),
			new Vector3(min.X, max.Y, min.Z),
			new Vector3(max.X, max.Y, min.Z),
			new Vector3(min.X, min.Y, max.Z),
			new Vector3(max.X, min.Y, max.Z),
			new Vector3(min.X, max.Y, max.Z),
			new Vector3(max.X, max.Y, max.Z)
		};

		var transformedMin = Vector3.Transform(corners[0], worldMatrix);
		var transformedMax = transformedMin;
		for (var index = 1; index < corners.Length; index++) {
			var corner = Vector3.Transform(corners[index], worldMatrix);
			transformedMin = Vector3.Min(transformedMin, corner);
			transformedMax = Vector3.Max(transformedMax, corner);
		}

		return new BoundingBox(transformedMin, transformedMax);
	}

	private string BuildParticleDebugSummary() {
		var summary = new StringBuilder();
		var loadedParticleNodes = _partNodes.Values.Sum(node => node.Children.Count(child => child is LegacyParticleSystemNode3D));
		summary.Append($"Emitters {_particleEmitterCount}  Loaded {loadedParticleNodes}  MissingPath {_particleMissingPathCount}  LoadFail {_particleLoadFailures}");
		if (_particleTrace.Count > 0) {
			summary.Append("  |  ");
			summary.Append(string.Join(" ; ", _particleTrace.Take(2)));
			if (_particleTrace.Count > 2) {
				summary.Append($" ; +{_particleTrace.Count - 2} more");
			}
		}

		return summary.ToString();
	}

	private void LogParticleLoadSummary() {
		AppLog.Info(Name,
			$"Particle load summary: emitters={_particleEmitterCount}, loadedSystems={_partNodes.Values.Sum(node => node.Children.Count(child => child is LegacyParticleSystemNode3D))}, missingPath={_particleMissingPathCount}, loadFailures={_particleLoadFailures}.");
		foreach (var traceLine in _particleTrace) {
			AppLog.Debug(Name, traceLine);
		}
	}

	private static void ApplyTransform(Node3D node, Matrix4x4 transform) {
		if (!Matrix4x4.Decompose(transform, out var scale, out var rotation, out var translation)) {
			scale = Vector3.One;
			rotation = Quaternion.Identity;
			translation = Vector3.Zero;
		}

		node.Position = translation;
		node.Rotation = rotation.LengthSquared() > 0.000001f ? Quaternion.Normalize(rotation) : Quaternion.Identity;
		node.Scale = scale;
	}

	private sealed class LegacyMeshAnimationController {
		private const float AnimatedTranslationScale = 0.01f;
		private readonly Dictionary<string, Matrix4x4> _partTransforms;
		private readonly Dictionary<string, Matrix4x4> _bindTransforms;

		public LegacyMeshAnimationController(IReadOnlyList<MeshAnimationClip> clips, Dictionary<string, Matrix4x4> partTransforms) {
			Clips = clips;
			_partTransforms = partTransforms;
			_bindTransforms = partTransforms.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
			ActiveClipIndex = SelectDefaultClip(clips);
			Apply();
		}

		public IReadOnlyList<MeshAnimationClip> Clips { get; }
		public int ActiveClipIndex { get; }
		public MeshAnimationClip? ActiveClip => Clips.Count == 0 ? null : Clips[ActiveClipIndex];
		public float Time { get; private set; }
		public int Direction { get; private set; } = 1;
		public bool Playing { get; private set; }
		public bool Loop { get; set; } = true;
		public bool PingPong { get; set; }

		public void Play() {
			Playing = true;
		}

		public void Update(float deltaTime) {
			var clip = ActiveClip;
			if (!Playing || clip is null) {
				return;
			}

			Time += deltaTime * Direction;
			ConstrainTime(clip.Duration);
			ResetBindTransforms();
			Apply();
		}

		private void ConstrainTime(float duration) {
			if (duration <= 0f) {
				Time = 0f;
				Playing = false;
				return;
			}

			if (PingPong) {
				while (Time > duration || Time < 0f) {
					if (Time > duration) {
						Time = duration - (Time - duration);
						Direction = -1;
					} else {
						Time = -Time;
						Direction = 1;
						if (!Loop) {
							Time = 0f;
							Playing = false;
							break;
						}
					}
				}

				return;
			}

			if (Loop) {
				Time %= duration;
				if (Time < 0f) {
					Time += duration;
				}
			} else if (Time >= duration) {
				Time = duration;
				Playing = false;
			}
		}

		private void Apply() {
			var clip = ActiveClip;
			if (clip is null) {
				return;
			}

			foreach (var track in clip.Tracks) {
				if (!_partTransforms.ContainsKey(track.TargetName)) {
					continue;
				}

				if (track.TargetType == MeshAnimationTargetType.Object && track.Channel.Kind == MeshAnimationChannelKind.FullTransform) {
					var sample = SampleTransform(track.Channel.TransformFrames, Time);
					ApplyObjectFullTransform(track.TargetName, sample);
				} else if (track.Channel.Kind == MeshAnimationChannelKind.FullTransform && track.Joint is MeshCompoundJoint fullJoint) {
					var sample = SampleTransform(track.Channel.TransformFrames, Time);
					ApplyFullJointTransform(track.TargetName, fullJoint, sample);
				} else if (track.Channel.Kind == MeshAnimationChannelKind.Float && track.Joint is MeshCompoundJoint floatJoint) {
					var sample = SampleFloat(track.Channel.FloatFrames, Time, floatJoint);
					ApplyFloatJointTransform(track.TargetName, floatJoint, sample.Value);
				} else if (track.Channel.Kind == MeshAnimationChannelKind.Quaternion && track.Joint is MeshCompoundJoint quatJoint) {
					var sample = SampleQuaternion(track.Channel.QuaternionFrames, Time);
					ApplyQuaternionJointTransform(track.TargetName, quatJoint, sample.Rotation);
				} else if (track.Channel.Kind == MeshAnimationChannelKind.Vector && track.Joint is MeshCompoundJoint vectorJoint) {
					var sample = SampleVector(track.Channel.VectorFrames, Time);
					ApplyVectorJointTransform(track.TargetName, vectorJoint, sample.Position);
				}
			}
		}

		private void ApplyObjectFullTransform(string targetName, MeshTransformKeyframe sample) {
			var bind = _bindTransforms.GetValueOrDefault(targetName, Matrix4x4.Identity);
			Matrix4x4.Decompose(bind, out var scale, out var rotation, out var translation);
			var animatedPosition = translation + Vector3.Transform(sample.Position, rotation);
			var animatedRotation = Quaternion.Normalize(rotation * sample.Rotation);
			_partTransforms[targetName] = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(animatedRotation) * Matrix4x4.CreateTranslation(animatedPosition);
		}

		private void ApplyFullJointTransform(string targetName, MeshCompoundJoint joint, MeshTransformKeyframe sample) {
			var relOrientation = MatrixFromRotation(joint.RelativeOrientation);
			var rotation = Matrix4x4.CreateFromQuaternion(sample.Rotation) * relOrientation;
			if (joint.Type is MeshCompoundJointType.Revolute or MeshCompoundJointType.Prismatic or MeshCompoundJointType.Spherical) {
				var childPoint = Vector3.Transform(joint.ChildPoint, rotation);
				_partTransforms[targetName] = MatrixWithTranslation(rotation, joint.ParentPoint + sample.Position - childPoint);
			} else {
				_partTransforms[targetName] = MatrixWithTranslation(rotation, joint.ParentPoint + sample.Position);
			}
		}

		private void ApplyQuaternionJointTransform(string targetName, MeshCompoundJoint joint, Quaternion rotationValue) {
			if (joint.Type != MeshCompoundJointType.Spherical) {
				return;
			}

			var rotation = Matrix4x4.CreateFromQuaternion(rotationValue) * MatrixFromRotation(joint.RelativeOrientation);
			var childPoint = Vector3.Transform(joint.ChildPoint, rotation);
			_partTransforms[targetName] = MatrixWithTranslation(rotation, joint.ParentPoint - childPoint);
		}

		private void ApplyVectorJointTransform(string targetName, MeshCompoundJoint joint, Vector3 position) {
			if (joint.Type != MeshCompoundJointType.Translational) {
				return;
			}

			_partTransforms[targetName] = MatrixFromRotationTranslation(joint.RelativeOrientation, joint.ParentPoint + position);
		}

		private void ApplyFloatJointTransform(string targetName, MeshCompoundJoint joint, float value) {
			if (joint.Type == MeshCompoundJointType.Revolute) {
				var axis = NormalizeOrDefault(joint.Axis, Vector3.UnitY);
				var rotation = Matrix4x4.CreateFromAxisAngle(axis, value) * MatrixFromRotation(joint.RelativeOrientation);
				var childPoint = Vector3.Transform(joint.ChildPoint, rotation);
				_partTransforms[targetName] = MatrixWithTranslation(rotation, joint.ParentPoint - childPoint);
			} else if (joint.Type == MeshCompoundJointType.Prismatic) {
				var axis = joint.Axis.LengthSquared() > 0.000001f ? Vector3.Normalize(joint.Axis) * value * AnimatedTranslationScale : Vector3.Zero;
				var relOrientation = MatrixFromRotation(joint.RelativeOrientation);
				var childPoint = Vector3.Transform(joint.ChildPoint, relOrientation);
				_partTransforms[targetName] = MatrixWithTranslation(relOrientation, joint.ParentPoint + axis - childPoint);
			}
		}

		private void ResetBindTransforms() {
			foreach (var (name, transform) in _bindTransforms) {
				_partTransforms[name] = transform;
			}
		}

		private static int SelectDefaultClip(IReadOnlyList<MeshAnimationClip> clips) {
			if (clips.Count == 0) {
				return 0;
			}

			var best = 0;
			for (var index = 1; index < clips.Count; index++) {
				if (clips[index].Duration > clips[best].Duration) {
					best = index;
				}
			}

			return best;
		}

		private static MeshFloatKeyframe SampleFloat(IReadOnlyList<MeshFloatKeyframe> frames, float time, MeshCompoundJoint joint) {
			var (a, b, t) = Bracket(frames, time);
			var value = joint.Type == MeshCompoundJointType.Revolute
				? InterpolateArc(a.Value, b.Value, t)
				: Lerp(a.Value, b.Value, t);
			return new MeshFloatKeyframe(time, value);
		}

		private static MeshVectorKeyframe SampleVector(IReadOnlyList<MeshVectorKeyframe> frames, float time) {
			var (a, b, t) = Bracket(frames, time);
			return new MeshVectorKeyframe(time, Vector3.Lerp(a.Position, b.Position, t));
		}

		private static MeshQuaternionKeyframe SampleQuaternion(IReadOnlyList<MeshQuaternionKeyframe> frames, float time) {
			var (a, b, t) = Bracket(frames, time);
			return new MeshQuaternionKeyframe(time, Quaternion.Slerp(a.Rotation, b.Rotation, t));
		}

		private static MeshTransformKeyframe SampleTransform(IReadOnlyList<MeshTransformKeyframe> frames, float time) {
			var (a, b, t) = Bracket(frames, time);
			return new MeshTransformKeyframe(time, Vector3.Lerp(a.Position, b.Position, t), Quaternion.Slerp(a.Rotation, b.Rotation, t));
		}

		private static (T A, T B, float T) Bracket<T>(IReadOnlyList<T> frames, float time) where T : notnull {
			if (frames.Count == 1 || time <= KeyTime(frames[0])) {
				return (frames[0], frames[0], 0f);
			}

			var last = frames[^1];
			if (time >= KeyTime(last)) {
				return (last, last, 0f);
			}

			var nextIndex = 1;
			while (nextIndex < frames.Count && KeyTime(frames[nextIndex]) < time) {
				nextIndex++;
			}

			var previous = frames[nextIndex - 1];
			var next = frames[nextIndex];
			var span = Math.Max(KeyTime(next) - KeyTime(previous), 0.000001f);
			return (previous, next, (time - KeyTime(previous)) / span);
		}

		private static float KeyTime<T>(T frame) {
			return frame switch {
				MeshFloatKeyframe value => value.Time,
				MeshVectorKeyframe value => value.Time,
				MeshQuaternionKeyframe value => value.Time,
				MeshTransformKeyframe value => value.Time,
				_ => 0f
			};
		}

		private static float InterpolateArc(float a, float b, float t) {
			var adjusted = b;
			if (adjusted - a < -MathF.PI) {
				adjusted += MathF.PI * 2f;
			} else if (adjusted - a > MathF.PI) {
				adjusted -= MathF.PI * 2f;
			}

			return Lerp(a, adjusted, t);
		}

		private static float Lerp(float a, float b, float t) {
			return a + (b - a) * t;
		}

		private static Vector3 NormalizeOrDefault(Vector3 value, Vector3 fallback) {
			return value.LengthSquared() > 0.000001f ? Vector3.Normalize(value) : fallback;
		}

		private static Matrix4x4 MatrixFromRotation(Matrix3x3 rotation) {
			return new Matrix4x4(
				rotation.E00, rotation.E10, rotation.E20, 0f,
				rotation.E01, rotation.E11, rotation.E21, 0f,
				rotation.E02, rotation.E12, rotation.E22, 0f,
				0f, 0f, 0f, 1f);
		}

		private static Matrix4x4 MatrixFromRotationTranslation(Matrix3x3 rotation, Vector3 translation) {
			return MatrixWithTranslation(MatrixFromRotation(rotation), translation);
		}

		private static Matrix4x4 MatrixWithTranslation(Matrix4x4 matrix, Vector3 translation) {
			matrix.M41 = translation.X;
			matrix.M42 = translation.Y;
			matrix.M43 = translation.Z;
			matrix.M44 = 1f;
			return matrix;
		}
	}
}
