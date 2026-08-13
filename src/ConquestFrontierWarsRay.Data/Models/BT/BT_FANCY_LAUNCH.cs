namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_FANCY_LAUNCH : BASIC_DATA {
	public BASE_LAUNCHER Type { get; set; } = new();
	public string Animation { get; set; } = string.Empty;
	public string Hardpoint { get; set; } = string.Empty;
	public float AnimTime { get; set; }
	public float EffectDuration { get; set; }
	public uint WarmupSound { get; set; }
	public BT_SINGLE_TECHNODE NeededTech { get; set; } = new();
	public uint Flags {
		get {
			uint value = 0;
			if (BSpecialWeapon) value |= 1;
			if (BTargetRequired) value |= 2;
			if (BWormHole) value |= 4;
			return value;
		}
		set {
			BSpecialWeapon = (value & 1) != 0;
			BTargetRequired = (value & 2) != 0;
			BWormHole = (value & 4) != 0;
		}
	}
	public UNIT_SPECIAL_ABILITY SpecialAbility { get; set; }
	public bool BSpecialWeapon { get; set; }
	public bool BTargetRequired { get; set; }
	public bool BWormHole { get; set; }
}

