namespace ConquestFrontierWarsRay.Data.Models.BT;

[Flags]
public enum BT_LIGHT_FLAGS : uint {
	None = 0,
	Infinite = 1 << 0,
	Ambient = 1 << 1
}
