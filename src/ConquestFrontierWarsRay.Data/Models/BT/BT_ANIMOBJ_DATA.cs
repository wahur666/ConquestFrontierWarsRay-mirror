namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_ANIMOBJ_DATA : BASIC_DATA {
	public EFFECTCLASS FxClass { get; set; }
	public string AnimName { get; set; } = string.Empty;
	public float AnimSize { get; set; }
	public float SizeVel { get; set; }
	public float LifeTime { get; set; }
	public byte FlagsRaw {
		get {
			var value = 0;
			if (FadeOut) value |= 1;
			if (FaceFront) value |= 2;
			if (Smooth) value |= 4;
			if (Looping) value |= 8;
			if (HasAlphaChannel) value |= 16;
			return (byte)value;
		}
		set {
			FadeOut = (value & 1) != 0;
			FaceFront = (value & 2) != 0;
			Smooth = (value & 4) != 0;
			Looping = (value & 8) != 0;
			HasAlphaChannel = (value & 16) != 0;
		}
	}
	public bool FadeOut { get; set; }
	public bool FaceFront { get; set; }
	public bool Smooth { get; set; }
	public bool Looping { get; set; }
	public bool HasAlphaChannel { get; set; }
}

