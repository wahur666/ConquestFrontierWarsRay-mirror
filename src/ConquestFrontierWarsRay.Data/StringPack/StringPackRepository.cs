using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models.MT;
using DACOM;
using DOSFile;

namespace ConquestFrontierWarsRay.Data.StringPack;

public sealed class StringPackRepository {
	private readonly IDacomRegistry _registry;
	private readonly string _databasePath;
	private readonly string _xmlRootPath;
	private readonly IFileSystem _root;

	public StringPackRepository(string databasePath, string xmlRootPath) {
		_databasePath = databasePath;
		_xmlRootPath = xmlRootPath;
		_registry = CreateRegistry();
		_root = OpenRoot(_registry, databasePath);
	}

	public string DatabasePath => _databasePath;

	public string XmlRootPath => _xmlRootPath;

	public IReadOnlyList<string> GetTypes() {
		return _root.FindFiles("*")
			.Where(entry => entry.IsDirectory)
			.Select(entry => entry.Name)
			.Where(name => StringPackSchemas.All.ContainsKey(name))
			.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}

	public IReadOnlyList<StringPackFileEntry> GetFiles(string typeName) {
		var directory = _root.CreateInstance(new DAFILEDESC(typeName), _registry);
		return directory.FindFiles("*")
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
		var directory = _root.CreateInstance(new DAFILEDESC(typeName), _registry);
		var bytes = directory.ReadAllBytes(fileName);
		return bytes;
	}

	private XDocument? LoadXml(string typeName, string fileName) {
		if (string.IsNullOrWhiteSpace(_xmlRootPath)) {
			return null;
		}

		var xmlPath = Path.Combine(_xmlRootPath, typeName, fileName + ".xml");
		return File.Exists(xmlPath) ? XDocument.Load(xmlPath) : null;
	}

	private static IDacomRegistry CreateRegistry() {
		var registry = new DacomRegistry();
		registry.RegisterComponent(
			new DelegateDacomFactory<DacomDesc>("IProfileParser", static (_, _) => new ProfileParser()));
		registry.RegisterComponent(
			new DelegateDacomFactory<DacomDesc>("IProfileParser2", static (_, _) => new ProfileParser()));
		DosFileRuntime.Register(registry);
		return registry;
	}

	private static IFileSystem OpenRoot(IDacomRegistry registry, string databasePath) {
		var searchPath = (ISearchPath)registry.CreateInstance(new SEARCHPATHDESC());
		searchPath.SetPath(databasePath);
		return searchPath.CreateInstance(new DAFILEDESC(), registry);
	}
}
