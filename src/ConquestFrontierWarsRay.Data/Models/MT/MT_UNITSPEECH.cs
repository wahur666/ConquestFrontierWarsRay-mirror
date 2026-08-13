namespace ConquestFrontierWarsRay.Data.Models.MT;

public sealed class MT_UNITSPEECH {
	public MT_UNITSPEECH(IReadOnlyList<string> fields) {
		if (fields is null) {
			throw new ArgumentNullException(nameof(fields));
		}

		if (fields.Count != 22) {
			throw new ArgumentException("MT_UNITSPEECH must contain exactly 22 fields.", nameof(fields));
		}

		Fields = fields;
	}

	public IReadOnlyList<string> Fields { get; }
}
