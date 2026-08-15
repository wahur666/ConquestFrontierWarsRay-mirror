using SharpGen.Runtime;
using Vortice.MediaFoundation;

namespace ConquestFrontierWarsRay.Framework;

internal static class MediaFoundationRuntime {
	private static readonly Lock Sync = new();
	private static int _referenceCount;

	public static Lease Acquire() {
		lock (Sync) {
			if (_referenceCount == 0) {
				MediaFactory.MFStartup(false).CheckError();
			}

			_referenceCount++;
			return new Lease();
		}
	}

	private static void Release() {
		lock (Sync) {
			if (_referenceCount == 0) {
				return;
			}

			_referenceCount--;
			if (_referenceCount == 0) {
				MediaFactory.MFShutdown().CheckError();
			}
		}
	}

	internal readonly struct Lease : IDisposable {
		public void Dispose() {
			Release();
		}
	}
}
