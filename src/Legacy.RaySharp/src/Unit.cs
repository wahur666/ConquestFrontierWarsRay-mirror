using System.Numerics;
using RaySharp.Mesh;
using RaySharp.MeshViewer;
using Raylib_cs;
using static RaySharp.AppTheme;
using static RaySharp.GameRules;
using static RaySharp.LitRenderer;

namespace RaySharp;

internal sealed class Unit : RenderableObject
{
    private const string UnitModelFileName = "tfabricator.cmp.xml";
    private Vector3 destination;
    private readonly MeshViewerModel? model;
    private readonly float groundOffset;
    private readonly float baseSelectionInnerRadius = 1.95f;
    private readonly float baseSelectionOuterRadius = 2.35f;

    public Unit(Vector3 position)
        : base(new Transform(KeepOnGround(position)))
    {
        destination = Position;
        SetYaw(0.0f);
        model = LoadModel();
        if (model is not null)
        {
            groundOffset = Math.Max(0.0f, -model.Geometry.Bounds.Min.Y);
            Vector3 size = model.Geometry.Bounds.Max - model.Geometry.Bounds.Min;
            float radius = MathF.Sqrt(size.X * size.X + size.Z * size.Z) * 0.5f;
            baseSelectionInnerRadius = Math.Max(0.6f, radius * 0.82f);
            baseSelectionOuterRadius = Math.Max(baseSelectionInnerRadius + 0.08f, radius * 0.98f);
            ApplyModelTransform();
        }
    }

    public Vector3 Position => Transform.Position;
    public Vector3 SelectionPoint => model is null
        ? Position + new Vector3(0.0f, UnitTopHeight * 0.5f, 0.0f)
        : new Vector3(
            (model.Bounds.Min.X + model.Bounds.Max.X) * 0.5f,
            (model.Bounds.Min.Y + model.Bounds.Max.Y) * 0.5f,
            (model.Bounds.Min.Z + model.Bounds.Max.Z) * 0.5f);
    public float Yaw { get; private set; }
    public bool HasDestination { get; private set; }
    public bool IsSelected { get; set; } = true;
    public string ModelStatus => model is null ? $"{UnitModelFileName} not loaded; using placeholder arrow." : $"Loaded {model.Geometry.Name}.";

    public void SetScale(float uniformScale)
    {
        SetScale(new Vector3(uniformScale));
    }

    public void SetScale(Vector3 newScale)
    {
        Transform.Scale = newScale;
        ApplyModelTransform();
    }

    public void SetDestination(Vector3 newDestination)
    {
        destination = KeepOnGround(newDestination);
        HasDestination = Vector3.DistanceSquared(Position, destination) > StopDistance * StopDistance;
    }

    public override void Update(RenderUpdateContext context)
    {
        if (!HasDestination)
        {
            return;
        }

        float deltaTime = context.DeltaTime;
        Vector3 toDestination = destination - Position;
        toDestination.Y = 0.0f;
        float distance = toDestination.Length();

        if (distance <= StopDistance)
        {
            Transform.Position = destination;
            HasDestination = false;
            context.Ui.Status = "Arrived. Right-click the plane to move again.";
            return;
        }

        float desiredYaw = MathF.Atan2(-toDestination.X, -toDestination.Z);
        SetYaw(DampAngle(Yaw, desiredYaw, UnitTurnSpeed, deltaTime));

        Vector3 forward = new(-MathF.Sin(Yaw), 0.0f, -MathF.Cos(Yaw));
        Transform.Position = KeepOnGround(Position + forward * Math.Min(distance, UnitSpeed * deltaTime));
        ApplyModelTransform();
    }

    public override void Render(RenderContext context)
    {
        if (HasDestination)
        {
            DrawTargetMarker(destination);
        }

        if (IsSelected)
        {
            Vector3 scale = Transform.Scale;
            float ringScale = MathF.Max(scale.X, MathF.Max(scale.Y, scale.Z));
            DrawSelectionRing(Position, baseSelectionInnerRadius * ringScale, baseSelectionOuterRadius * ringScale, UnitOutlineColor);
        }

        if (model is not null)
        {
            context.ModelRenderer.Draw(model);
        }
        else
        {
            DrawExtrudedArrow();
        }
    }

    public override void Dispose()
    {
        model?.Dispose();
    }

    private static MeshViewerModel? LoadModel()
    {
        string? path = MeshSampleResolver.Resolve(UnitModelFileName);
        return path is null
            ? null
            : MeshViewerModel.Load(path, MeshUvMode.Raw);
    }

    private void ApplyModelTransform()
    {
        if (model is null)
        {
            return;
        }

        model.SetPosition(Position + new Vector3(0.0f, groundOffset, 0.0f));
        model.SetRotation(Transform.Rotation);
        model.SetScale(Transform.Scale);
    }

    private void DrawExtrudedArrow()
    {
        Vector3[] bottom = CreateArrowPoints(UnitBaseHeight);
        Vector3[] top = CreateArrowPoints(UnitTopHeight);

        for (int i = 0; i < bottom.Length; i++)
        {
            int next = (i + 1) % bottom.Length;
            DrawQuad(bottom[i], bottom[next], top[next], top[i], UnitSideColor);
        }

        DrawArrowFace(top, UnitTopColor, false);
        DrawArrowFace(bottom, UnitBottomColor, true);
        DrawOutline(top, UnitOutlineColor);
        DrawOutline(bottom, new Color(72, 158, 217, 255));

        for (int i = 0; i < top.Length; i++)
        {
            Raylib.DrawLine3D(bottom[i], top[i], UnitOutlineColor);
        }
    }

    private Vector3[] CreateArrowPoints(float height)
    {
        return
        [
            TransformLocalPoint(new Vector3(0.0f, height, 1.85f)),
                TransformLocalPoint(new Vector3(1.10f, height, -0.95f)),
                TransformLocalPoint(new Vector3(0.36f, height, -0.95f)),
                TransformLocalPoint(new Vector3(0.0f, height, -0.48f)),
                TransformLocalPoint(new Vector3(-0.36f, height, -0.95f)),
                TransformLocalPoint(new Vector3(-1.10f, height, -0.95f))
        ];
    }

    private Vector3 TransformLocalPoint(Vector3 localPoint)
    {
        return Transform.TransformPoint(localPoint);
    }

    private void SetYaw(float yaw)
    {
        Yaw = yaw;
        Transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, Yaw);
    }

    private static void DrawArrowFace(Vector3[] points, Color color, bool reverse)
    {
        if (!reverse)
        {
            DrawTriangle(points[0], points[1], points[2], color);
            DrawTriangle(points[0], points[2], points[3], color);
            DrawTriangle(points[0], points[3], points[4], color);
            DrawTriangle(points[0], points[4], points[5], color);
            return;
        }

        DrawTriangle(points[2], points[1], points[0], color);
        DrawTriangle(points[3], points[2], points[0], color);
        DrawTriangle(points[4], points[3], points[0], color);
        DrawTriangle(points[5], points[4], points[0], color);
    }

    private static void DrawTargetMarker(Vector3 position)
    {
        DrawSelectionRing(position, 0.42f, 0.52f, TargetColor);
        Raylib.DrawCube(WithHeight(position, 0.055f), 1.25f, 0.035f, 0.035f, new Color(255, 209, 102, 230));
        Raylib.DrawCube(WithHeight(position, 0.055f), 0.035f, 0.035f, 1.25f, new Color(255, 209, 102, 230));
    }

    private static void DrawSelectionRing(Vector3 center, float innerRadius, float outerRadius, Color color)
    {
        DrawRingShape(center, innerRadius, outerRadius, new Color((int)color.R, (int)color.G, (int)color.B, 185));
    }

    private static void DrawRingShape(Vector3 center, float innerRadius, float outerRadius, Color color)
    {
        const int segments = 96;

        for (int i = 0; i < segments; i++)
        {
            float currentAngle = MathF.Tau * i / segments;
            float nextAngle = MathF.Tau * (i + 1) / segments;

            Vector3 outerCurrent = RingPoint(center, outerRadius, currentAngle);
            Vector3 outerNext = RingPoint(center, outerRadius, nextAngle);
            Vector3 innerCurrent = RingPoint(center, innerRadius, currentAngle);
            Vector3 innerNext = RingPoint(center, innerRadius, nextAngle);

            Raylib.DrawTriangle3D(outerCurrent, innerNext, outerNext, color);
            Raylib.DrawTriangle3D(outerCurrent, innerCurrent, innerNext, color);
        }
    }

    private static Vector3 RingPoint(Vector3 center, float radius, float angle)
    {
        return WithHeight(center + new Vector3(MathF.Cos(angle) * radius, 0.0f, MathF.Sin(angle) * radius), RingHeight);
    }

    private static void DrawOutline(Vector3[] points, Color color)
    {
        for (int i = 0; i < points.Length; i++)
        {
            Raylib.DrawLine3D(points[i], points[(i + 1) % points.Length], color);
        }
    }

    private static float DampAngle(float current, float target, float lambda, float deltaTime)
    {
        float delta = NormalizeAngle(target - current);
        float t = 1.0f - MathF.Exp(-lambda * deltaTime);
        return current + delta * t;
    }

    private static float NormalizeAngle(float angle)
    {
        float result = angle;
        while (result > MathF.PI)
        {
            result -= MathF.Tau;
        }

        while (result < -MathF.PI)
        {
            result += MathF.Tau;
        }

        return result;
    }

    private static Vector3 KeepOnGround(Vector3 position)
    {
        position.Y = 0.0f;
        return position;
    }

    private static Vector3 WithHeight(Vector3 position, float height)
    {
        position.Y = height;
        return position;
    }
}
