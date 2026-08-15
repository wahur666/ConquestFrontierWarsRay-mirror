using System.Xml.Serialization;

namespace ConquestFrontierWarsRay.Runtime.MapGen.Legacy;

public enum DMAP_FUNC {
	LINEAR = 0,
	LESS_IS_LIKLY,
	MORE_IS_LIKLY
}

public enum PLACEMENT {
	RANDOM = 0,
	CLUSTER,
	PLANET_RING,
	STREEKS,
	SPOTS,
}

public enum OVERLAP {
	NO_OVERLAP = 0,
	LEVEL1,
	LEVEL2
}

public enum SECTOR_SIZE {
	SMALL_SIZE = 0x01,
	MEDIUM_SIZE = 0x02,
	LARGE_SIZE = 0x04,
	S_M_SIZE = 0x03,
	S_L_SIZE = 0x05,
	M_L_SIZE = 0x06,
	ALL_SIZE = 0x07
}

public enum MACRO_OPERATION {
	MC_PLACE_HABITABLE_PLANET,
	MC_PLACE_GAS_PLANET,
	MC_PLACE_METAL_PLANET,
	MC_PLACE_OTHER_PLANET,
	MC_PLACE_TERRAIN,
	MC_PLACE_PLAYER_BOMB,
	MC_MARK_RING
}

[XmlRoot("BT_MAP_GEN")]
public sealed class BT_MAP_GEN {
	[XmlAttribute("id")]
	public string Id { get; set; } = string.Empty;

	[XmlElement("terrainTheme")]
	public List<TerrainTheme> Themes { get; set; } = [];
}

public sealed class TerrainTheme {
	[XmlArray("systemKit")]
	[XmlArrayItem("element")]
	public List<string> SystemKit { get; set; } = [];

	[XmlArray("metalPlanets")]
	[XmlArrayItem("element")]
	public List<string> MetalPlanets { get; set; } = [];

	[XmlArray("gasPlanets")]
	[XmlArrayItem("element")]
	public List<string> GasPlanets { get; set; } = [];

	[XmlArray("habitablePlanets")]
	[XmlArrayItem("element")]
	public List<string> HabitablePlanets { get; set; } = [];

	[XmlArray("otherPlanets")]
	[XmlArrayItem("element")]
	public List<string> OtherPlanets { get; set; } = [];

	[XmlArray("moonTypes")]
	[XmlArrayItem("element")]
	public List<string> MoonTypes { get; set; } = [];

	[XmlElement("sizeOk")]
	public LegacyEnumTypeWrapper SizeOkRaw { get; set; } = new();

	[XmlIgnore]
	public SECTOR_SIZE SizeOk => (SECTOR_SIZE)SizeOkRaw.Value;

	[XmlElement("minSize")]
	public uint MinSize { get; set; }

	[XmlElement("maxSize")]
	public uint MaxSize { get; set; }

	[XmlElement("sizeFunc")]
	public LegacyEnumTypeWrapper SizeFuncRaw { get; set; } = new();

	[XmlIgnore]
	public DMAP_FUNC SizeFunc => (DMAP_FUNC)SizeFuncRaw.Value;

	[XmlArray("numHabitablePlanets")]
	[XmlArrayItem("element")]
	public List<uint> NumHabitablePlanets { get; set; } = [];

	[XmlArray("numMetalPlanets")]
	[XmlArrayItem("element")]
	public List<uint> NumMetalPlanets { get; set; } = [];

	[XmlArray("numGasPlanets")]
	[XmlArrayItem("element")]
	public List<uint> NumGasPlanets { get; set; } = [];

	[XmlArray("numOtherPlanets")]
	[XmlArrayItem("element")]
	public List<uint> NumOtherPlanets { get; set; } = [];

	[XmlElement("minMoonPerPlanet")]
	public uint MinMoonPerPlanet { get; set; }

	[XmlElement("maxMoonPerPlanet")]
	public uint MaxMoonPerPlanet { get; set; }

	[XmlElement("moonNumberFunc")]
	public LegacyEnumTypeWrapper MoonNumberFuncRaw { get; set; } = new();

	[XmlIgnore]
	public DMAP_FUNC MoonNumberFunc => (DMAP_FUNC)MoonNumberFuncRaw.Value;

	[XmlArray("numNuggetPatchesMetal")]
	[XmlArrayItem("element")]
	public List<uint> NumNuggetPatchesMetal { get; set; } = [];

	[XmlArray("numNuggetPatchesGas")]
	[XmlArrayItem("element")]
	public List<uint> NumNuggetPatchesGas { get; set; } = [];

	[XmlArray("terrain")]
	[XmlArrayItem("terrainInfo")]
	public List<TerrainInfo> Terrain { get; set; } = [];

	[XmlArray("nuggetMetalTypes")]
	[XmlArrayItem("terrainInfo")]
	public List<TerrainInfo> NuggetMetalTypes { get; set; } = [];

	[XmlArray("nuggetGasTypes")]
	[XmlArrayItem("terrainInfo")]
	public List<TerrainInfo> NuggetGasTypes { get; set; } = [];

	[XmlElement("okForPlayerStart")]
	public bool OkForPlayerStart { get; set; }

	[XmlElement("okForRemoteSystem")]
	public bool OkForRemoteSystem { get; set; }

	[XmlArray("density")]
	[XmlArrayItem("element")]
	public List<float> Density { get; set; } = [];

	[XmlArray("macros")]
	[XmlArrayItem("macro")]
	public List<Macro> Macros { get; set; } = [];
}

public sealed class TerrainInfo {
	[XmlElement("terrainArchType")]
	public string TerrainArchType { get; set; } = string.Empty;

	[XmlElement("probability")]
	public float Probability { get; set; }

	[XmlElement("minToPlace")]
	public uint MinToPlace { get; set; }

	[XmlElement("maxToPlace")]
	public uint MaxToPlace { get; set; }

	[XmlElement("numberFunc")]
	public LegacyEnumTypeWrapper NumberFuncRaw { get; set; } = new();

	[XmlIgnore]
	public DMAP_FUNC NumberFunc => (DMAP_FUNC)NumberFuncRaw.Value;

	[XmlElement("size")]
	public uint Size { get; set; }

	[XmlElement("requiredToPlace")]
	public uint RequiredToPlace { get; set; }

	[XmlElement("overlap")]
	public LegacyEnumTypeWrapper OverlapRaw { get; set; } = new();

	[XmlIgnore]
	public OVERLAP Overlap => (OVERLAP)OverlapRaw.Value;

	[XmlElement("placement")]
	public LegacyEnumTypeWrapper PlacementRaw { get; set; } = new();

	[XmlIgnore]
	public PLACEMENT Placement => (PLACEMENT)PlacementRaw.Value;
}

public sealed class Macro {
	[XmlElement("operation")]
	public LegacyEnumTypeWrapper OperationRaw { get; set; } = new();

	[XmlIgnore]
	public MACRO_OPERATION Operation => (MACRO_OPERATION)OperationRaw.Value;

	[XmlElement("range")]
	public uint Range { get; set; }

	[XmlElement("active")]
	public bool Active { get; set; }

	[XmlElement("info")]
	public TerrainInfo Info { get; set; } = new();
}
