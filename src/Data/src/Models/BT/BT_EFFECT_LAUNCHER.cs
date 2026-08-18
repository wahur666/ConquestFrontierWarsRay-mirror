namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_EFFECT_LAUNCHER : BASIC_DATA {
	public BASE_LAUNCHER Type { get; set; } = new();
	public uint WeaponDamage { get; set; }
	public float WeaponFireDelay { get; set; }
	public float WeaponVelocity { get; set; }
}

