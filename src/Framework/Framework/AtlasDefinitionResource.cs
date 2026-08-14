using System.Text.Json;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Loads atlas frame data from a JSON file.
/// </summary>
public sealed class AtlasDefinitionResource : Resource {
	private AtlasDefinition? _definition;

	/// <summary>
	/// Creates an atlas definition resource from a file path.
	/// </summary>
	public AtlasDefinitionResource(string path) : base(path) {
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
	}

	/// <summary>
	/// All frames defined in the atlas file.
	/// </summary>
	public IReadOnlyList<AtlasFrame> Frames {
		get {
			EnsureLoaded();
			return _definition!.Frames;
		}
	}

	/// <summary>
	/// Gets one frame by index.
	/// </summary>
	public AtlasFrame GetFrame(int index) {
		EnsureLoaded();

		if (_definition!.Frames.Count == 0) {
			throw new InvalidOperationException($"Atlas definition '{ResourcePath}' contains no frames.");
		}

		return _definition.Frames[index];
	}

	protected override void LoadCore() {
		var json = File.ReadAllText(ResourcePath!);
		_definition = JsonSerializer.Deserialize<AtlasDefinition>(json, new JsonSerializerOptions {
			PropertyNameCaseInsensitive = true
		}) ?? throw new InvalidOperationException($"Failed to deserialize atlas definition '{ResourcePath}'.");

		if (_definition.Frames.Count == 0) {
			throw new InvalidOperationException($"Atlas definition '{ResourcePath}' contains no frames.");
		}
	}

	protected override void UnloadCore() {
		_definition = null;
	}

	/// <summary>
	/// One atlas frame rectangle.
	/// </summary>
	public readonly record struct AtlasFrame(float X, float Y, float Width, float Height);

	private sealed class AtlasDefinition {
		public List<AtlasFrame> Frames { get; init; } = [];
	}
}
