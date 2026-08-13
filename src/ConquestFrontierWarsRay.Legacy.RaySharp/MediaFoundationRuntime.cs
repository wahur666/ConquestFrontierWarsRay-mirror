using SharpGen.Runtime;
using Vortice.MediaFoundation;

namespace RaySharp;

internal sealed class MediaFoundationRuntime : IDisposable
{
    private MediaFoundationRuntime()
    {
    }

    public static MediaFoundationRuntime Start()
    {
        MediaFactory.MFStartup(false).CheckError();
        return new MediaFoundationRuntime();
    }

    public void Dispose()
    {
        MediaFactory.MFShutdown().CheckError();
    }
}
