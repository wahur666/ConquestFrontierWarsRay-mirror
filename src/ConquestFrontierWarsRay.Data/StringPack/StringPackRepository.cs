using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models.MT;
using ConquestFrontierWarsRay.Data.DosFile;

namespace ConquestFrontierWarsRay.Data.StringPack;

public sealed class StringPackRepository {
	private readonly string _databasePath;
	private readonly string _xmlRootPath;
	private readonly DosFileReader _root;

	public StringPackRepository(string databasePath, string xmlRootPath) {
		_databasePath = databasePath;
		_xmlRootPath = xmlRootPath;
		_root = new DosFileReader(databasePath);
	}

	public string DatabasePath => _databasePath;

	public string XmlRootPath => _xmlRootPath;

	public IReadOnlyList<string> GetTypes() {
		return _root.FindFiles()
			.Where(entry => entry.IsDirectory)
			.Select(entry => entry.Name)
			.Where(name => StringPackSchemas.All.ContainsKey(name))
			.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}

	public IReadOnlyList<StringPackFileEntry> GetFiles(string typeName) {
		return _root.FindFiles(typeName)
			.Where(entry => !entry.IsDirectory)
			.OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
			.Select(entry => new StringPackFileEntry(entry.Name, checked((int)entry.Length)))
			.ToArray();
	}

	public StringPackEntryDetails ReadEntryDetails(string typeName, string fileName) {
		if (!StringPackSchemas.All.TryGetValue(typeName, out var schema)) {
			throw new InvalidOperationException($"Unsupported StringPack type '{typeName}'.");
		}

		var bytes = ReadEntryBytes(typeName, fileName);
		var xml = LoadXml(typeName, fileName);
		return schema.ParseDetails(fileName, bytes, xml);
	}

	public MT_STRINGPACK ReadMT_STRINGPACK(string fileName) {
		return StringPackSchemas.MT_STRINGPACK.Parse(fileName, ReadEntryBytes("MT_STRINGPACK", fileName));
	}

	public MT_UNITSPEECH ReadMT_UNITSPEECH(string fileName) {
		return StringPackSchemas.MT_UNITSPEECH.Parse(fileName, ReadEntryBytes("MT_UNITSPEECH", fileName));
	}

	private byte[] ReadEntryBytes(string typeName, string fileName) {
		return _root.ReadAllBytes(Path.Combine(typeName, fileName));
	}

	private XDocument? LoadXml(string typeName, string fileName) {
		if (string.IsNullOrWhiteSpace(_xmlRootPath)) {
			return null;
		}

		var xmlPath = Path.Combine(_xmlRootPath, typeName, fileName + ".xml");
		return File.Exists(xmlPath) ? XDocument.Load(xmlPath) : null;
	}
}
