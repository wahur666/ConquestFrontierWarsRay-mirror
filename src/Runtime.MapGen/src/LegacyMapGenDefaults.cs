using LegacyEngine = MapGen;

namespace ConquestFrontierWarsRay.Runtime.MapGen;

public static class LegacyMapGenDefaults {
	public static List<LegacyEngine.BASE_FIELD_DATA> CreateBaseFieldData() {
		return [
			new("ANTIMATTER!!Antimatter", LegacyEngine.ObjClass.OC_FIELD, LegacyEngine.FIELDCLASS.FC_ANTIMATTER),
			new("ANTIMATTER!!mantis", LegacyEngine.ObjClass.OC_FIELD, LegacyEngine.FIELDCLASS.FC_ANTIMATTER),
			new("ANTIMATTER!!solarian", LegacyEngine.ObjClass.OC_FIELD, LegacyEngine.FIELDCLASS.FC_ANTIMATTER),
			new("ANTIMATTER!!terran", LegacyEngine.ObjClass.OC_FIELD, LegacyEngine.FIELDCLASS.FC_ANTIMATTER),
			new("Field!!AsteroidsHeavy", LegacyEngine.ObjClass.OC_FIELD, LegacyEngine.FIELDCLASS.FC_ASTEROIDFIELD),
			new("Field!!AsteroidsLight", LegacyEngine.ObjClass.OC_FIELD, LegacyEngine.FIELDCLASS.FC_ASTEROIDFIELD),
			new("Field!!AsteroidsMed", LegacyEngine.ObjClass.OC_FIELD, LegacyEngine.FIELDCLASS.FC_ASTEROIDFIELD),
			new("Field!!Debris", LegacyEngine.ObjClass.OC_FIELD, LegacyEngine.FIELDCLASS.FC_ASTEROIDFIELD),
			new("Nebula!!Antimatter", LegacyEngine.ObjClass.OC_NEBULA, LegacyEngine.FIELDCLASS.FC_ANTIMATTER),
			new("Nebula!!Antimatter(mantis)", LegacyEngine.ObjClass.OC_NEBULA, LegacyEngine.FIELDCLASS.FC_ANTIMATTER),
			new("Nebula!!Antimatter(solarian)", LegacyEngine.ObjClass.OC_NEBULA, LegacyEngine.FIELDCLASS.FC_ANTIMATTER),
			new("Nebula!!Antimatter(terran)", LegacyEngine.ObjClass.OC_NEBULA, LegacyEngine.FIELDCLASS.FC_ANTIMATTER),
			new("Nebula!!Celsius(terran)", LegacyEngine.ObjClass.OC_NEBULA, LegacyEngine.FIELDCLASS.FC_NEBULA),
			new("Nebula!!Cygnus(solarian)", LegacyEngine.ObjClass.OC_NEBULA, LegacyEngine.FIELDCLASS.FC_NEBULA),
			new("Nebula!!Helious(terran)", LegacyEngine.ObjClass.OC_NEBULA, LegacyEngine.FIELDCLASS.FC_NEBULA),
			new("Nebula!!Hyades(mantis)", LegacyEngine.ObjClass.OC_NEBULA, LegacyEngine.FIELDCLASS.FC_NEBULA),
			new("Nebula!!Ion(solarian)", LegacyEngine.ObjClass.OC_NEBULA, LegacyEngine.FIELDCLASS.FC_NEBULA),
			new("Nebula!!Lithium(mantis)", LegacyEngine.ObjClass.OC_NEBULA, LegacyEngine.FIELDCLASS.FC_NEBULA),
			new("BlackHole", LegacyEngine.ObjClass.OC_NEBULA, LegacyEngine.FIELDCLASS.FC_NEBULA),
			new("BlueStar", LegacyEngine.ObjClass.OC_BLACKHOLE, LegacyEngine.FIELDCLASS.FC_OTHER),
			new("GreenStar", LegacyEngine.ObjClass.OC_BLACKHOLE, LegacyEngine.FIELDCLASS.FC_OTHER),
			new("RedStar", LegacyEngine.ObjClass.OC_BLACKHOLE, LegacyEngine.FIELDCLASS.FC_OTHER),
			new("YellowStar", LegacyEngine.ObjClass.OC_BLACKHOLE, LegacyEngine.FIELDCLASS.FC_OTHER),
		];
	}
}
