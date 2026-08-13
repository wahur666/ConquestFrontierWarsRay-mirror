namespace ConquestFrontierWarsRay.Data.UtfDb;

internal static class UtfDbTypeLayouts {
	public static GroupSpec BasicData(string label = "base") => new(label, [
		new ScalarSpec("obj_class", ScalarKind.U4),
		new ScalarSpec("b_edit_dropable", ScalarKind.U4)
	]);

	public static GroupSpec GenBase(string label = "base") => new(label, [
		new ScalarSpec("type", ScalarKind.U4)
	]);

	public static GroupSpec BaseLauncher(string label) => new(label, [
		new ScalarSpec("type", ScalarKind.U4),
		new ScalarSpec("weapon_type", ScalarKind.Ascii32),
		new ScalarSpec("supply_cost", ScalarKind.S4),
		new ScalarSpec("refire_period", ScalarKind.F4),
		new ScalarSpec("launcher_special_id", ScalarKind.U4)
	]);

	public static GroupSpec BaseWeaponData(string label) => new(label, [
		new ScalarSpec("wpn_class", ScalarKind.U4)
	]);

	public static GroupSpec BaseFieldData(string label) => new(label, [
		new ScalarSpec("field_class", ScalarKind.U4),
		new ScalarSpec("info_help_id", ScalarKind.U4)
	]);

	public static GroupSpec ResourceCost(string label) => new(label, [
		new ScalarSpec("gas", ScalarKind.U1),
		new ScalarSpec("metal", ScalarKind.U1),
		new ScalarSpec("crew", ScalarKind.U1),
		new ScalarSpec("command_pt", ScalarKind.U1)
	]);

	public static GroupSpec ArmorDamage(string label) => new(label, [
		new ScalarSpec("no_armor", ScalarKind.F4),
		new ScalarSpec("light_armor", ScalarKind.F4),
		new ScalarSpec("medium_armor", ScalarKind.F4),
		new ScalarSpec("heavy_armor", ScalarKind.F4)
	]);

	public static GroupSpec ArmorData(string label) => new(label, [
		new ScalarSpec("my_armor", ScalarKind.U4),
		ArmorDamage("armor_damage")
	]);

	public static GroupSpec ObjClassAndRaceAndDisplayName(string label) => new(label, [
		new ScalarSpec("raw", ScalarKind.U4)
	]);

	public static GroupSpec CapsBlock(string label) => new(label, [
		new ScalarSpec("padding", ScalarKind.U1),
		new ScalarSpec("flags_lo", ScalarKind.U2),
		new ScalarSpec("flags_hi", ScalarKind.U1)
	]);

	public static GroupSpec MissionData(string label) => new(label, [
		ObjClassAndRaceAndDisplayName("obj_class_and_race_and_display_name"),
		CapsBlock("caps_raw"),
		new ScalarSpec("hull_points_max", ScalarKind.U2),
		new ScalarSpec("supply_points_max", ScalarKind.U2),
		new ScalarSpec("scrap_value", ScalarKind.U2),
		new ScalarSpec("build_time", ScalarKind.U2),
		ResourceCost("resource_cost"),
		new ScalarSpec("sensor_radius", ScalarKind.F4),
		new ScalarSpec("cloaked_sensor_radius", ScalarKind.F4),
		new ScalarSpec("max_velocity", ScalarKind.F4),
		new ScalarSpec("base_weapon_accuracy", ScalarKind.F4),
		new ScalarSpec("base_shield_level", ScalarKind.F4),
		ArmorData("armor_data"),
		new ScalarSpec("silhouette_image", ScalarKind.U4),
		new ScalarSpec("special_ability", ScalarKind.U4),
		new ScalarSpec("special_ability1", ScalarKind.U4),
		new ScalarSpec("special_ability2", ScalarKind.U4),
		new ScalarSpec("speech_priority", ScalarKind.U4)
	]);

	public static GroupSpec FieldAttributes(string label) => new(label, [
		new ScalarSpec("sensor_damp", ScalarKind.F4),
		new ScalarSpec("to_hit_penalty", ScalarKind.F4),
		new ScalarSpec("damage", ScalarKind.F4),
		new ScalarSpec("move_speed_modifier", ScalarKind.F4),
		new ScalarSpec("maneuver_modifier", ScalarKind.F4)
	]);

	public static GroupSpec Color(string label) => new(label, [
		new ScalarSpec("red", ScalarKind.U1),
		new ScalarSpec("green", ScalarKind.U1),
		new ScalarSpec("blue", ScalarKind.U1)
	]);

	public static GroupSpec Colora(string label) => new(label, [
		new ScalarSpec("red", ScalarKind.U1),
		new ScalarSpec("green", ScalarKind.U1),
		new ScalarSpec("blue", ScalarKind.U1),
		new ScalarSpec("alpha", ScalarKind.U1)
	]);

	public static GroupSpec Vector(string label) => new(label, [
		new ScalarSpec("x", ScalarKind.F4),
		new ScalarSpec("y", ScalarKind.F4),
		new ScalarSpec("z", ScalarKind.F4)
	]);

	public static GroupSpec FlashData(string label) => new(label, [
		new ScalarSpec("life_time", ScalarKind.F4),
		new ScalarSpec("range", ScalarKind.F4),
		Color("color"),
		new PaddingSpec("padding", 1)
	]);

	public static GroupSpec BtVector(string label) => new(label, [
		new ScalarSpec("x", ScalarKind.F4),
		new ScalarSpec("y", ScalarKind.F4),
		new ScalarSpec("z", ScalarKind.F4)
	]);

	public static GroupSpec AmbientNebulaLight(string label) => new(label, [
		Color("color"),
		new ScalarSpec("padding", ScalarKind.U1),
		new ScalarSpec("pulse_frequency", ScalarKind.F4)
	]);

	public static GroupSpec ProjectileDataBase(string label) => new(label, [
		BasicData("base"),
		BaseWeaponData("wpn_class"),
		new ScalarSpec("file_name", ScalarKind.Ascii32),
		new ScalarSpec("launch_sfx", ScalarKind.U4),
		new ScalarSpec("damage", ScalarKind.U4),
		new ScalarSpec("max_velocity", ScalarKind.F4),
		new ScalarSpec("blast_type", ScalarKind.Ascii32),
		new ScalarSpec("engine_trail_type", ScalarKind.Ascii32)
	]);

	public static GroupSpec DynamicsData(string label) => new(label, [
		new ScalarSpec("linear_acceleration", ScalarKind.F4),
		new ScalarSpec("ang_acceleration", ScalarKind.F4),
		new ScalarSpec("max_linear_velocity", ScalarKind.F4),
		new ScalarSpec("max_ang_velocity", ScalarKind.F4)
	]);

	public static GroupSpec DynamicsDataJr(string label) => new(label, [
		new ScalarSpec("max_linear_velocity", ScalarKind.F4),
		new ScalarSpec("linear_acceleration", ScalarKind.F4),
		new ScalarSpec("max_ang_velocity", ScalarKind.F4)
	]);

	public static GroupSpec RockingData(string label) => new(label, [
		DynamicsData("base"),
		new ScalarSpec("rock_linear_max", ScalarKind.F4),
		new ScalarSpec("rock_ang_max", ScalarKind.F4)
	]);

	public static GroupSpec EngineGlowData(string label) => new(label, [
		new RepeatSpec(new ScalarSpec("size", ScalarKind.S4), 6),
		Color("color"),
		new ScalarSpec("engine_texture_name", ScalarKind.Ascii32),
		new ScalarSpec("padding", ScalarKind.U1)
	]);

	public static GroupSpec BlinkerData(string label) => new(label, [
		new ScalarSpec("light_script", ScalarKind.Ascii32),
		new ScalarSpec("texture_name", ScalarKind.Ascii32)
	]);

	public static GroupSpec ShieldData(string label) => new(label, [
		new ScalarSpec("mesh_name", ScalarKind.Ascii32),
		new ScalarSpec("anim_name", ScalarKind.Ascii32),
		new ScalarSpec("fizz_anim_name", ScalarKind.Ascii32),
		new ScalarSpec("sfx", ScalarKind.U4),
		new ScalarSpec("fizz_out", ScalarKind.U4),
		new ScalarSpec("fizz_in", ScalarKind.U4)
	]);

	public static GroupSpec CloakData(string label) => new(label, [
		new ScalarSpec("cloak_tex", ScalarKind.Ascii32),
		new ScalarSpec("auto_cloak", ScalarKind.U1),
		new ScalarSpec("cloak_effect_type", ScalarKind.Ascii32),
		new PaddingSpec("padding", 3)
	]);

	public static GroupSpec BillboardData(string label) => new(label, [
		new ScalarSpec("billboard_tex_name", ScalarKind.Ascii32),
		new ScalarSpec("billboard_threshhold", ScalarKind.U4),
		new ScalarSpec("b_tex2", ScalarKind.U1),
		new PaddingSpec("padding", 3)
	]);

	public static GroupSpec BillboardMesh(string label) => new(label, [
		new ScalarSpec("mesh_name", ScalarKind.Ascii32),
		new ScalarSpec("tex_name", ScalarKind.Ascii32),
		new ScalarSpec("size_min", ScalarKind.S4),
		new ScalarSpec("size_max", ScalarKind.S4),
		BtVector("offset"),
		new ScalarSpec("blend_mode", ScalarKind.U4)
	]);

	public static GroupSpec FormationFilter(string label) => new(label, [
		new ScalarSpec("flags", ScalarKind.U4)
	]);

	public static GroupSpec BaseSpaceshipData(string label) => new(label, [
		BasicData("base"),
		new ScalarSpec("type", ScalarKind.U4),
		new ScalarSpec("file_name", ScalarKind.Ascii32),
		MissionData("mission_data"),
		DynamicsData("dynamics_data"),
		RockingData("rocking_data"),
		new ScalarSpec("explosion_type", ScalarKind.Ascii32),
		new ScalarSpec("trail_type", ScalarKind.Ascii32),
		new ScalarSpec("ambient_animation", ScalarKind.Ascii32),
		new ScalarSpec("ambient_effect", ScalarKind.Ascii32),
		EngineGlowData("engine_glow"),
		BlinkerData("blinkers"),
		ShieldData("shield"),
		new ScalarSpec("damage_blast", ScalarKind.Ascii32),
		CloakData("cloak"),
		BillboardData("billboard"),
		SingleTechnode("tech_active"),
		FormationFilter("formation_filter"),
		new ScalarSpec("large_ship", ScalarKind.U4)
	]);

	public static GroupSpec ComplexColor(string label) => new(label, [
		new ScalarSpec("red_hi", ScalarKind.U1),
		new ScalarSpec("green_hi", ScalarKind.U1),
		new ScalarSpec("blue_hi", ScalarKind.U1),
		new ScalarSpec("red_lo", ScalarKind.U1),
		new ScalarSpec("green_lo", ScalarKind.U1),
		new ScalarSpec("blue_lo", ScalarKind.U1),
		new ScalarSpec("alpha_in", ScalarKind.U1),
		new ScalarSpec("alpha_out", ScalarKind.U1)
	]);

	public static GroupSpec Rect(string label) => new(label, [
		new ScalarSpec("left", ScalarKind.S4),
		new ScalarSpec("top", ScalarKind.S4),
		new ScalarSpec("right", ScalarKind.S4),
		new ScalarSpec("bottom", ScalarKind.S4)
	]);

	public static GroupSpec ButtonData(string label) => new(label, [
		new ScalarSpec("button_type", ScalarKind.Ascii32),
		new ScalarSpec("button_text", ScalarKind.U4),
		new ScalarSpec("x_origin", ScalarKind.S4),
		new ScalarSpec("y_origin", ScalarKind.S4),
		Rect("button_area")
	]);

	public static GroupSpec StaticData(string label) => new(label, [
		new ScalarSpec("static_type", ScalarKind.Ascii32),
		new ScalarSpec("static_text", ScalarKind.U4),
		new ScalarSpec("static_tooltip", ScalarKind.U4),
		new ScalarSpec("static_hintbox", ScalarKind.U4),
		new ScalarSpec("alignment", ScalarKind.U4),
		new ScalarSpec("x_origin", ScalarKind.S4),
		new ScalarSpec("y_origin", ScalarKind.S4),
		new ScalarSpec("width", ScalarKind.S4),
		new ScalarSpec("height", ScalarKind.S4)
	]);

	public static GroupSpec AnimateData(string label) => new(label, [
		new ScalarSpec("animate_type", ScalarKind.Ascii32),
		new ScalarSpec("x_origin", ScalarKind.S4),
		new ScalarSpec("y_origin", ScalarKind.S4),
		new ScalarSpec("dw_timer", ScalarKind.U4),
		new ScalarSpec("fuzz_effect", ScalarKind.U4)
	]);

	public static GroupSpec EditData(string label) => new(label, [
		new ScalarSpec("edit_type", ScalarKind.Ascii32),
		new ScalarSpec("edit_text", ScalarKind.U4),
		new ScalarSpec("x_origin", ScalarKind.S4),
		new ScalarSpec("y_origin", ScalarKind.S4)
	]);

	public static GroupSpec ListboxData(string label) => new(label, [
		new ScalarSpec("listbox_type", ScalarKind.Ascii32),
		new ScalarSpec("x_origin", ScalarKind.S4),
		new ScalarSpec("y_origin", ScalarKind.S4),
		Rect("text_area"),
		new ScalarSpec("leading_height", ScalarKind.U4),
		new ScalarSpec("flags", ScalarKind.U4)
	]);

	public static GroupSpec DiplomacyButtonData(string label) => new(label, [
		new ScalarSpec("button_type", ScalarKind.Ascii32),
		new ScalarSpec("x_origin", ScalarKind.S4),
		new ScalarSpec("y_origin", ScalarKind.S4)
	]);

	public static GroupSpec SliderData(string label) => new(label, [
		new ScalarSpec("slider_type", ScalarKind.Ascii32),
		Rect("screen_rect"),
		new ScalarSpec("x_origin", ScalarKind.S4),
		new ScalarSpec("y_origin", ScalarKind.S4)
	]);

	public static GroupSpec DropdownData(string label) => new(label, [
		new ScalarSpec("dropdown_type", ScalarKind.Ascii32),
		Rect("screen_rect"),
		ButtonData("button_data"),
		ListboxData("listbox_data")
	]);

	public static GroupSpec ComboboxData(string label) => new(label, [
		new ScalarSpec("combobox_type", ScalarKind.Ascii32),
		Rect("screen_rect"),
		EditData("edit_data"),
		ButtonData("button_data"),
		ListboxData("listbox_data")
	]);

	public static GroupSpec TabcontrolData(string label) => new(label, [
		new ScalarSpec("tab_control_type", ScalarKind.Ascii32),
		new ScalarSpec("hot_button_type", ScalarKind.Ascii32),
		new ScalarSpec("base_image", ScalarKind.S4),
		new ScalarSpec("num_tabs", ScalarKind.S4),
		new RepeatSpec(new ScalarSpec("text_id", ScalarKind.U4), 6),
		new ScalarSpec("upper_tabs", ScalarKind.U4),
		new ScalarSpec("x_pos", ScalarKind.S4),
		new ScalarSpec("y_pos", ScalarKind.S4)
	]);

	public static GroupSpec HotstaticData(string label) => new(label, [
		new ScalarSpec("base_image", ScalarKind.U4),
		new ScalarSpec("num_tech_levels", ScalarKind.U4),
		new ScalarSpec("x_origin", ScalarKind.S4),
		new ScalarSpec("y_origin", ScalarKind.S4),
		new ScalarSpec("width", ScalarKind.S4),
		new ScalarSpec("height", ScalarKind.S4),
		new ScalarSpec("bar_start_x", ScalarKind.S4),
		new ScalarSpec("bar_spacing", ScalarKind.U4),
		new ScalarSpec("text", ScalarKind.U4),
		Color("text_color"),
		new PaddingSpec("padding", 1)
	]);

	public static GroupSpec HotbuttonData(string label) => new(label, [
		new ScalarSpec("base_image", ScalarKind.U4),
		new ScalarSpec("x_origin", ScalarKind.S4),
		new ScalarSpec("y_origin", ScalarKind.S4),
		new ScalarSpec("button_text", ScalarKind.U4),
		new ScalarSpec("button_info", ScalarKind.U4),
		new ScalarSpec("button_hint", ScalarKind.U4),
		new ScalarSpec("hotkey", ScalarKind.U4),
		new ScalarSpec("disabled", ScalarKind.U4)
	]);

	public static GroupSpec IconData(string label) => new(label, [
		new ScalarSpec("base_image", ScalarKind.U4),
		new ScalarSpec("x_origin", ScalarKind.S4),
		new ScalarSpec("y_origin", ScalarKind.S4),
		new ScalarSpec("tooltip", ScalarKind.U4)
	]);

	public static GroupSpec QueuecontrolData(string label) => new(label, [
		new ScalarSpec("x_origin", ScalarKind.S4),
		new ScalarSpec("y_origin", ScalarKind.S4),
		new ScalarSpec("width", ScalarKind.S4),
		new ScalarSpec("height", ScalarKind.S4)
	]);

	public static GroupSpec MultihotbuttonData(string label) => new(label, [
		new ScalarSpec("x_origin", ScalarKind.S4),
		new ScalarSpec("y_origin", ScalarKind.S4),
		new ScalarSpec("hotkey", ScalarKind.U4),
		new ScalarSpec("single_shape", ScalarKind.U1),
		new ScalarSpec("disabled", ScalarKind.U1),
		new PaddingSpec("padding", 2)
	]);

	public static GroupSpec SingleTechnode(string label) => new(label, [
		new ScalarSpec("race_id", ScalarKind.U4),
		new ScalarSpec("tech", ScalarKind.U4),
		new ScalarSpec("build", ScalarKind.U4),
		new ScalarSpec("common", ScalarKind.U4),
		new ScalarSpec("common_extra", ScalarKind.U4),
		new ScalarSpec("cq2_vars_1", ScalarKind.U4),
		new ScalarSpec("cq2_vars_2", ScalarKind.U4)
	]);

	public static GroupSpec BuildbuttonData(string label) => new(label, [
		new ScalarSpec("base_image", ScalarKind.U4),
		new ScalarSpec("no_money_image", ScalarKind.U4),
		new ScalarSpec("x_origin", ScalarKind.S4),
		new ScalarSpec("y_origin", ScalarKind.S4),
		new ScalarSpec("rt_archetype", ScalarKind.Ascii32),
		SingleTechnode("tech_dependency"),
		SingleTechnode("tech_greyed"),
		new ScalarSpec("greyed_tooltip", ScalarKind.U4),
		new ScalarSpec("build_info", ScalarKind.U4),
		new ScalarSpec("hotkey", ScalarKind.U4),
		new ScalarSpec("disabled", ScalarKind.U1),
		new PaddingSpec("padding", 3)
	]);

	public static GroupSpec ResearchbuttonData(string label) => new(label, [
		new ScalarSpec("base_image", ScalarKind.U4),
		new ScalarSpec("no_money_image", ScalarKind.U4),
		new ScalarSpec("x_origin", ScalarKind.S4),
		new ScalarSpec("y_origin", ScalarKind.S4),
		new ScalarSpec("rt_archetype", ScalarKind.Ascii32),
		new ScalarSpec("tooltip", ScalarKind.U4),
		new ScalarSpec("research_info", ScalarKind.U4),
		new ScalarSpec("hotkey", ScalarKind.U4),
		new ScalarSpec("disabled", ScalarKind.U1),
		new PaddingSpec("padding", 3)
	]);

	public static GroupSpec ShipsilbuttonData(string label) => new(label, [
		new ScalarSpec("x_origin", ScalarKind.S4),
		new ScalarSpec("y_origin", ScalarKind.S4)
	]);

	public static GroupSpec ArtifactButtonInfo(string label) => new(label, [
		new ScalarSpec("base_button", ScalarKind.U4),
		new ScalarSpec("tooltip", ScalarKind.U4),
		new ScalarSpec("help_box", ScalarKind.U4),
		new ScalarSpec("hint_box", ScalarKind.U4)
	]);

	public static GroupSpec ExtensionData(string label) => new(label, [
		new ScalarSpec("extension_name", ScalarKind.Ascii32)
	]);

	public static GroupSpec RaceDamage(string label) => new(label, [
		new ScalarSpec("terran", ScalarKind.F4),
		new ScalarSpec("mantis", ScalarKind.F4),
		new ScalarSpec("celareon", ScalarKind.F4),
		new ScalarSpec("vyrium", ScalarKind.F4)
	]);

	public static GroupSpec BonusValues(string label) => new(label, [
		new ScalarSpec("damage", ScalarKind.F4),
		new ScalarSpec("supply_usage", ScalarKind.F4),
		new ScalarSpec("range_modifier", ScalarKind.F4),
		new ScalarSpec("speed", ScalarKind.F4),
		new ScalarSpec("sensors", ScalarKind.F4),
		new ScalarSpec("defence", ScalarKind.F4),
		new ScalarSpec("platform_damage", ScalarKind.F4),
		RaceDamage("hated_race_damage"),
		ArmorData("hated_armor_damage")
	]);

	public static GroupSpec AdmiralBonuses(string label) => new(label, [
		BonusValues("base_bonuses"),
		BonusValues("favored_ship_bonus"),
		BonusValues("favored_armor"),
		new RepeatSpec(new ScalarSpec("bonus_ships", ScalarKind.U4), 5),
		new ScalarSpec("flags", ScalarKind.U4)
	]);

	public static GroupSpec ShipFilters(string label) => new(label, [
		FormationFilter("positive_filter"),
		FormationFilter("negative_filter"),
		new ScalarSpec("max", ScalarKind.U4),
		new ScalarSpec("min", ScalarKind.U4),
		new ScalarSpec("overflow_only", ScalarKind.U4)
	]);

	public static GroupSpec AdvancedPlacement(string label) => new(label, [
		new ScalarSpec("placement_type", ScalarKind.U4),
		new ScalarSpec("placement_dir_x", ScalarKind.U4),
		new ScalarSpec("placement_dir_y", ScalarKind.U4)
	]);

	public static GroupSpec FleetGroupDef(string label) => new(label, [
		new RepeatSpec(ShipFilters("filters"), 6),
		new ScalarSpec("flags", ScalarKind.U4),
		new ScalarSpec("parent_group", ScalarKind.U4),
		new ScalarSpec("parent_group_number", ScalarKind.U4),
		new ScalarSpec("priority", ScalarKind.U4),
		new ScalarSpec("creation_number", ScalarKind.U4),
		new ScalarSpec("relative_to", ScalarKind.U4),
		new ScalarSpec("relative_group_id", ScalarKind.U4),
		new ScalarSpec("relation", ScalarKind.U4),
		new ScalarSpec("rel_dir_x", ScalarKind.U4),
		new ScalarSpec("rel_dir_y", ScalarKind.U4),
		AdvancedPlacement("advanced_placement"),
		new ScalarSpec("ai_type", ScalarKind.U4)
	]);

	public static GroupSpec DroneRelease(string label) => new(label, [
		new ScalarSpec("hardpoint", ScalarKind.Ascii64),
		new ScalarSpec("builder_type", ScalarKind.Ascii32),
		new ScalarSpec("num_drones", ScalarKind.U1)
	]);

	public static GroupSpec BaseResearchData(string label) => new(label, [
		BasicData("base"),
		new ScalarSpec("type", ScalarKind.U4),
		ResourceCost("cost"),
		new ScalarSpec("time", ScalarKind.U4)
	]);

	public static GroupSpec BasePlatformData(string label) => new(label, [
		BasicData("base"),
		new ScalarSpec("type", ScalarKind.U4),
		new ScalarSpec("file_name", ScalarKind.Ascii32),
		MissionData("mission_data"),
		new RepeatSpec(ExtensionData("extension"), 4),
		new ScalarSpec("extension_bits", ScalarKind.U1),
		new ScalarSpec("extension_level", ScalarKind.S1),
		new ScalarSpec("explosion_type", ScalarKind.Ascii32),
		new ScalarSpec("shield_hit_type", ScalarKind.Ascii32),
		new ScalarSpec("ambient_animation", ScalarKind.Ascii32),
		new ScalarSpec("ambient_effect", ScalarKind.Ascii32),
		new PaddingSpec("padding", 2),
		new ScalarSpec("mass", ScalarKind.F4),
		SingleTechnode("tech_active"),
		ShieldData("shield"),
		new ScalarSpec("slots_needed", ScalarKind.U4),
		BlinkerData("blinkers"),
		new ScalarSpec("command_points", ScalarKind.U4),
		new ScalarSpec("metal_storage", ScalarKind.U4),
		new ScalarSpec("gas_storage", ScalarKind.U4),
		new ScalarSpec("crew_storage", ScalarKind.U4),
		new ScalarSpec("size", ScalarKind.U1),
		new PaddingSpec("padding2", 2),
		new ScalarSpec("moon_platform", ScalarKind.U1)
	]);

	public static GroupSpec SystemKitLightInfo(string label) => new(label, [
		Color("color"),
		new PaddingSpec("padding1", 1),
		new ScalarSpec("range", ScalarKind.S4),
		Vector("position"),
		Vector("direction"),
		new ScalarSpec("cutoff", ScalarKind.F4),
		new ScalarSpec("infinite", ScalarKind.U1),
		new ScalarSpec("name", ScalarKind.Ascii32),
		new ScalarSpec("ambient", ScalarKind.U1),
		new PaddingSpec("padding", 2)
	]);

	public static IReadOnlyList<LayoutSpec> FixedAscii32Layout(string name, int count) {
		return Enumerable.Range(0, count)
			.Select(index => (LayoutSpec)new ScalarSpec($"{name}_{index:00}", ScalarKind.Ascii32))
			.ToArray();
	}
}
