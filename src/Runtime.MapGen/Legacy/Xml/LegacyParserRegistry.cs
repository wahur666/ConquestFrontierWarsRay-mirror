using System.Xml.Linq;
using System.Xml.Serialization;

namespace ConquestFrontierWarsRay.Runtime.MapGen.Legacy;

public static class LegacyParserRegistry {
	private static readonly Dictionary<string, Type> Registry = new(StringComparer.Ordinal);

	public static void Register<T>(string name) where T : class
		=> Registry[name] = typeof(T);

	public static T Parse<T>(string xml) where T : class {
		try {
			var document = XDocument.Parse(xml);
			var rootName = document.Root?.Name.LocalName
				?? throw new InvalidOperationException("XML does not have a root element.");

			if (!Registry.TryGetValue(rootName, out var type) || type != typeof(T)) {
				throw new InvalidOperationException($"Type '{rootName}' is not registered or mismatched.");
			}

			var serializer = new XmlSerializer(typeof(T));
			using var reader = document.CreateReader();
			try {
				var result = (T?)serializer.Deserialize(reader);
				return result ?? throw new InvalidOperationException($"Deserialization returned null for type '{typeof(T).Name}'.");
			}
			catch (InvalidOperationException exception) when (exception.InnerException is not null) {
				throw new InvalidOperationException(
					$"XML deserialization error for type '{typeof(T).Name}': {exception.InnerException.Message}",
					exception.InnerException);
			}
		}
		catch (Exception exception) {
			var message = exception.InnerException?.Message ?? exception.Message;
			throw new InvalidOperationException($"Failed to parse XML for type '{typeof(T).Name}': {message}", exception);
		}
	}
}
