namespace ConquestFrontierWarsRay.Data.Models.MT;

public sealed class MT_STRINGPACK {
	public MT_STRINGPACK(IReadOnlyList<string> strings) {
		Strings = strings ?? throw new ArgumentNullException(nameof(strings));
	}

	public IReadOnlyList<string> Strings { get; }
}
