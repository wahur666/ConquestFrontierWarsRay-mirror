using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Framework.TestApp.Scenes;

internal abstract class ShowcaseScene : Node {
	protected ShowcaseScene(string name, string title) : base(name) {
		Title = title;
	}

	public string Title { get; }
}
