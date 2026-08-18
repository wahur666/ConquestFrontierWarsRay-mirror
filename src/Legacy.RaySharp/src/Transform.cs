using System.Numerics;

namespace RaySharp;

internal sealed class Transform
{
    public Transform()
        : this(Vector3.Zero, Quaternion.Identity, Vector3.One)
    {
    }

    public Transform(Vector3 position)
        : this(position, Quaternion.Identity, Vector3.One)
    {
    }

    public Transform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        this.position = position;
        this.rotation = NormalizeOrIdentity(rotation);
        this.scale = ClampScale(scale);
        RebuildMatrices();
    }

    private Vector3 position;
    private Quaternion rotation;
    private Vector3 scale;

    public Vector3 Position
    {
        get => position;
        set
        {
            position = value;
            RebuildMatrices();
        }
    }

    public Vector3 Origin
    {
        get => Position;
        set => Position = value;
    }

    public Quaternion Rotation
    {
        get => rotation;
        set
        {
            rotation = NormalizeOrIdentity(value);
            RebuildMatrices();
        }
    }

    public Vector3 Scale
    {
        get => scale;
        set
        {
            scale = ClampScale(value);
            RebuildMatrices();
        }
    }

    public Matrix4x4 WorldMatrix { get; private set; }
    public Matrix4x4 NormalMatrix { get; private set; }

    public void Set(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        this.position = position;
        this.rotation = NormalizeOrIdentity(rotation);
        this.scale = ClampScale(scale);
        RebuildMatrices();
    }

    public void Translate(Vector3 delta)
    {
        Position += delta;
    }

    public void Rotate(Quaternion delta)
    {
        Rotation = delta * Rotation;
    }

    public void ScaleBy(float factor)
    {
        Scale *= Math.Max(0.01f, factor);
    }

    public Vector3 TransformPoint(Vector3 point)
    {
        return Vector3.Transform(point, WorldMatrix);
    }

    public Vector3 TransformDirection(Vector3 direction)
    {
        Vector3 transformed = Vector3.TransformNormal(direction, NormalMatrix);
        return transformed == Vector3.Zero ? direction : Vector3.Normalize(transformed);
    }

    private void RebuildMatrices()
    {
        WorldMatrix = Matrix4x4.CreateScale(scale)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(position);
        NormalMatrix = Matrix4x4.Invert(WorldMatrix, out Matrix4x4 inverseWorld)
            ? Matrix4x4.Transpose(inverseWorld)
            : Matrix4x4.Identity;
    }

    private static Vector3 ClampScale(Vector3 value)
    {
        return Vector3.Max(new Vector3(0.01f), value);
    }

    private static Quaternion NormalizeOrIdentity(Quaternion value)
    {
        return value.LengthSquared() > 0.000001f
            ? Quaternion.Normalize(value)
            : Quaternion.Identity;
    }
}
