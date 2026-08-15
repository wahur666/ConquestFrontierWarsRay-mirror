using System.Xml.Serialization;

namespace ConquestFrontierWarsRay.Runtime.MapGen.Legacy;

public sealed class LegacyEnumTypeWrapper {
	[XmlAttribute("type")]
	public string Type { get; set; } = string.Empty;

	[XmlAttribute("value")]
	public string ValueName { get; set; } = string.Empty;

	[XmlText]
	public long Value { get; set; }
}
