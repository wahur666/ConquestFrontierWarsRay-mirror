using RaySharp.MeshViewer;

namespace RaySharp;

internal readonly record struct RenderUpdateContext(float DeltaTime, UiState Ui);

internal readonly record struct RenderContext(MeshViewerModelRenderer ModelRenderer);

internal abstract class RenderableObject : IDisposable
{
    protected RenderableObject()
        : this(new Transform())
    {
    }

    protected RenderableObject(Transform transform)
    {
        Transform = transform;
    }

    public Transform Transform { get; }

    public virtual void Update(RenderUpdateContext context)
    {
    }

    public abstract void Render(RenderContext context);

    public virtual void Dispose()
    {
    }
}
