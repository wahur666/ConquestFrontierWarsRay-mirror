using System.Numerics;
using RaySharp.Mesh;
using RaySharp.Particle;
using Raylib_cs;

namespace RaySharp.MeshViewer;

internal sealed class MeshViewerModel : RenderableObject {
	private MeshViewerModel(
		MeshGeometry geometry,
		Dictionary<string, MeshTextureResource> textures,
		List<MeshViewerParticleSystem> particleSystems,
		Dictionary<string, Matrix4x4> partLocalTransforms,
		MeshViewerAnimationController? animation,
		Transform transform)
		: base(transform) {
		Geometry = geometry;
		Textures = textures;
		ParticleSystems = particleSystems;
		this.partLocalTransforms = partLocalTransforms;
		partWorldTransforms = new Dictionary<string, Matrix4x4>(StringComparer.OrdinalIgnoreCase);
		(childrenByParent, childPartNames) = BuildHierarchy(geometry.Hierarchy);
		Animation = animation;
		RebuildMatrices();
	}

	public MeshGeometry Geometry { get; }
	public IReadOnlyDictionary<string, MeshTextureResource> Textures { get; private set; }
	public IReadOnlyList<MeshViewerParticleSystem> ParticleSystems { get; }
	public IReadOnlyDictionary<string, Matrix4x4> PartLocalTransforms => partLocalTransforms;
	public IReadOnlyDictionary<string, Matrix4x4> PartWorldTransforms => partWorldTransforms;
	public MeshViewerAnimationController? Animation { get; }
	public Matrix4x4 WorldMatrix { get; private set; }
	public Matrix4x4 NormalMatrix { get; private set; }
	public BoundingBox Bounds { get; private set; }
	public int UploadedTextureCount => Textures.Count;
	public int ParticleSystemCount => ParticleSystems.Count;
	public int ActiveParticleCount => ParticleSystems.Sum(system => system.System.Particles.Count);
	public int UploadedParticleTextureCount => ParticleSystems.Count(system => system.TextureResource.IsValid);
	private readonly Dictionary<string, Matrix4x4> partLocalTransforms;
	private readonly Dictionary<string, Matrix4x4> partWorldTransforms;
	private readonly Dictionary<string, List<string>> childrenByParent;
	private readonly HashSet<string> childPartNames;
	private readonly Dictionary<string, Quaternion> manualPartRotations = new(StringComparer.OrdinalIgnoreCase);

	public static MeshViewerModel Load(string path, MeshUvMode uvMode) {
		return Load(path, uvMode, new Transform());
	}

	public static MeshViewerModel Load(string path, MeshUvMode uvMode, Vector3 origin, Quaternion rotation, Vector3 scale) {
		return Load(path, uvMode, new Transform(origin, rotation, scale));
	}

	public static MeshViewerModel Load(string path, MeshUvMode uvMode, Transform transform) {
		MeshGeometry geometry = MeshGeometryLoader.LoadXml(path, uvMode);
		Dictionary<string, MeshTextureResource> textures = MeshTextureLoader.Load(geometry.TextureImages);
		Dictionary<string, Matrix4x4> partLocalTransforms = geometry.Parts.ToDictionary(part => part.Name, part => part.LocalTransform, StringComparer.OrdinalIgnoreCase);
		foreach (MeshParticleEmitter emitter in geometry.ParticleEmitters) {
			partLocalTransforms.TryAdd(emitter.Name, emitter.LocalTransform);
		}

		MeshViewerAnimationController? animation = geometry.Animations.Count > 0
			? new MeshViewerAnimationController(geometry.Animations, partLocalTransforms)
			: null;
		List<MeshViewerParticleSystem> particleSystems = LoadParticleSystems(geometry);
		return new MeshViewerModel(geometry, textures, particleSystems, partLocalTransforms, animation, transform);
	}

	public Vector3 TransformPosition(Vector3 position) {
		return Vector3.Transform(position, WorldMatrix);
	}

	public Vector3 TransformNormal(Vector3 normal) {
		Vector3 transformed = Vector3.TransformNormal(normal, NormalMatrix);
		return transformed == Vector3.Zero ? normal : Vector3.Normalize(transformed);
	}

	public void SetTransform(Transform transform) {
		Transform.Set(transform.Position, transform.Rotation, transform.Scale);
		RebuildMatrices();
	}

	public void SetPosition(Vector3 position) {
		Transform.Position = position;
		RebuildMatrices();
	}

	public void SetRotation(Quaternion rotation) {
		Transform.Rotation = rotation;
		RebuildMatrices();
	}

	public void SetScale(Vector3 scale) {
		Transform.Scale = scale;
		RebuildMatrices();
	}

	public override void Update(RenderUpdateContext context) {
		Update(context.DeltaTime);
	}

	public void Update(float deltaTime) {
		Animation?.Update(deltaTime);
		ApplyManualRotations();
		RebuildPartWorldTransforms();
		RebuildParticleTransforms();
		foreach (MeshViewerParticleSystem particleSystem in ParticleSystems) {
			particleSystem.Update(deltaTime);
		}
	}

	public override void Render(RenderContext context) {
		context.ModelRenderer.Draw(this);
	}

	public void Translate(Vector3 delta) {
		SetPosition(Transform.Position + delta);
	}

	public void Rotate(Quaternion delta) {
		SetRotation(delta * Transform.Rotation);
	}

	public void ScaleBy(float factor) {
		float safeFactor = Math.Max(0.01f, factor);
		SetScale(Transform.Scale * safeFactor);
	}

	public bool SetPartLocalTransform(string partName, Matrix4x4 transform) {
		if (!partLocalTransforms.ContainsKey(partName)) {
			return false;
		}

		partLocalTransforms[partName] = transform;
		RebuildPartWorldTransforms();
		RebuildParticleTransforms();
		return true;
	}

	public bool SetPartRotation(string partName, Quaternion rotation) {
		if (!partLocalTransforms.ContainsKey(partName)) {
			return false;
		}

		manualPartRotations[partName] = Quaternion.Normalize(rotation);
		ApplyManualRotations();
		RebuildPartWorldTransforms();
		RebuildParticleTransforms();
		return true;
	}

	public bool ClearPartRotation(string partName) {
		if (!manualPartRotations.Remove(partName)) {
			return false;
		}

		RebuildPartWorldTransforms();
		RebuildParticleTransforms();
		return true;
	}

	public void SetParticleVisualSizeMultiplier(float multiplier) {
		float safeMultiplier = Math.Max(0.001f, multiplier);
		foreach (MeshViewerParticleSystem particleSystem in ParticleSystems) {
			particleSystem.SetVisualSizeMultiplier(safeMultiplier);
		}
	}

	public void SetParticleOriginOffset(Vector3 offset) {
		foreach (MeshViewerParticleSystem particleSystem in ParticleSystems) {
			particleSystem.SetOriginOffset(offset);
		}
	}

	public override void Dispose() {
		foreach (MeshTextureResource texture in Textures.Values) {
			texture.Dispose();
		}

		Textures = new Dictionary<string, MeshTextureResource>(StringComparer.OrdinalIgnoreCase);
		foreach (MeshViewerParticleSystem particleSystem in ParticleSystems) {
			particleSystem.Dispose();
		}
	}

	private void RebuildMatrices() {
		WorldMatrix = Transform.WorldMatrix;
		NormalMatrix = Transform.NormalMatrix;
		RebuildPartWorldTransforms();
		RebuildParticleTransforms();
		RebuildBounds();
	}

	private void ApplyManualRotations() {
		foreach ((string name, Quaternion rotation) in manualPartRotations) {
			if (!partLocalTransforms.TryGetValue(name, out Matrix4x4 current)) {
				continue;
			}

			Matrix4x4.Decompose(current, out Vector3 scale, out _, out Vector3 translation);
			partLocalTransforms[name] = Matrix4x4.CreateScale(scale)
				* Matrix4x4.CreateFromQuaternion(rotation)
				* Matrix4x4.CreateTranslation(translation);
		}
	}

	private void RebuildParticleTransforms() {
		foreach (MeshViewerParticleSystem particleSystem in ParticleSystems) {
			Matrix4x4 compoundWorld = partWorldTransforms.GetValueOrDefault(particleSystem.Name, particleSystem.LocalTransform);
			particleSystem.SetCompoundWorld(compoundWorld, WorldMatrix);
		}

		RebuildBounds();
	}

	private void RebuildPartWorldTransforms() {
		partWorldTransforms.Clear();
		foreach (string rootPartName in partLocalTransforms.Keys.Where(name => !childPartNames.Contains(name))) {
			AttachPart(rootPartName, Matrix4x4.Identity);
		}

		foreach (string partName in partLocalTransforms.Keys) {
			if (!partWorldTransforms.ContainsKey(partName)) {
				AttachPart(partName, Matrix4x4.Identity);
			}
		}
	}

	private void AttachPart(string partName, Matrix4x4 parentWorld) {
		if (partWorldTransforms.ContainsKey(partName)) {
			return;
		}

		Matrix4x4 local = partLocalTransforms.GetValueOrDefault(partName, Matrix4x4.Identity);
		Matrix4x4 world = local * parentWorld;
		partWorldTransforms[partName] = world;
		if (!childrenByParent.TryGetValue(partName, out List<string>? children)) {
			return;
		}

		foreach (string child in children) {
			AttachPart(child, world);
		}
	}

	private static (Dictionary<string, List<string>> ChildrenByParent, HashSet<string> ChildPartNames) BuildHierarchy(
		IReadOnlyList<MeshPartHierarchyLink> hierarchy) {
		Dictionary<string, List<string>> childrenByParent = new(StringComparer.OrdinalIgnoreCase);
		HashSet<string> childPartNames = new(StringComparer.OrdinalIgnoreCase);
		foreach (MeshPartHierarchyLink link in hierarchy) {
			if (string.IsNullOrWhiteSpace(link.ParentName) || string.IsNullOrWhiteSpace(link.ChildName)) {
				continue;
			}

			if (!childrenByParent.TryGetValue(link.ParentName, out List<string>? children)) {
				children = [];
				childrenByParent[link.ParentName] = children;
			}

			children.Add(link.ChildName);
			childPartNames.Add(link.ChildName);
		}

		return (childrenByParent, childPartNames);
	}

	private static List<MeshViewerParticleSystem> LoadParticleSystems(MeshGeometry geometry) {
		List<MeshViewerParticleSystem> systems = [];
		foreach (MeshParticleEmitter emitter in geometry.ParticleEmitters) {
			if (string.IsNullOrWhiteSpace(emitter.UnifiedPath) || !File.Exists(emitter.UnifiedPath)) {
				continue;
			}

			ParticleLoadResult result = ParticleParameterLoader.Load(emitter.UnifiedPath);
			ParticleParameters parameters = ScaleParticleParameters(result.Parameters, emitter.SimulationScale);
			ParticleTextureResource texture = ParticleTextureLoader.Load(result.Textures, parameters.TextureName);
			ParticlePreviewSystem system = new(parameters);
			systems.Add(new MeshViewerParticleSystem(
				emitter.Name,
				emitter.FileName,
				emitter.UnifiedPath,
				emitter.SimulationScale,
				emitter.LocalTransform,
				system,
				texture));
		}

		return systems;
	}

	private void RebuildBounds() {
		BoundingBox bounds = TransformBounds(Geometry.Bounds, WorldMatrix);
		foreach (MeshViewerParticleSystem particleSystem in ParticleSystems) {
			float radius = ParticleBoundsRadius(particleSystem);
			if (radius <= 0.0f) {
				continue;
			}

			Vector3 extent = new(radius);
			Vector3 origin = particleSystem.WorldOrigin;
			bounds = new BoundingBox(
				Vector3.Min(bounds.Min, origin - extent),
				Vector3.Max(bounds.Max, origin + extent));
		}

		Bounds = bounds;
	}

	private static ParticleParameters ScaleParticleParameters(ParticleParameters source, float scale) {
		if (Math.Abs(scale - 1.0f) <= 0.0001f) {
			return source;
		}

		ParticleParameters scaled = source.Clone();
		scaled.ParticlePositionRandomizer *= scale;
		scaled.ParticleVelocity *= scale;
		scaled.ParticleSize *= scale;
		scaled.ParticleSizeVelocity *= scale;
		scaled.BoundingSphereRadius *= scale;
		return scaled;
	}

	private static float ParticleBoundsRadius(MeshViewerParticleSystem particleSystem) {
		ParticleParameters parameters = particleSystem.System.Parameters;
		float radius = parameters.BoundingSphereRadius != 0.0f
			? parameters.BoundingSphereRadius
			: parameters.ParticleSize != 0.0f
				? parameters.ParticleSize * 8.0f
				: 0.0f;
		if (radius <= 0.0f) {
			return 0.0f;
		}

		Vector3 scale = particleSystem.System.Transform.Scale;
		float scaleFactor = Math.Max(Math.Abs(scale.X), Math.Max(Math.Abs(scale.Y), Math.Abs(scale.Z)));
		return radius * Math.Max(0.01f, scaleFactor);
	}

	private static BoundingBox TransformBounds(BoundingBox bounds, Matrix4x4 worldMatrix) {
		Vector3 min = bounds.Min;
		Vector3 max = bounds.Max;
		Vector3[] corners = [
			new(min.X, min.Y, min.Z),
			new(max.X, min.Y, min.Z),
			new(min.X, max.Y, min.Z),
			new(max.X, max.Y, min.Z),
			new(min.X, min.Y, max.Z),
			new(max.X, min.Y, max.Z),
			new(min.X, max.Y, max.Z),
			new(max.X, max.Y, max.Z)
		];

		Vector3 transformedMin = Vector3.Transform(corners[0], worldMatrix);
		Vector3 transformedMax = transformedMin;
		for (int index = 1; index < corners.Length; index++) {
			Vector3 corner = Vector3.Transform(corners[index], worldMatrix);
			transformedMin = Vector3.Min(transformedMin, corner);
			transformedMax = Vector3.Max(transformedMax, corner);
		}

		return new BoundingBox(transformedMin, transformedMax);
	}
}

internal sealed class MeshViewerParticleSystem : RenderableObject {
	public MeshViewerParticleSystem(
		string name,
		string fileName,
		string? unifiedPath,
		float simulationScale,
		Matrix4x4 localTransform,
		ParticlePreviewSystem system,
		ParticleTextureResource textureResource) {
		Name = name;
		FileName = fileName;
		UnifiedPath = unifiedPath;
		SimulationScale = simulationScale;
		LocalTransform = localTransform;
		System = system;
		TextureResource = textureResource;
		baseParticleSize = system.Parameters.ParticleSize;
		baseParticleSizeVelocity = system.Parameters.ParticleSizeVelocity;
	}

	public string Name { get; }
	public string FileName { get; }
	public string? UnifiedPath { get; }
	public float SimulationScale { get; }
	public Matrix4x4 LocalTransform { get; }
	public ParticlePreviewSystem System { get; }
	public ParticleTextureResource TextureResource { get; }
	public Vector3 WorldOrigin => Transform.Position;
	private readonly float baseParticleSize;
	private readonly float baseParticleSizeVelocity;
	private Matrix4x4 compoundWorld = Matrix4x4.Identity;
	private Matrix4x4 modelWorld = Matrix4x4.Identity;
	private Vector3 originOffset = Vector3.Zero;
	private float visualSizeMultiplier = 1.0f;

	public void SetParentWorld(Matrix4x4 parentWorld) {
		modelWorld = parentWorld;
		compoundWorld = LocalTransform;
		RebuildTransform();
	}

	public void SetCompoundWorld(Matrix4x4 compoundWorld, Matrix4x4 modelWorld) {
		this.compoundWorld = compoundWorld;
		this.modelWorld = modelWorld;
		RebuildTransform();
	}

	public void SetOriginOffset(Vector3 offset) {
		originOffset = offset;
		RebuildTransform();
	}

	public void SetVisualSizeMultiplier(float multiplier) {
		float safeMultiplier = Math.Max(0.001f, multiplier);
		if (Math.Abs(safeMultiplier - visualSizeMultiplier) <= 0.0001f) {
			return;
		}

		float ratio = safeMultiplier / visualSizeMultiplier;
		visualSizeMultiplier = safeMultiplier;
		System.Parameters.ParticleSize = baseParticleSize * visualSizeMultiplier;
		System.Parameters.ParticleSizeVelocity = baseParticleSizeVelocity * visualSizeMultiplier;
		foreach (ParticleInstance particle in System.Particles) {
			particle.Size *= ratio;
		}
	}

	public override void Update(RenderUpdateContext context) {
		Update(context.DeltaTime);
	}

	public void Update(float deltaTime) {
		System.Update(deltaTime);
	}

	public override void Render(RenderContext context) {
		// Particle rendering needs a camera and texture, so MeshViewerScene/ParticleRenderer draw particles explicitly.
	}

	public override void Dispose() {
		System.Dispose();
		TextureResource.Dispose();
	}

	private static Transform ToTransform(Matrix4x4 matrix) {
		return Matrix4x4.Decompose(matrix, out Vector3 scale, out Quaternion rotation, out Vector3 translation)
			? new Transform(translation, Quaternion.Normalize(rotation), scale)
			: new Transform();
	}

	private void RebuildTransform() {
		Matrix4x4 world = compoundWorld * modelWorld * Matrix4x4.CreateTranslation(originOffset);
		Transform transform = ToTransform(world);
		Transform.Set(transform.Position, transform.Rotation, transform.Scale);
		System.SetTransform(Transform);
	}
}
