namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_RECON_LAUNCH : BASIC_DATA {
	public BASE_LAUNCHER Type { get; set; } = new();
	public string Animation { get; set; } = string.Empty;
	public string Hardpoint { get; set; } = string.Empty;
	public float AnimTime { get; set; }
	public float EffectDuration { get; set; }
	public uint WarmupSound { get; set; }
	public BT_SINGLE_TECHNODE NeededTech { get; set; } = new();
	public UNIT_SPECIAL_ABILITY SpecialAbility { get; set; }
	public bool WormWeapon { get; set; }
	public bool SelfTarget { get; set; }
}

