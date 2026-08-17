namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Framework light kinds for 3D scene nodes.
/// </summary>
public enum Light3DType {
	/// <summary>
	/// Infinite light with direction only.
	/// Matches raylib's stock <c>LIGHT_DIRECTIONAL</c> value.
	/// </summary>
	Directional = 0,

	/// <summary>
	/// Omni-directional point light.
	/// Matches raylib's stock <c>LIGHT_POINT</c> value.
	/// </summary>
	Point = 1,

	/// <summary>
	/// Cone-shaped spotlight.
	/// This extends the stock raylib helper model, which only defines directional and point lights.
	/// </summary>
	Spot = 2
}
