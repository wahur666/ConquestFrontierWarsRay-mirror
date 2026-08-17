using System.Numerics;
using Raylib_cs;
using RlCamera3D = Raylib_cs.Camera3D;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Visibility result for a projected 3D object.
/// </summary>
public enum CameraVisibilityState {
	NotVisible,
	PartiallyVisible,
	FullyVisible,
	SubPixel
}

/// <summary>
/// Optional viewport used by camera projection helpers.
/// </summary>
public readonly record struct CameraViewport(float X, float Y, float Width, float Height) {
	public float CenterX => X + (Width * 0.5f);

	public float CenterY => Y + (Height * 0.5f);
}

/// <summary>
/// Scene-tree camera node that mirrors raylib's <see cref="RlCamera3D"/> and delegates projection work to raylib where possible.
/// </summary>
public sealed class Camera3DNode : Node3D {
	private const float DefaultWidth = 640f;
	private const float DefaultHeight = 480f;
	private const float Epsilon = 0.000001f;

	/// <summary>
	/// Creates a 3D camera node.
	/// </summary>
	public Camera3DNode(string? name = null) : base(name) {
	}

	/// <summary>
	/// Perspective field of view in degrees.
	/// </summary>
	public float FovY { get; set; } = 45f;

	/// <summary>
	/// Near clip plane distance used by framework helpers.
	/// </summary>
	public float ZNear { get; set; } = 0.1f;

	/// <summary>
	/// Far clip plane distance used by framework helpers.
	/// </summary>
	public float ZFar { get; set; } = 1000f;

	/// <summary>
	/// Projection mode forwarded to raylib.
	/// </summary>
	public CameraProjection Projection { get; set; } = CameraProjection.Perspective;

	/// <summary>
	/// Optional viewport used by projection helpers. When unset, the active window size is used.
	/// </summary>
	public CameraViewport? ViewportOverride { get; set; }

	/// <summary>
	/// World-space target point used by the underlying raylib camera.
	/// </summary>
	public Vector3 Target { get; set; } = Vector3.Zero;

	/// <summary>
	/// Camera up vector used by the underlying raylib camera.
	/// </summary>
	public Vector3 Up { get; set; } = Vector3.UnitY;

	/// <summary>
	/// World-space point the camera looks at.
	/// </summary>
	public Vector3 LookPosition => Target;

	/// <summary>
	/// Forward direction derived from the current raylib-style target.
	/// </summary>
	public Vector3 Forward => NormalizeOrFallback(Target - GlobalPosition, TransformDirection(-Vector3.UnitZ), allowZeroFallback: false);

	/// <summary>
	/// Up direction derived from the current raylib-style up vector.
	/// </summary>
	public Vector3 UpDirection => NormalizeOrFallback(Up, Vector3.UnitY, allowZeroFallback: false);

	/// <summary>
	/// Right direction derived from the current target and up vectors.
	/// </summary>
	public Vector3 RightDirection => NormalizeOrFallback(Vector3.Cross(Forward, UpDirection), Vector3.UnitX, allowZeroFallback: false);

	/// <summary>
	/// Current viewport used for projection math.
	/// </summary>
	public CameraViewport Viewport => ResolveViewport();

	/// <summary>
	/// Aspect ratio derived from the active viewport.
	/// </summary>
	public float Aspect {
		get {
			var viewport = ResolveViewport();
			return viewport.Height > Epsilon
				? viewport.Width / viewport.Height
				: DefaultWidth / DefaultHeight;
		}
	}

	/// <summary>
	/// Horizontal field of view in degrees.
	/// </summary>
	public float FovX {
		get {
			if (Projection == CameraProjection.Orthographic) {
				return FovY * Aspect;
			}

			var halfFovYRadians = DegreesToRadians(FovY) * 0.5f;
			var tanHalfFovX = MathF.Tan(halfFovYRadians) * Aspect;
			return RadiansToDegrees(MathF.Atan(tanHalfFovX) * 2f);
		}
	}

	/// <summary>
	/// Horizontal pixels per clip-space unit at the near plane.
	/// </summary>
	public float HorizontalPixelsPerClipUnit {
		get {
			var viewport = ResolveViewport();
			return Projection == CameraProjection.Orthographic
				? viewport.Width / Math.Max(FovY * Aspect, Epsilon)
				: viewport.CenterX / Math.Max(ZNear * MathF.Tan(DegreesToRadians(FovX) * 0.5f), Epsilon);
		}
	}

	/// <summary>
	/// Vertical pixels per clip-space unit at the near plane.
	/// </summary>
	public float VerticalPixelsPerClipUnit {
		get {
			var viewport = ResolveViewport();
			return Projection == CameraProjection.Orthographic
				? -viewport.Height / Math.Max(FovY, Epsilon)
				: -viewport.CenterY / Math.Max(ZNear * MathF.Tan(DegreesToRadians(FovY) * 0.5f), Epsilon);
		}
	}

	/// <summary>
	/// View matrix from raylib's camera implementation.
	/// </summary>
	public Matrix4x4 ViewMatrix => Raylib.GetCameraMatrix(ToRaylibCamera());

	/// <summary>
	/// Snapshot of the current node state as a raylib camera.
	/// </summary>
	public RlCamera3D ToRaylibCamera() {
		return new RlCamera3D(GlobalPosition, Target, UpDirection, FovY, Projection);
	}

	/// <summary>
	/// Orients the node so its forward vector looks at a world-space target.
	/// </summary>
	public void LookAt(Vector3 target, Vector3? up = null) {
		Target = target;
		Up = up ?? Vector3.UnitY;
	}

	/// <summary>
	/// Positions and orients the camera so a sphere fits inside the current field of view.
	/// </summary>
	public void FrameSphere(Vector3 center, float radius, float yaw = MathF.PI / 4f, float pitch = -0.42f, float fitMultiplier = 1.15f) {
		var clampedRadius = Math.Max(radius, 0.1f);
		var halfFov = DegreesToRadians(Math.Max(Projection == CameraProjection.Perspective ? FovY : 45f, 1f)) * 0.5f;
		var distance = Projection == CameraProjection.Perspective
			? (clampedRadius / MathF.Sin(Math.Max(halfFov, 0.1f))) * fitMultiplier
			: clampedRadius * 2f * fitMultiplier;
		var cosPitch = MathF.Cos(pitch);
		var offset = new Vector3(
			distance * MathF.Sin(yaw) * cosPitch,
			distance * MathF.Sin(pitch),
			distance * MathF.Cos(yaw) * cosPitch);
		Position = center + offset;
		Target = center;
		Up = Vector3.UnitY;
	}

	/// <summary>
	/// Begins a raylib 3D pass with the current camera state.
	/// </summary>
	public void BeginMode() {
		Raylib.BeginMode3D(ToRaylibCamera());
	}

	/// <summary>
	/// Ends the active raylib 3D pass.
	/// </summary>
	public void EndMode() {
		Raylib.EndMode3D();
	}

	/// <summary>
	/// Projects a world-space point into screen coordinates.
	/// </summary>
	public bool TryPointToScreen(Vector3 worldPoint, out float screenX, out float screenY, out float depth) {
		var camera = ToRaylibCamera();
		var viewport = ResolveViewport();
		var viewPoint = Vector3.Transform(worldPoint, ViewMatrix);
		depth = -viewPoint.Z;

		if (depth < ZNear || depth > ZFar) {
			screenX = 0f;
			screenY = 0f;
			return false;
		}

		var screenPoint = Raylib.GetWorldToScreenEx(
			worldPoint,
			camera,
			Math.Max(1, (int)MathF.Round(viewport.Width)),
			Math.Max(1, (int)MathF.Round(viewport.Height)));

		screenX = viewport.X + screenPoint.X;
		screenY = viewport.Y + screenPoint.Y;
		return true;
	}

	/// <summary>
	/// Returns the world-space point on the near plane for the supplied screen position.
	/// </summary>
	public Vector3 ScreenToPoint(float screenX, float screenY) {
		var camera = ToRaylibCamera();
		var viewport = ResolveViewport();
		var localScreen = new Vector2(screenX - viewport.X, screenY - viewport.Y);
		var ray = Raylib.GetScreenToWorldRayEx(
			localScreen,
			camera,
			Math.Max(1, (int)MathF.Round(viewport.Width)),
			Math.Max(1, (int)MathF.Round(viewport.Height)));

		if (Projection == CameraProjection.Orthographic) {
			return ray.Position;
		}

		return ray.Position + (Vector3.Normalize(ray.Direction) * ZNear);
	}

	/// <summary>
	/// Classifies a sphere against the current view frustum using explicit frustum planes.
	/// </summary>
	public CameraVisibilityState ObjectVisibility(Vector3 worldPosition, float radius) {
		var result = CameraVisibilityState.FullyVisible;
		foreach (var plane in BuildFrustumPlanes()) {
			var distance = plane.SignedDistanceTo(worldPosition);
			if (distance < -radius) {
				return CameraVisibilityState.NotVisible;
			}

			if (distance < radius) {
				result = CameraVisibilityState.PartiallyVisible;
			}
		}

		if (TryProjectSphere(worldPosition, radius, out _, out _, out var projectedRadius, out _) &&
		    MathF.PI * projectedRadius * projectedRadius < 1f) {
			return CameraVisibilityState.SubPixel;
		}

		return result;
	}

	/// <summary>
	/// Classifies an axis-aligned bounding box against the current view frustum using explicit frustum planes.
	/// </summary>
	public CameraVisibilityState ObjectVisibility(BoundingBox bounds) {
		var result = CameraVisibilityState.FullyVisible;
		foreach (var plane in BuildFrustumPlanes()) {
			var positiveVertex = SelectBoxVertex(bounds, plane.Normal, usePositiveVertex: true);
			if (plane.SignedDistanceTo(positiveVertex) < 0f) {
				return CameraVisibilityState.NotVisible;
			}

			var negativeVertex = SelectBoxVertex(bounds, plane.Normal, usePositiveVertex: false);
			if (plane.SignedDistanceTo(negativeVertex) < 0f) {
				result = CameraVisibilityState.PartiallyVisible;
			}
		}

		return result;
	}

	private bool TryProjectSphere(Vector3 worldPosition, float radius, out float centerX, out float centerY, out float projectedRadius, out float depth) {
		if (!TryPointToScreen(worldPosition, out centerX, out centerY, out depth)) {
			projectedRadius = 0f;
			return false;
		}

		var cameraToPoint = worldPosition - GlobalPosition;
		var tangentAxis = Vector3.Cross(cameraToPoint, UpDirection);
		if (tangentAxis.LengthSquared() <= Epsilon) {
			tangentAxis = RightDirection;
		}

		tangentAxis = Vector3.Normalize(tangentAxis) * Math.Max(radius, 0f);
		if (!TryPointToScreen(worldPosition + tangentAxis, out var edgeX, out var edgeY, out _)) {
			projectedRadius = 0f;
			return false;
		}

		projectedRadius = Vector2.Distance(new Vector2(centerX, centerY), new Vector2(edgeX, edgeY));
		return true;
	}

	private FrustumPlane[] BuildFrustumPlanes() {
		var forward = Forward;
		var right = RightDirection;
		var up = UpDirection;
		var position = GlobalPosition;
		var nearCenter = position + (forward * ZNear);
		var farCenter = position + (forward * ZFar);

		if (Projection == CameraProjection.Orthographic) {
			var halfHeight = Math.Max(FovY * 0.5f, Epsilon);
			var halfWidth = halfHeight * Aspect;
			return [
				new FrustumPlane( forward, nearCenter),
				new FrustumPlane(-forward, farCenter),
				new FrustumPlane( right, nearCenter - (right * halfWidth)),
				new FrustumPlane(-right, nearCenter + (right * halfWidth)),
				new FrustumPlane( up, nearCenter - (up * halfHeight)),
				new FrustumPlane(-up, nearCenter + (up * halfHeight))
			];
		}

		var halfNearHeight = ZNear * MathF.Tan(DegreesToRadians(FovY) * 0.5f);
		var halfNearWidth = halfNearHeight * Aspect;
		var leftEdge = (forward * ZNear) - (right * halfNearWidth);
		var rightEdge = (forward * ZNear) + (right * halfNearWidth);
		var topEdge = (forward * ZNear) + (up * halfNearHeight);
		var bottomEdge = (forward * ZNear) - (up * halfNearHeight);

		return [
			new FrustumPlane( forward, nearCenter),
			new FrustumPlane(-forward, farCenter),
			new FrustumPlane(Vector3.Normalize(Vector3.Cross(leftEdge, up)), position),
			new FrustumPlane(Vector3.Normalize(Vector3.Cross(up, rightEdge)), position),
			new FrustumPlane(Vector3.Normalize(Vector3.Cross(topEdge, right)), position),
			new FrustumPlane(Vector3.Normalize(Vector3.Cross(right, bottomEdge)), position)
		];
	}

	private static Vector3 SelectBoxVertex(BoundingBox bounds, Vector3 normal, bool usePositiveVertex) {
		return new Vector3(
			SelectComponent(bounds.Min.X, bounds.Max.X, normal.X, usePositiveVertex),
			SelectComponent(bounds.Min.Y, bounds.Max.Y, normal.Y, usePositiveVertex),
			SelectComponent(bounds.Min.Z, bounds.Max.Z, normal.Z, usePositiveVertex));
	}

	private static float SelectComponent(float min, float max, float normalComponent, bool usePositiveVertex) {
		if (usePositiveVertex) {
			return normalComponent >= 0f ? max : min;
		}

		return normalComponent >= 0f ? min : max;
	}

	private Vector3 TransformDirection(Vector3 localDirection) {
		var direction = Vector3.TransformNormal(localDirection, GlobalTransform);
		return NormalizeOrFallback(direction, localDirection);
	}

	private CameraViewport ResolveViewport() {
		if (ViewportOverride is { } viewport && viewport.Width > Epsilon && viewport.Height > Epsilon) {
			return viewport;
		}

		var width = Math.Max(Raylib.GetScreenWidth(), (int)DefaultWidth);
		var height = Math.Max(Raylib.GetScreenHeight(), (int)DefaultHeight);
		return new CameraViewport(0f, 0f, width, height);
	}

	private static Vector3 NormalizeOrFallback(Vector3 value, Vector3 fallback, bool allowZeroFallback = true) {
		if (value.LengthSquared() > Epsilon) {
			return Vector3.Normalize(value);
		}

		if (!allowZeroFallback && fallback.LengthSquared() > Epsilon) {
			return Vector3.Normalize(fallback);
		}

		return fallback;
	}

	private static float DegreesToRadians(float value) => value * (MathF.PI / 180f);

	private static float RadiansToDegrees(float value) => value * (180f / MathF.PI);

	private readonly record struct FrustumPlane(Vector3 Normal, Vector3 Point) {
		public float SignedDistanceTo(Vector3 value) => Vector3.Dot(Normal, value - Point);
	}
}
