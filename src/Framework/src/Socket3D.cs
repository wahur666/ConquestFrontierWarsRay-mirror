using System.Numerics;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Named 3D attachment node for mesh parts, mounts, emitters, and similar anchors.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Socket3D"/> is the framework's reusable named attachment point for
/// ownership-driven content such as weapon mounts, dock points, particle anchors,
/// and mesh-part sockets.
/// </para>
/// <para>
/// The node stores local attachment transform data and derives world-space
/// attachment state from the scene hierarchy instead of flattening sockets into
/// standalone world markers.
/// </para>
/// </remarks>
public class Socket3D : Node3D {
	/// <summary>
	/// Creates a socket node.
	/// </summary>
	public Socket3D(string? name = null, string? groupName = null) : base(name) {
		GroupName = groupName ?? string.Empty;
	}

	/// <summary>
	/// Optional semantic group such as mount, muzzle, dock, or particle.
	/// </summary>
	public string GroupName { get; set; }

	/// <summary>
	/// Local socket transform relative to the owning node.
	/// </summary>
	public Transform3D SocketTransform3D => LocalTransform3D;

	/// <summary>
	/// World socket transform after parent composition.
	/// </summary>
	public Transform3D WorldSocketTransform3D => GlobalTransform3D;

	/// <summary>
	/// World-space socket orientation.
	/// </summary>
	public Quaternion WorldOrientation => GlobalRotation;

	/// <summary>
	/// Finds the first descendant socket with the given name.
	/// </summary>
	public Socket3D? FindSocket(string name, bool includeNested = true) {
		ArgumentException.ThrowIfNullOrWhiteSpace(name);

		foreach (var socket in EnumerateSockets(includeNested)) {
			if (string.Equals(socket.Name, name, StringComparison.OrdinalIgnoreCase)) {
				return socket;
			}
		}

		return null;
	}

	/// <summary>
	/// Enumerates direct or recursive descendant sockets.
	/// </summary>
	public IEnumerable<Socket3D> EnumerateSockets(bool includeNested = true) {
		foreach (var child in Children) {
			if (child is Socket3D socket) {
				yield return socket;
				if (includeNested) {
					foreach (var nested in socket.EnumerateSockets(includeNested: true)) {
						yield return nested;
					}
				}
			} else if (includeNested) {
				foreach (var nested in EnumerateSockets(child)) {
					yield return nested;
				}
			}
		}
	}

	private static IEnumerable<Socket3D> EnumerateSockets(Node node) {
		foreach (var child in node.Children) {
			if (child is Socket3D socket) {
				yield return socket;
				foreach (var nested in socket.EnumerateSockets(includeNested: true)) {
					yield return nested;
				}
			} else {
				foreach (var nested in EnumerateSockets(child)) {
					yield return nested;
				}
			}
		}
	}
}
