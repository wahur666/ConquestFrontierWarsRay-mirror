using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using ConquestFrontierWarsRay.Data;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;

namespace ConquestFrontierWarsRay;

internal sealed class Menu1OpeningDataReader {
	private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

	public Menu1OpeningData ReadOpening() {
		var xmlPath = Path.Combine(
			RepoPaths.LocateRepoRoot(),
			"assets",
			"DB",
			"xml",
			"GenData.db",
			"GT_MENU1",
			"Menu1.xml");

		if (!File.Exists(xmlPath)) {
			throw new FileNotFoundException($"Could not locate Menu1 XML at '{xmlPath}'.", xmlPath);
		}

		var document = XDocument.Load(xmlPath);
		var opening = document.Root?.Element("opening");
		if (opening is null) {
			throw new InvalidDataException($"Could not find <opening> in '{xmlPath}'.");
		}

		return new Menu1OpeningData(
			ParseRect(GetRequiredElement(opening, "screenRect")),
			ParseStaticData(GetRequiredElement(opening, "background")),
			ParseButtonData(GetRequiredElement(opening, "single")),
			ParseButtonData(GetRequiredElement(opening, "multi")),
			ParseButtonData(GetRequiredElement(opening, "intro")),
			ParseButtonData(GetRequiredElement(opening, "options")),
			ParseButtonData(GetRequiredElement(opening, "help")),
			ParseButtonData(GetRequiredElement(opening, "quit")),
			ParseStaticData(GetRequiredElement(opening, "staticSingle")),
			ParseStaticData(GetRequiredElement(opening, "staticMulti")),
			ParseStaticData(GetRequiredElement(opening, "staticIntro")),
			ParseStaticData(GetRequiredElement(opening, "staticOptions")),
			ParseStaticData(GetRequiredElement(opening, "staticHelp")),
			ParseAnimateData(GetRequiredElement(opening, "animMedia")),
			ParseAnimateData(GetRequiredElement(opening, "animSingle")),
			ParseAnimateData(GetRequiredElement(opening, "animMulti")),
			ParseAnimateData(GetRequiredElement(opening, "animOptions")),
			ParseAnimateData(GetRequiredElement(opening, "animQuestion")),
			ParseStaticData(GetRequiredElement(opening, "staticLegal")));
	}

	private static RECT ParseRect(XElement element) {
		return new RECT {
			Left = ReadInt(element, "left"),
			Top = ReadInt(element, "top"),
			Right = ReadInt(element, "right"),
			Bottom = ReadInt(element, "bottom")
		};
	}

	private static BUTTON_DATA ParseButtonData(XElement element) {
		return new BUTTON_DATA {
			ButtonType = ReadString(element, "buttonType"),
			ButtonText = ReadUInt(element, "buttonText"),
			XOrigin = ReadInt(element, "xOrigin"),
			YOrigin = ReadInt(element, "yOrigin"),
			ButtonArea = ParseRect(GetRequiredElement(element, "buttonArea"))
		};
	}

	private static STATIC_DATA ParseStaticData(XElement element) {
		return new STATIC_DATA {
			StaticType = ReadString(element, "staticType"),
			StaticText = ReadUInt(element, "staticText"),
			StaticTooltip = ReadUInt(element, "staticTooltip"),
			StaticHintbox = ReadUInt(element, "staticHintbox"),
			Alignment = ReadUInt(element, "alignment"),
			XOrigin = ReadInt(element, "xOrigin"),
			YOrigin = ReadInt(element, "yOrigin"),
			Width = ReadInt(element, "width"),
			Height = ReadInt(element, "height")
		};
	}

	private static ANIMATE_DATA ParseAnimateData(XElement element) {
		return new ANIMATE_DATA {
			AnimateType = ReadString(element, "animateType"),
			XOrigin = ReadInt(element, "xOrigin"),
			YOrigin = ReadInt(element, "yOrigin"),
			Timer = ReadUInt(element, "dwTimer"),
			FuzzEffect = ReadBool(element, "fuzzEffect")
		};
	}

	private static XElement GetRequiredElement(XElement parent, string name) {
		var element = FindChild(parent, name);
		return element ?? throw new InvalidDataException($"Missing <{name}> under <{parent.Name.LocalName}>.");
	}

	private static XElement? FindChild(XElement parent, string name) {
		foreach (var child in parent.Elements()) {
			if (NameComparer.Equals(child.Name.LocalName, name)) {
				return child;
			}
		}

		return null;
	}

	private static string ReadString(XElement parent, string childName) {
		return GetRequiredElement(parent, childName).Value.Trim();
	}

	private static int ReadInt(XElement parent, string childName) {
		var value = ReadString(parent, childName);
		return int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
	}

	private static uint ReadUInt(XElement parent, string childName) {
		var value = ReadString(parent, childName);
		return uint.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
	}

	private static bool ReadBool(XElement parent, string childName) {
		var value = ReadString(parent, childName);
		return bool.Parse(value);
	}
}

internal sealed record Menu1OpeningData(
	RECT ScreenRect,
	STATIC_DATA Background,
	BUTTON_DATA Single,
	BUTTON_DATA Multi,
	BUTTON_DATA Intro,
	BUTTON_DATA Options,
	BUTTON_DATA Help,
	BUTTON_DATA Quit,
	STATIC_DATA StaticSingle,
	STATIC_DATA StaticMulti,
	STATIC_DATA StaticIntro,
	STATIC_DATA StaticOptions,
	STATIC_DATA StaticHelp,
	ANIMATE_DATA AnimMedia,
	ANIMATE_DATA AnimSingle,
	ANIMATE_DATA AnimMulti,
	ANIMATE_DATA AnimOptions,
	ANIMATE_DATA AnimQuestion,
	STATIC_DATA StaticLegal) {
	public IReadOnlyList<BUTTON_DATA> Buttons =>
		[Single, Multi, Intro, Options, Help, Quit];

	public IReadOnlyList<ANIMATE_DATA> Animations =>
		[AnimMedia, AnimSingle, AnimMulti, AnimOptions, AnimQuestion];
}
