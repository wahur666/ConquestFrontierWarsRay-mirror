namespace MapGen.Terrain;

public class InfoList<T> {
	public readonly LinkedList<T> Items = new();

	public void Add(T data) {
		Items.AddLast(data);
	}
}
