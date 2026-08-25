using System.Text.Json;
using ConquestFrontierWarsRay.Data;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Core.UI;

public static class LegacyUiHitZoneSnapshot {
	private static readonly JsonSerializerOptions JsonOptions = new() {
		WriteIndented = true
	};

	public static string WriteToArtifacts(Node root, string snapshotName, string? fileSuffix = null) {
		ArgumentNullException.ThrowIfNull(root);
		if (string.IsNullOrWhiteSpace(snapshotName)) {
			throw new ArgumentException("Snapshot name is required.", nameof(snapshotName));
		}

		var repoRoot = RepoPaths.LocateRepoRoot();
		var directory = Path.Combine(repoRoot, "artifacts", "ui-hit-zones");
		Directory.CreateDirectory(directory);

		var safeName = SanitizeFilePart(snapshotName);
		var safeSuffix = string.IsNullOrWhiteSpace(fileSuffix) ? string.Empty : $"-{SanitizeFilePart(fileSuffix)}";
		var path = Path.Combine(directory, $"{safeName}{safeSuffix}-hit-zones.json");
		var snapshot = CreateSnapshot(root, snapshotName);
		File.WriteAllText(path, JsonSerializer.Serialize(snapshot, JsonOptions));
		return path;
	}

	public static HitZoneSnapshot CreateSnapshot(Node root, string snapshotName) {
		ArgumentNullException.ThrowIfNull(root);

		var contexts = new List<HitZoneContext> {
			CaptureContext("active", root)
		};

		foreach (var tabControl in DescendantsAndSelf(root).OfType<LegacyTabControlNode>()) {
			var originalTab = tabControl.SelectedTabIndex;
			try {
				for (var tabIndex = 0; tabIndex < tabControl.Tabs.Count; tabIndex++) {
					tabControl.SetCurrentTabSilently(tabIndex);
					contexts.Add(CaptureContext($"tab-{tabIndex}", root));

					foreach (var dropdown in DescendantsAndSelf(tabControl.GetTab(tabIndex)).OfType<LegacyDropdownNode>()) {
						var wasExpanded = dropdown.IsExpanded;
						try {
							dropdown.SetExpandedState(true);
							contexts.Add(CaptureContext($"tab-{tabIndex}-dropdown-{dropdown.Name}-expanded", root));
						} finally {
							dropdown.SetExpandedState(wasExpanded);
						}
					}
				}
			} finally {
				tabControl.SetCurrentTabSilently(originalTab);
			}
		}

		return new HitZoneSnapshot(
			snapshotName,
			DateTimeOffset.Now,
			root.Name,
			contexts);
	}

	private static HitZoneContext CaptureContext(string name, Node root) {
		var zones = DescendantsAndSelf(root)
			.Select(CreateZone)
			.Where(static zone => zone is not null)
			.Select(static zone => zone!)
			.ToArray();

		return new HitZoneContext(name, zones);
	}

	private static HitZone? CreateZone(Node node) {
		if (node is not Control control) {
			return null;
		}

		var kind = GetInteractableKind(node);
		if (kind is null) {
			return null;
		}

		var bounds = GetInteractableBounds(node, control);
		if (bounds.Width <= 0f || bounds.Height <= 0f) {
			return null;
		}

		return new HitZone(
			node.Name,
			GetNodePath(node),
			kind,
			IsEffectivelyInteractable(node),
			node is CanvasItem canvasItem ? canvasItem.GlobalZIndex : 0,
			new HitRect(bounds.X, bounds.Y, bounds.Width, bounds.Height));
	}

	private static string? GetInteractableKind(Node node) {
		return node switch {
			LegacyButtonNode => nameof(LegacyButtonNode),
			LegacyTabButtonNode => nameof(LegacyTabButtonNode),
			LegacyDropdownNode dropdown => dropdown.IsExpanded
				? $"{nameof(LegacyDropdownNode)}:expanded-union"
				: $"{nameof(LegacyDropdownNode)}:collapsed",
			LegacyListBoxNode => nameof(LegacyListBoxNode),
			LegacySliderNode => nameof(LegacySliderNode),
			LegacyScrollBarNode => nameof(LegacyScrollBarNode),
			_ => null
		};
	}

	private static Rectangle GetInteractableBounds(Node node, Control control) {
		if (node is LegacyDropdownNode { IsExpanded: true } dropdown) {
			return DescendantsAndSelf(dropdown)
				.OfType<Control>()
				.Where(static child => child is LegacyButtonNode or LegacyListBoxNode)
				.Select(static child => GetControlHitBounds(child))
				.Aggregate(control.GlobalBounds, Union);
		}

		return GetControlHitBounds(control);
	}

	private static Rectangle GetControlHitBounds(Control control) {
		return control switch {
			LegacyButtonNode button => button.GlobalHitBounds,
			LegacyListBoxNode listBox => listBox.GlobalHitBounds,
			LegacyScrollBarNode scrollBar => scrollBar.GlobalHitBounds,
			_ => control.GlobalBounds
		};
	}

	private static bool IsVisibleInTree(Node node) {
		for (var current = node; current is not null; current = current.Parent!) {
			if (current is CanvasItem { Visible: false }) {
				return false;
			}
		}

		return true;
	}

	private static bool IsEffectivelyInteractable(Node node) {
		if (!IsVisibleInTree(node)) {
			return false;
		}

		return node switch {
			LegacyButtonNode button => button.IsPointerInputEnabled,
			LegacyTabButtonNode tab => tab.IsPointerInputEnabled,
			LegacyDropdownNode dropdown => dropdown.IsPointerInputEnabled,
			LegacyListBoxNode listBox => listBox.IsPointerInputEnabled,
			LegacySliderNode slider => slider.IsPointerInputEnabled,
			LegacyScrollBarNode scrollBar => scrollBar.IsPointerInputEnabled,
			_ => false
		};
	}

	private static string GetNodePath(Node node) {
		var names = new Stack<string>();
		for (var current = node; current is not null; current = current.Parent) {
			names.Push(current.Name);
		}

		return string.Join("/", names);
	}

	private static IEnumerable<Node> DescendantsAndSelf(Node root) {
		yield return root;
		foreach (var child in root.Children) {
			foreach (var descendant in DescendantsAndSelf(child)) {
				yield return descendant;
			}
		}
	}

	private static Rectangle Union(Rectangle a, Rectangle b) {
		var left = MathF.Min(a.X, b.X);
		var top = MathF.Min(a.Y, b.Y);
		var right = MathF.Max(a.X + a.Width, b.X + b.Width);
		var bottom = MathF.Max(a.Y + a.Height, b.Y + b.Height);
		return new Rectangle(left, top, right - left, bottom - top);
	}

	private static string SanitizeFilePart(string value) {
		return string.Concat(value.Select(static c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '-'));
	}

	public sealed record HitZoneSnapshot(
		string Name,
		DateTimeOffset CapturedAt,
		string Root,
		IReadOnlyList<HitZoneContext> Contexts);

	public sealed record HitZoneContext(
		string Name,
		IReadOnlyList<HitZone> Zones);

	public sealed record HitZone(
		string Name,
		string Path,
		string Kind,
		bool Visible,
		int ZIndex,
		HitRect Bounds);

	public sealed record HitRect(float X, float Y, float Width, float Height);
}
