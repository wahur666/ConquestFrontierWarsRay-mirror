namespace ConquestFrontierWarsRay.Data.Models.GT;

[Flags]
public enum GT_FONT_FLAGS : uint {
	None = 0,
	Multiline = 1 << 0,
	NotScaling = 1 << 1,
	ToolbarMoney = 1 << 2
}
