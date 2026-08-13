namespace ConquestFrontierWarsRay.SceneReference.SceneGraph;

/// <summary>
///     Base tree node for the lightweight screen graph.
///     Owns parent/child relationships and tree traversal.
/// </summary>
public class Node {
	private readonly List<Node> _children = [];

	public Node? Parent { get; private set; }

	public IReadOnlyList<Node> Children => _children;

	public bool Visible { get; set; } = true;

	public void AddChild(Node child) {
		if (child == this) {
			throw new InvalidOperationException("A node cannot be its own child.");
		}

		for (var ancestor = this; ancestor is not null; ancestor = ancestor.Parent) {
			if (ancestor == child) {
				throw new InvalidOperationException("Cannot add an ancestor as a child.");
			}
		}

		child.Parent?.RemoveChild(child);
		child.Parent = this;
		_children.Add(child);
	}

	public bool RemoveChild(Node child) {
		if (!_children.Remove(child)) {
			return false;
		}

		child.Parent = null;
		return true;
	}

	public void ClearChildren() {
		foreach (var child in _children) {
			child.Parent = null;
		}

		_children.Clear();
	}

	public void UpdateTree(float deltaTime) {
		Update(deltaTime);

		foreach (var child in _children) {
			child.UpdateTree(deltaTime);
		}
	}

	public void DrawTree() {
		if (!Visible) {
			return;
		}

		Draw();

		foreach (var child in _children) {
			child.DrawTree();
		}
	}

	protected virtual void Update(float deltaTime) {
	}

	protected virtual void Draw() {
	}
}
