using Legacy = MapGen;

namespace ConquestFrontierWarsRay.Runtime.MapGen;

public static class LegacyMapGenDefaults {
	public static List<Legacy.BASE_FIELD_DATA> CreateBaseFieldData() {
		return [
			new("ANTIMATTER!!Antimatter", Legacy.ObjClass.OC_FIELD, Legacy.FIELDCLASS.FC_ANTIMATTER),
			new("ANTIMATTER!!mantis", Legacy.ObjClass.OC_FIELD, Legacy.FIELDCLASS.FC_ANTIMATTER),
			new("ANTIMATTER!!solarian", Legacy.ObjClass.OC_FIELD, Legacy.FIELDCLASS.FC_ANTIMATTER),
			new("ANTIMATTER!!terran", Legacy.ObjClass.OC_FIELD, Legacy.FIELDCLASS.FC_ANTIMATTER),
			new("Field!!AsteroidsHeavy", Legacy.ObjClass.OC_FIELD, Legacy.FIELDCLASS.FC_ASTEROIDFIELD),
			new("Field!!AsteroidsLight", Legacy.ObjClass.OC_FIELD, Legacy.FIELDCLASS.FC_ASTEROIDFIELD),
			new("Field!!AsteroidsMed", Legacy.ObjClass.OC_FIELD, Legacy.FIELDCLASS.FC_ASTEROIDFIELD),
			new("Field!!Debris", Legacy.ObjClass.OC_FIELD, Legacy.FIELDCLASS.FC_ASTEROIDFIELD),
			new("Nebula!!Antimatter", Legacy.ObjClass.OC_NEBULA, Legacy.FIELDCLASS.FC_ANTIMATTER),
			new("Nebula!!Antimatter(mantis)", Legacy.ObjClass.OC_NEBULA, Legacy.FIELDCLASS.FC_ANTIMATTER),
			new("Nebula!!Antimatter(solarian)", Legacy.ObjClass.OC_NEBULA, Legacy.FIELDCLASS.FC_ANTIMATTER),
			new("Nebula!!Antimatter(terran)", Legacy.ObjClass.OC_NEBULA, Legacy.FIELDCLASS.FC_ANTIMATTER),
			new("Nebula!!Celsius(terran)", Legacy.ObjClass.OC_NEBULA, Legacy.FIELDCLASS.FC_NEBULA),
			new("Nebula!!Cygnus(solarian)", Legacy.ObjClass.OC_NEBULA, Legacy.FIELDCLASS.FC_NEBULA),
			new("Nebula!!Helious(terran)", Legacy.ObjClass.OC_NEBULA, Legacy.FIELDCLASS.FC_NEBULA),
			new("Nebula!!Hyades(mantis)", Legacy.ObjClass.OC_NEBULA, Legacy.FIELDCLASS.FC_NEBULA),
			new("Nebula!!Ion(solarian)", Legacy.ObjClass.OC_NEBULA, Legacy.FIELDCLASS.FC_NEBULA),
			new("Nebula!!Lithium(mantis)", Legacy.ObjClass.OC_NEBULA, Legacy.FIELDCLASS.FC_NEBULA),
			new("BlackHole", Legacy.ObjClass.OC_NEBULA, Legacy.FIELDCLASS.FC_NEBULA),
			new("BlueStar", Legacy.ObjClass.OC_BLACKHOLE, Legacy.FIELDCLASS.FC_OTHER),
			new("GreenStar", Legacy.ObjClass.OC_BLACKHOLE, Legacy.FIELDCLASS.FC_OTHER),
			new("RedStar", Legacy.ObjClass.OC_BLACKHOLE, Legacy.FIELDCLASS.FC_OTHER),
			new("YellowStar", Legacy.ObjClass.OC_BLACKHOLE, Legacy.FIELDCLASS.FC_OTHER),
		];
	}
}
