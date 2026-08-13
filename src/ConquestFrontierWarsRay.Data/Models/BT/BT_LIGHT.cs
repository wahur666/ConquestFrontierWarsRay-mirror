using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_LIGHT : BASIC_DATA {
	public GT_COLOR Color { get; init; } = new();

	public int Range { get; init; }

	public GT_VECTOR Direction { get; init; } = new();

	public float Cutoff { get; init; }

	public BT_LIGHT_FLAGS Flags { get; init; }

	public bool Infinite => Flags.HasFlag(BT_LIGHT_FLAGS.Infinite);

	public bool Ambient => Flags.HasFlag(BT_LIGHT_FLAGS.Ambient);
}
