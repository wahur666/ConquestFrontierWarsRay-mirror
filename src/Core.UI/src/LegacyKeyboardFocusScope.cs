using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Core.UI;

internal static class LegacyKeyboardFocusScope {
	public static void Acquire(Node owner) {
		ArgumentNullException.ThrowIfNull(owner);

		var root = owner;
		while (root.Parent is not null) {
			root = root.Parent;
		}

		ClearNonAncestorFocus(root, owner);
	}

	private static void ClearNonAncestorFocus(Node current, Node owner) {
		if (current is ILegacyKeyboardFocusable focusable &&
		    !ReferenceEquals(current, owner) &&
		    !IsAncestorOf(current, owner)) {
			focusable.SetKeyboardFocus(false);
		}

		foreach (var child in current.Children) {
			ClearNonAncestorFocus(child, owner);
		}
	}

	private static bool IsAncestorOf(Node candidateAncestor, Node node) {
		for (var current = node.Parent; current is not null; current = current.Parent) {
			if (ReferenceEquals(current, candidateAncestor)) {
				return true;
			}
		}

		return false;
	}
}
