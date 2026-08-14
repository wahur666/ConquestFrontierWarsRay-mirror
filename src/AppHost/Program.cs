using System;
using System.Numerics;
using ConquestFrontierWarsRay.Runtime.Collision;
using Math3D;
using Raylib_cs;

namespace ConquestFrontierWarsRay;

internal static class Program {
	private static readonly CollisionService Collision = new();
	private static readonly BallState[] Balls = [
		new BallState("A", new Vector2(180f, 220f), new Vector2(180f, 0f), 42f, Color.SkyBlue),
		new BallState("B", new Vector2(620f, 220f), new Vector2(-150f, 0f), 58f, Color.Violet),
		new BallState("C", new Vector2(410f, 110f), new Vector2(0f, 135f), 28f, Color.Lime)
	];

	private static CollisionSnapshot _lastCollision = CollisionSnapshot.None;

	private static void Main() {
		Raylib.SetConfigFlags(ConfigFlags.VSyncHint | ConfigFlags.Msaa4xHint);
		Raylib.InitWindow(960, 540, "ConquestFrontierWarsRay Collision Demo");
		Raylib.SetTargetFPS(60);

		using var win32 = new Win32Window(onTick: Tick);
		while (!Raylib.WindowShouldClose()) {
			Tick();
		}

		Raylib.CloseWindow();
	}

	private static void Tick() {
		var dt = MathF.Max(Raylib.GetFrameTime(), 1f / 240f);
		UpdateBalls(dt);
		DrawScene();
	}

	private static void UpdateBalls(float dt) {
		var bounds = GetBounds();
		foreach (var ball in Balls) {
			ball.Position += ball.Velocity * dt;
			ConstrainToBounds(ball, bounds);
		}

		_lastCollision = CollisionSnapshot.None;
		for (var leftIndex = 0; leftIndex < Balls.Length; leftIndex++) {
			for (var rightIndex = leftIndex + 1; rightIndex < Balls.Length; rightIndex++) {
				if (!TryResolveCollision(Balls[leftIndex], Balls[rightIndex], out var snapshot)) {
					continue;
				}

				_lastCollision = snapshot;
			}
		}
	}

	private static bool TryResolveCollision(BallState left, BallState right, out CollisionSnapshot snapshot) {
		var leftExtent = new SphereExtent(left.Label, Transform3.Identity, new CollisionSphere(left.Radius));
		var rightExtent = new SphereExtent(right.Label, Transform3.Identity, new CollisionSphere(right.Radius));
		var leftTransform = new Transform3(Matrix3.Identity, new Vector3(left.Position, 0f));
		var rightTransform = new Transform3(Matrix3.Identity, new Vector3(right.Position, 0f));
		var data = new CollisionData();
		if (!Collision.CollideExtents(data, leftExtent, leftTransform, rightExtent, rightTransform)) {
			snapshot = CollisionSnapshot.None;
			return false;
		}

		var delta = left.Position - right.Position;
		var distance = delta.Length();
		var minDistance = left.Radius + right.Radius;
		var normal2 = distance > 1e-5f
			? Vector2.Normalize(delta)
			: new Vector2(data.Normal.X, data.Normal.Y) is var fallback && fallback.LengthSquared() > 0f
				? Vector2.Normalize(fallback)
				: Vector2.UnitX;
		var penetration = MathF.Max(0f, minDistance - distance);
		if (penetration > 0f) {
			var correction = normal2 * (penetration * 0.5f);
			left.Position += correction;
			right.Position -= correction;
		}

		var relativeVelocity = left.Velocity - right.Velocity;
		var closingSpeed = Vector2.Dot(relativeVelocity, normal2);
		if (closingSpeed < 0f) {
			left.Velocity -= normal2 * closingSpeed;
			right.Velocity += normal2 * closingSpeed;
		}

		snapshot = new CollisionSnapshot(
			true,
			new Vector2(data.Contact.X, data.Contact.Y),
			new Vector2(data.Normal.X, data.Normal.Y),
			$"{left.Label}-{right.Label}",
			penetration);
		return true;
	}

	private static Rectangle GetBounds() {
		var width = Raylib.GetScreenWidth();
		var height = Raylib.GetScreenHeight();
		return new Rectangle(32f, 96f, width - 64f, height - 128f);
	}

	private static void ConstrainToBounds(BallState ball, Rectangle bounds) {
		if (ball.Position.X - ball.Radius < bounds.X) {
			ball.Position = new Vector2(bounds.X + ball.Radius, ball.Position.Y);
			ball.Velocity = new Vector2(MathF.Abs(ball.Velocity.X), ball.Velocity.Y);
		} else if (ball.Position.X + ball.Radius > bounds.X + bounds.Width) {
			ball.Position = new Vector2(bounds.X + bounds.Width - ball.Radius, ball.Position.Y);
			ball.Velocity = new Vector2(-MathF.Abs(ball.Velocity.X), ball.Velocity.Y);
		}

		if (ball.Position.Y - ball.Radius < bounds.Y) {
			ball.Position = new Vector2(ball.Position.X, bounds.Y + ball.Radius);
			ball.Velocity = new Vector2(ball.Velocity.X, MathF.Abs(ball.Velocity.Y));
		} else if (ball.Position.Y + ball.Radius > bounds.Y + bounds.Height) {
			ball.Position = new Vector2(ball.Position.X, bounds.Y + bounds.Height - ball.Radius);
			ball.Velocity = new Vector2(ball.Velocity.X, -MathF.Abs(ball.Velocity.Y));
		}
	}

	private static void DrawScene() {
		var bounds = GetBounds();
		var colliding = _lastCollision.Hit;

		Raylib.BeginDrawing();
		Raylib.ClearBackground(new Color(10, 14, 24, 255));
		Raylib.DrawRectangleRounded(bounds, 0.025f, 8, new Color(20, 27, 42, 255));
		Raylib.DrawRectangleLinesEx(bounds, 2f, new Color(76, 91, 123, 255));
		Raylib.DrawText("Collision runtime demo", 32, 24, 28, Color.RayWhite);
		Raylib.DrawText("Three sphere extents update every frame through CollisionService.CollideExtents(...).", 32, 58, 18, new Color(180, 192, 214, 255));

		foreach (var ball in Balls) {
			var tint = colliding && _lastCollision.Pair.Contains(ball.Label, StringComparison.Ordinal)
				? new Color(255, 180, 68, 255)
				: ball.Color;
			Raylib.DrawCircleV(ball.Position, ball.Radius, tint);
			Raylib.DrawCircleLines((int)ball.Position.X, (int)ball.Position.Y, ball.Radius, Color.Black);
			Raylib.DrawText(ball.Label, (int)(ball.Position.X - 6f), (int)(ball.Position.Y - 10f), 20, Color.Black);
		}

		if (colliding) {
			var contact = _lastCollision.Contact;
			var normalEnd = contact + (_lastCollision.Normal * 64f);
			Raylib.DrawCircleV(contact, 7f, Color.Red);
			Raylib.DrawLineEx(contact, normalEnd, 4f, Color.Gold);
			Raylib.DrawText($"Detected pair: {_lastCollision.Pair}", 32, Raylib.GetScreenHeight() - 92, 20, Color.Orange);
			Raylib.DrawText(
				$"Expected: overlapping balls separate, swap closing velocity along the normal, and mark contact at ({contact.X:0.0}, {contact.Y:0.0}).",
				32,
				Raylib.GetScreenHeight() - 64,
				18,
				new Color(235, 221, 173, 255));
			Raylib.DrawText(
				$"Normal: ({_lastCollision.Normal.X:0.00}, {_lastCollision.Normal.Y:0.00})  penetration: {_lastCollision.Penetration:0.00}",
				32,
				Raylib.GetScreenHeight() - 38,
				18,
				new Color(235, 221, 173, 255));
		} else {
			Raylib.DrawText("Expected: while balls are separated, no contact point is shown and all spheres keep their base colors.", 32, Raylib.GetScreenHeight() - 52, 18, new Color(180, 192, 214, 255));
		}

		Raylib.DrawFPS(Raylib.GetScreenWidth() - 100, 12);
		Raylib.EndDrawing();
	}

	private sealed class BallState(string label, Vector2 position, Vector2 velocity, float radius, Color color) {
		public string Label { get; } = label;
		public Vector2 Position { get; set; } = position;
		public Vector2 Velocity { get; set; } = velocity;
		public float Radius { get; } = radius;
		public Color Color { get; } = color;
	}

	private readonly record struct CollisionSnapshot(bool Hit, Vector2 Contact, Vector2 Normal, string Pair, float Penetration) {
		public static CollisionSnapshot None => new(false, Vector2.Zero, Vector2.Zero, string.Empty, 0f);
	}
}
