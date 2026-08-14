using System.Globalization;
using System.Numerics;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Reusable scene tree debug printer.
/// </summary>
public static class DebugTreeView {
	/// <summary>
	/// Prints one node subtree to the supplied writer.
	/// </summary>
	public static void Print(Node node, TextWriter? writer = null, string title = "Scene tree:") {
		ArgumentNullException.ThrowIfNull(node);

		var output = writer ?? Console.Out;
		output.WriteLine(title);
		WriteNode(node, depth: 0, output);
	}

	private static void WriteNode(Node node, int depth, TextWriter writer) {
		var indent = new string(' ', depth * 2);
		writer.Write(indent);
		writer.Write("- ");
		writer.Write(node.Name);
		writer.Write(" (");
		writer.Write(node.GetType().Name);
		writer.Write(')');

		var metadata = BuildMetadata(node);
		if (metadata.Count > 0) {
			writer.Write(" [");
			writer.Write(string.Join(", ", metadata));
			writer.Write(']');
		}

		writer.WriteLine();

		foreach (var child in node.Children) {
			WriteNode(child, depth + 1, writer);
		}
	}

	private static List<string> BuildMetadata(Node node) {
		var metadata = new List<string>();

		if (node is Node2D node2D) {
			metadata.Add($"local={FormatTransform(node2D.LocalTransform2D)}");
			metadata.Add($"global={FormatTransform(node2D.GlobalTransform2D)}");
		}

		if (node is CanvasItem canvasItem) {
			metadata.Add($"visible={canvasItem.Visible}");
			metadata.Add($"z={canvasItem.ZIndex}");
			metadata.Add($"globalZ={canvasItem.GlobalZIndex}");
			metadata.Add($"zRelative={canvasItem.ZAsRelative}");
			metadata.Add($"ySort={canvasItem.YSortEnabled}");
		}

		return metadata;
	}

	private static string FormatTransform(Transform2D transform) {
		return string.Create(
			CultureInfo.InvariantCulture,
			$"pos={FormatVector(transform.Position)} rot={transform.Rotation:0.###} scale={FormatVector(transform.Scale)}");
	}

	private static string FormatVector(Vector2 value) {
		return string.Create(CultureInfo.InvariantCulture, $"({value.X:0.###}, {value.Y:0.###})");
	}
}
