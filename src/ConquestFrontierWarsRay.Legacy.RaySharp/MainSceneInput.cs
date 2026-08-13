using System.Numerics;
using Raylib_cs;

namespace RaySharp;

internal static class MainSceneInput {
	internal static void Handle(Camera3D camera, IReadOnlyList<Unit> units, UiState ui) {
		if (Raylib.IsKeyPressed(KeyboardKey.Escape) && ui.MenuOpen) {
			ui.MenuOpen = false;
			return;
		}

		Vector2 mouse = Raylib.GetMousePosition();
		if (Raylib.CheckCollisionPointRec(mouse, MainSceneHud.MediaControlsPanel)) {
			return;
		}

		if (ui.MenuOpen) {
			if (Raylib.IsMouseButtonPressed(MouseButton.Left) &&
			    Raylib.CheckCollisionPointRec(mouse, UiLayout.CloseButton)) {
				ui.MenuOpen = false;
			}

			return;
		}

		if (Raylib.IsMouseButtonPressed(MouseButton.Left) &&
		    Raylib.CheckCollisionPointRec(mouse, UiLayout.MenuButton)) {
			ui.MenuOpen = true;
			return;
		}

		if (Raylib.IsMouseButtonPressed(MouseButton.Left) && mouse.Y >= AppTheme.HeaderHeight) {
			ui.SelectionStart = mouse;
			ui.SelectionCurrent = mouse;
			ui.IsSelecting = true;
		}

		if (ui.IsSelecting && Raylib.IsMouseButtonDown(MouseButton.Left)) {
			ui.SelectionCurrent = mouse;
		}

		if (ui.IsSelecting && Raylib.IsMouseButtonReleased(MouseButton.Left)) {
			Rectangle selection = MainSceneHud.NormalizeRectangle(ui.SelectionStart, ui.SelectionCurrent);
			bool marquee = selection.Width >= 4.0f && selection.Height >= 4.0f;
			int selectedCount = 0;
			foreach (Unit unit in units) {
				Vector2 unitScreenPosition = Raylib.GetWorldToScreen(unit.SelectionPoint, camera);
				unit.IsSelected = marquee
					? Raylib.CheckCollisionPointRec(unitScreenPosition, selection)
					: Vector2.DistanceSquared(unitScreenPosition, mouse) <= 18.0f * 18.0f;
				if (unit.IsSelected) {
					selectedCount++;
				}
			}

			ui.IsSelecting = false;
			ui.Status = selectedCount > 0
				? $"{selectedCount} unit{(selectedCount == 1 ? string.Empty : "s")} selected. Right-click the plane to move."
				: "No units selected. Left-drag around units to select them.";
			return;
		}

		if (!Raylib.IsMouseButtonPressed(MouseButton.Right)) {
			return;
		}

		if (mouse.Y < AppTheme.HeaderHeight) {
			return;
		}

		List<Unit> selectedUnits = units.Where(unit => unit.IsSelected).ToList();
		if (selectedUnits.Count == 0) {
			ui.Status = "No units selected. Left-drag around units before issuing move commands.";
			return;
		}

		if (TryGetGroundPoint(camera, mouse, out Vector3 destination)) {
			for (int index = 0; index < selectedUnits.Count; index++) {
				selectedUnits[index].SetDestination(destination + FormationOffset(index, selectedUnits.Count));
			}

			ui.Status = $"Move {selectedUnits.Count} unit{(selectedUnits.Count == 1 ? string.Empty : "s")} to x {destination.X:0.0}, z {destination.Z:0.0}";
		}
	}

	private static Vector3 FormationOffset(int index, int count) {
		if (count <= 1) {
			return Vector3.Zero;
		}

		const float spacing = 3.0f;
		int columns = (int)MathF.Ceiling(MathF.Sqrt(count));
		int rows = (int)MathF.Ceiling(count / (float)columns);
		int row = index / columns;
		int column = index % columns;
		return new Vector3(
			(column - (columns - 1) * 0.5f) * spacing,
			0.0f,
			(row - (rows - 1) * 0.5f) * spacing);
	}

	private static bool TryGetGroundPoint(Camera3D camera, Vector2 mousePosition, out Vector3 point) {
		Ray ray = Raylib.GetScreenToWorldRay(mousePosition, camera);

		if (MathF.Abs(ray.Direction.Y) < 0.0001f) {
			point = default;
			return false;
		}

		float distanceAlongRay = -ray.Position.Y / ray.Direction.Y;
		if (distanceAlongRay < 0.0f) {
			point = default;
			return false;
		}

		point = ray.Position + ray.Direction * distanceAlongRay;
		point.Y = 0.0f;
		return true;
	}
}