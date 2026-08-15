using MapGen;
using Newtonsoft.Json;

var mapGenPath = args.Length > 0
	? args[0]
	: Path.Combine(AppContext.BaseDirectory, "data", "bt_map_gen.json");
var seed = args.Length > 1 && int.TryParse(args[1], out var parsedSeed) ? parsedSeed : 12345;
var template = args.Length > 2 && Enum.TryParse<RANDOM_TEMPLATE>(args[2], ignoreCase: true, out var parsedTemplate)
	? parsedTemplate
	: RANDOM_TEMPLATE.TEMPLATE_RING;
var numSystems = args.Length > 3 && int.TryParse(args[3], out var parsedSystems) ? parsedSystems : 9;

var settings = new JsonSerializerSettings {
	Converters = new List<JsonConverter> { new BT_MAP_GEN_InfoConverter() }
};

if (!File.Exists(mapGenPath)) {
	Console.Error.WriteLine($"MapGen data file not found: {mapGenPath}");
	return 1;
}

var text = await File.ReadAllTextAsync(mapGenPath);
var mapGen = JsonConvert.DeserializeObject<BT_MAP_GEN>(text, settings);
if (mapGen is null) {
	Console.Error.WriteLine($"Failed to deserialize mapgen data: {mapGenPath}");
	return 1;
}

mapGen.MoonsEnabled = false;
var game = CreateDefaultGame(template, numSystems);
ValidateGame(game);

var engine = new MapGen.MapGen(mapGen, ConquestFrontierWarsRay.Runtime.MapGen.LegacyMapGenDefaults.CreateBaseFieldData());
engine.GenerateMap(game, seed);
return 0;

static FULLCQGAME CreateDefaultGame(RANDOM_TEMPLATE template, int numSystems) {
	var game = new FULLCQGAME {
		szMapName = "MigratedMapGen",
		localSlot = 0,
		numSystems = numSystems,
		money = MONEY.LOW_MONEY,
		mapType = MAPTYPE.RANDOM_MAP,
		templateType = template,
		mapSize = MAPSIZE.SMALL_MAP,
		terrain = TERRAIN.LIGHT_TERRAIN,
	};

	game.Slots.Add(new Slot {
		type = TYPE.HUMAN,
		compChalange = COMP_CHALANGE.AVERAGE_CH,
		state = STATE.READY,
		race = RACE.TERRAN,
		color = COLOR.YELLOW,
		team = TEAM.NOTEAM,
		dpid = 12345
	});

	game.Slots.Add(new Slot {
		type = TYPE.COMPUTER,
		compChalange = COMP_CHALANGE.HARD_CH,
		state = STATE.READY,
		race = RACE.MANTIS,
		color = COLOR.RED,
		team = TEAM.NOTEAM,
		dpid = 0
	});

	game.Slots.Add(new Slot {
		type = TYPE.COMPUTER,
		compChalange = COMP_CHALANGE.EASY_CH,
		state = STATE.READY,
		race = RACE.SOLARIAN,
		color = COLOR.BLUE,
		team = TEAM.NOTEAM,
		dpid = 0
	});

	game.szPlayerNames[0] = "Player1";
	game.szPlayerNames[1] = "Player2";
	game.szPlayerNames[2] = "Player3";
	return game;
}

static void ValidateGame(FULLCQGAME game) {
	if (game.templateType == RANDOM_TEMPLATE.TEMPLATE_RING && game.numSystems % game.ActiveSlots != 0) {
		throw new InvalidOperationException($"Number of systems must be a multiple of the number of players. Players: {game.ActiveSlots}, Systems: {game.numSystems}");
	}

	if (game.templateType == RANDOM_TEMPLATE.TEMPLATE_STAR && (game.numSystems - 1) % game.ActiveSlots != 0) {
		throw new InvalidOperationException($"Number of systems must be a multiple of the number of players plus 1. Players: {game.ActiveSlots}, Systems: {game.numSystems}");
	}
}
