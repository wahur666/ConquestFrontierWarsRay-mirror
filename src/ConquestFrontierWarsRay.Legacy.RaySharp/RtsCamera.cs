using System.Numerics;
using Raylib_cs;
using static RaySharp.GameRules;

namespace RaySharp;

internal sealed class RtsCamera
{
    private const float MinDistance = 16.0f;
    private const float MaxDistance = 70.0f;
    private const float RotateSpeed = 1.4f;

    public Vector3 Target { get; set; }
    public float Distance { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }

    public void Update(float deltaTime, bool menuOpen)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.R))
        {
            Target = Vector3.Zero;
            Distance = 37.0f;
            Yaw = 0.0f;
            Pitch = -0.86f;
        }

        if (Raylib.IsKeyDown(KeyboardKey.Q))
        {
            Yaw -= RotateSpeed * deltaTime;
        }

        if (Raylib.IsKeyDown(KeyboardKey.E))
        {
            Yaw += RotateSpeed * deltaTime;
        }

        float wheelMove = Raylib.GetMouseWheelMove();
        if (wheelMove != 0.0f)
        {
            Distance = Math.Clamp(Distance - wheelMove * 2.4f, MinDistance, MaxDistance);
        }

        if (!menuOpen)
        {
            PanFromMouseEdges(deltaTime);
        }
    }

    public Camera3D ToCamera()
    {
        return new Camera3D
        {
            Position = GetPosition(),
            Target = Target,
            Up = Vector3.UnitY,
            FovY = 48.0f,
            Projection = CameraProjection.Perspective
        };
    }

    private Vector3 GetPosition()
    {
        float horizontalDistance = MathF.Cos(Pitch) * Distance;

        return Target + new Vector3(
            MathF.Sin(Yaw) * horizontalDistance,
            -MathF.Sin(Pitch) * Distance,
            MathF.Cos(Yaw) * horizontalDistance);
    }

    private void PanFromMouseEdges(float deltaTime)
    {
        Vector2 mouse = Raylib.GetMousePosition();
        int screenWidth = Raylib.GetScreenWidth();
        int screenHeight = Raylib.GetScreenHeight();
        Vector2 direction = Vector2.Zero;

        if (mouse.X <= EdgePanMargin)
        {
            direction.X -= 1.0f;
        }
        else if (mouse.X >= screenWidth - EdgePanMargin)
        {
            direction.X += 1.0f;
        }

        if (mouse.Y <= EdgePanMargin)
        {
            direction.Y += 1.0f;
        }
        else if (mouse.Y >= screenHeight - EdgePanMargin)
        {
            direction.Y -= 1.0f;
        }

        if (direction == Vector2.Zero)
        {
            return;
        }

        direction = Vector2.Normalize(direction);
        Vector3 forward = new(-MathF.Sin(Yaw), 0.0f, -MathF.Cos(Yaw));
        Vector3 right = new(MathF.Cos(Yaw), 0.0f, -MathF.Sin(Yaw));
        Target += (right * direction.X + forward * direction.Y) * EdgePanSpeed * deltaTime;
    }
}
