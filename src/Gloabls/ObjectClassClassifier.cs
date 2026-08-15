namespace ConquestFrontierWarsRay.Globals;

public static class ObjectClassClassifier {
	public static bool IsHQ(MObjClass value) {
		return value is MObjClass.M_HQ or MObjClass.M_COCOON or MObjClass.M_ACROPOLIS or MObjClass.M_LOCUS;
	}

	public static bool IsPlatform(MObjClass value) {
		return (value >= MObjClass.M_HQ && value <= MObjClass.M_CLOAKSTATION)
		       || (value >= MObjClass.M_JUMPPLAT && value <= MObjClass.M_PLASMAHIVE)
		       || (value >= MObjClass.M_ACROPOLIS && value <= MObjClass.M_PORTAL)
		       || (value >= MObjClass.M_LOCUS && value <= MObjClass.M_CYNOSURE);
	}

	public static bool IsRefinery(MObjClass value) {
		return value is MObjClass.M_REFINERY or MObjClass.M_HEAVYREFINERY or MObjClass.M_SUPERHEAVYREFINERY
			or MObjClass.M_COLLECTOR or MObjClass.M_GREATER_COLLECTOR or MObjClass.M_OXIDATOR
			or MObjClass.M_COALESCER;
	}

	public static bool IsShipyard(MObjClass value) {
		return value is MObjClass.M_LIGHTIND or MObjClass.M_HEAVYIND or MObjClass.M_THRIPID
			or MObjClass.M_NIAD or MObjClass.M_PAVILION or MObjClass.M_GREATERPAVILION
			or MObjClass.M_COMPILER;
	}

	public static bool IsRepairPlat(MObjClass value) {
		return value is MObjClass.M_REPAIR or MObjClass.M_GREATER_PLANTATION or MObjClass.M_EUTROMILL;
	}

	public static bool IsTenderPlat(MObjClass value) {
		return value is MObjClass.M_TENDER or MObjClass.M_REPAIR or MObjClass.M_PLANTATION
			or MObjClass.M_GREATER_PLANTATION or MObjClass.M_EUTROMILL;
	}

	public static bool IsMilitaryShip(MObjClass value) {
		return (value >= MObjClass.M_CORVETTE && value <= MObjClass.M_INFILTRATOR)
		       || (value >= MObjClass.M_SCOUTCARRIER && value <= MObjClass.M_TIAMAT)
		       || (value >= MObjClass.M_TAOS && value <= MObjClass.M_MONOLITH)
		       || value is MObjClass.M_TROOPSHIP or MObjClass.M_SPINELAYER
		       || (value >= MObjClass.M_MYSTIC && value <= MObjClass.M_LEVIATHIN);
	}

	public static bool IsObjectThreatening(MObjClass value) {
		if (value is MObjClass.M_LSAT or MObjClass.M_SPACESTATION or MObjClass.M_IONCANNON) {
			return true;
		}

		if (value is MObjClass.M_PLASMASPLITTER or MObjClass.M_VORAAKCANNON or MObjClass.M_PLASMAHIVE) {
			return true;
		}

		if (value is MObjClass.M_PROTEUS or MObjClass.M_HYDROFOIL or MObjClass.M_ESPCOIL or MObjClass.M_STARBURST) {
			return true;
		}

		return IsMilitaryShip(value);
	}

	public static bool IsGunboat(MObjClass value) {
		return (value >= MObjClass.M_CORVETTE && value <= MObjClass.M_INFILTRATOR)
		       || (value >= MObjClass.M_SEEKER && value <= MObjClass.M_TIAMAT)
		       || (value >= MObjClass.M_TAOS && value <= MObjClass.M_MONOLITH);
	}

	public static bool IsSeekerShip(MObjClass value) {
		return value is MObjClass.M_SEEKER or MObjClass.M_INFILTRATOR or MObjClass.M_ORACLE or MObjClass.M_MOK;
	}

	public static bool IsLightGunboat(MObjClass value) {
		return value is MObjClass.M_CORVETTE or MObjClass.M_MISSILECRUISER or MObjClass.M_INFILTRATOR
			or MObjClass.M_SCOUTCARRIER or MObjClass.M_KHAMIR or MObjClass.M_SEEKER or MObjClass.M_SPINELAYER
			or MObjClass.M_TAOS or MObjClass.M_AURORA or MObjClass.M_ORACLE
			or MObjClass.M_VIPER or MObjClass.M_MOK or MObjClass.M_NECTROP or MObjClass.M_ADDER;
	}

	public static bool IsMediumGunboat(MObjClass value) {
		return value is MObjClass.M_BATTLESHIP or MObjClass.M_LANCER
			or MObjClass.M_FRIGATE or MObjClass.M_SCOUTCARRIER or MObjClass.M_HIVECARRIER or MObjClass.M_SCARAB
			or MObjClass.M_POLARIS or MObjClass.M_TRIREME
			or MObjClass.M_COBRA or MObjClass.M_BASILISK or MObjClass.M_CROTAL or MObjClass.M_MYSTIC;
	}

	public static bool IsHeavyGunboat(MObjClass value) {
		return value is MObjClass.M_CARRIER or MObjClass.M_DREADNOUGHT or MObjClass.M_TIAMAT
			or MObjClass.M_MONOLITH or MObjClass.M_LEVIATHIN;
	}

	public static bool IsCarrier(MObjClass value) {
		return value is MObjClass.M_CARRIER or MObjClass.M_SCOUTCARRIER or MObjClass.M_HIVECARRIER or MObjClass.M_TIAMAT;
	}

	public static bool IsTroopship(MObjClass value) {
		return value is MObjClass.M_TROOPSHIP or MObjClass.M_LEECH or MObjClass.M_LEGIONAIRE or MObjClass.M_ERTRAG;
	}

	public static bool IsMinelayer(MObjClass value) {
		return value is MObjClass.M_SPINELAYER or MObjClass.M_ATLAS;
	}

	public static bool IsHarvester(MObjClass value) {
		return value is MObjClass.M_HARVEST or MObjClass.M_SIPHON or MObjClass.M_GALIOT or MObjClass.M_AGGREGATOR;
	}

	public static bool IsFlagship(MObjClass value) {
		return value is MObjClass.M_FLAGSHIP or MObjClass.M_WARLORD or MObjClass.M_HIGHCOUNSEL;
	}

	public static bool IsSupplyShip(MObjClass value) {
		return value is MObjClass.M_SUPPLY or MObjClass.M_ZORAP or MObjClass.M_STRATUM or MObjClass.M_ANACONDA;
	}

	public static bool IsFabricator(MObjClass value) {
		return value is MObjClass.M_FABRICATOR or MObjClass.M_WEAVER or MObjClass.M_FORGER or MObjClass.M_SHAPER;
	}

	public static bool IsJumpPlat(MObjClass value) {
		return value == MObjClass.M_JUMPPLAT;
	}

	public static bool IsGunPlat(MObjClass value) {
		return value is MObjClass.M_LSAT or MObjClass.M_SPACESTATION or MObjClass.M_IONCANNON
			or MObjClass.M_PLASMASPLITTER or MObjClass.M_VORAAKCANNON or MObjClass.M_PROTEUS
			or MObjClass.M_HYDROFOIL or MObjClass.M_ESPCOIL or MObjClass.M_STARBURST;
	}
}
