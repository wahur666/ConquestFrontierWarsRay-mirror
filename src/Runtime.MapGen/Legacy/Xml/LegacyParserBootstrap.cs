namespace ConquestFrontierWarsRay.Runtime.MapGen.Legacy;

public static class LegacyParserBootstrap {
	private static readonly Lock InitializationLock = new();
	private static bool _isInitialized;

	public static void EnsureRegistered() {
		lock (InitializationLock) {
			if (_isInitialized) {
				return;
			}

			LegacyParserRegistry.Register<BT_MAP_GEN>(nameof(BT_MAP_GEN));
			_isInitialized = true;
		}
	}
}
