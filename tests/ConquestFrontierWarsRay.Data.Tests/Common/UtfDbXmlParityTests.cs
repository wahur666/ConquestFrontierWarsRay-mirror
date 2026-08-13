namespace ConquestFrontierWarsRay.Data.Tests.Common;

public sealed class UtfDbXmlParityDiscoveryTests
{
    [Fact]
    public void Discovers_XmlBacked_Entries_For_All_Supported_Types_Present_In_Repo()
    {
        Assert.NotEmpty(UtfDbXmlParityTestSupport.EntriesByType);
    }
}

public abstract class UtfDbXmlParityTestBase
{
    protected static void AssertParity(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase)
    {
        UtfDbXmlParityTestSupport.AssertBinaryMatchesXml(testCase);
    }
}

public sealed class MtStringPackXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("MT_STRINGPACK");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class MtUnitSpeechXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("MT_UNITSPEECH");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtGroupXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_GROUP");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtLightXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_LIGHT");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtAirDefenseXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_AIR_DEFENSE");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtArtifactLauncherXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_ARTIFACT_LAUNCHER");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtArtileryLauncherXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_ARTILERY_LAUNCHER");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtBarrageLauncherXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_BARRAGE_LAUNCHER");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtBeamDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_BEAM_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtBuffArtifactXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_BUFF_ARTIFACT");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtBuffLauncherXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_BUFF_LAUNCHER");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtCloakLauncherXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_CLOAK_LAUNCHER");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtEffectLauncherXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_EFFECT_LAUNCHER");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtFireballDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_FIREBALL_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtJumpLauncherXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_JUMP_LAUNCHER");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtMoonResourceLauncherXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_MOON_RESOURCE_LAUNCHER");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtNovaLauncherXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_NOVA_LAUNCHER");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtPingLauncherXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_PING_LAUNCHER");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtRepairLauncherXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_REPAIR_LAUNCHER");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtMulticloakLauncherXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_MULTICLOAK_LAUNCHER");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtWormholeLauncherXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_WORMHOLE_LAUNCHER");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtAeBoltDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_AEBOLT_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtAegisDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_AEGIS_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtArcDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_ARC_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtEngineTrailXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_ENGINETRAIL");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtExtensionInfoXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_EXTENSION_INFO");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtGattlingBeamDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_GATTLINGBEAM_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtMassDisruptorDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_MASS_DISRUPTOR_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtMimicDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_MIMIC_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtProjectileDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_PROJECTILE_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtSwapperDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_SWAPPER_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtSystemBuffLauncherXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_SYSTEM_BUFF_LAUNCHER");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtTrailDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_TRAIL_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtTalorianLauncherXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_TALORIAN_LAUNCHER");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtDestabilizerDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_DESTABILIZER_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtDumbReconProbeDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_DUMBRECONPROBE_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtMeshExplosionXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_MESH_EXPLOSION");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtNovaExplosionXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_NOVA_EXPLOSION");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtParticleCircleXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_PARTICLE_CIRCLE");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtParticleDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_PARTICLE_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtPkBoltDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_PKBOLT_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtRepellentCloudDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_REPELLENTCLOUD_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtRepulsorDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_REPULSOR_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtRepulsorWaveDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_REPULSORWAVE_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtSpaceWaveDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_SPACEWAVE_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtStasisBoltDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_STASISBOLT_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtTalorianEffectXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_TALORIAN_EFFECT");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtTempHqLauncherXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_TEMPHQ_LAUNCHER");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtStreakDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_STREAK_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtTerranBuildXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_TERRAN_BUILD");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtSolarianBuildXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_SOLARIAN_BUILD");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtMantisBuildXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_MANTIS_BUILD");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtUiAnimXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_UI_ANIM");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtShipLaunchXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_SHIPLAUNCH");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtTroopPodDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_TROOPPOD_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtAnimObjDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_ANIMOBJ_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtTractorWaveLauncherXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_TRACTOR_WAVE_LAUNCHER");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtListenArtifactXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_LISTEN_ARTIFACT");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtTerraformArtifactXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_TERRAFORM_ARTIFACT");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtBuilderShipDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_BUILDERSHIP_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtBlastXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_BLAST");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtSynthesisDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_SYNTHESIS_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtNuggetDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_NUGGET_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtTurretXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_TURRET");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtZealotDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_ZEALOT_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtTractorDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_TRACTOR_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtUpgradeXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_UPGRADE");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtReconLaunchXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_RECON_LAUNCH");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtFancyLaunchXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_FANCY_LAUNCH");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtFighterWingXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_FIGHTER_WING");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtResearchXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_RESEARCH");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtFighterDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_FIGHTER_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtAdmiralResXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_ADMIRAL_RES");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtWaypointXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_WAYPOINT");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtMovieCameraDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_MOVIE_CAMERA_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtTriggerXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_TRIGGER");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtObjectGeneratorXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_OBJECT_GENERATOR");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtScriptObjectXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_SCRIPTOBJECT");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtPlasmaBoltDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_PLASMABOLT_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtAntimatterDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_ANTIMATTER_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtVerticalLaunchXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_VERTICAL_LAUNCH");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtMinefieldDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_MINEFIELD_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtFormationXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_FORMATION");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtPlanetDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_PLANET_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtNebulaDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_NEBULA_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtBlackHoleDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_BLACKHOLE_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtJumpGateDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_JUMPGATE_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtCommandKitXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_COMMAND_KIT");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtAsteroidFieldDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_ASTEROIDFIELD_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtPlatJumpDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_PLAT_JUMP_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtPlatGeneralDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_PLAT_GENERAL_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtPlatSellDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_PLAT_SELL_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtPlatRepairDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_PLAT_REPAIR_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtPlatGunXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_PLAT_GUN");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtPlatBuildDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_PLAT_BUILD_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtPlatBuildSupDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_PLAT_BUILDSUP_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtTorpedoDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_TORPEDO_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtReconProbeDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_RECONPROBE_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtSupplyShipDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_SUPPLYSHIP_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtPlatRefineDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_PLAT_REFINE_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtTroopShipDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_TROOPSHIP_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtMineLayerDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_MINELAYER_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtGunboatDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_GUNBOAT_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtHarvestShipDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_HARVESTSHIP_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtFabricatorDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_FABRICATOR_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtPlayerBombDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_PLAYERBOMB_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtFlagshipDataXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_FLAGSHIP_DATA");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtWormholeEffectXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_WORMHOLE_EFFECT");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class BtMapGenXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("BT_MAP_GEN");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtFontXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_FONT");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtCreditsXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_CREDITS");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtGlobalSoundsXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_GLOBAL_SOUNDS");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtSystemKitSaveLoadXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_SYSTEM_KIT_SAVELOAD");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtBriefingXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_BRIEFING");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtLoadSaveXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_LOADSAVE");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtPauseXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_PAUSE");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtMapSelectXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_MAPSELECT");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtSystemKitXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_SYSTEM_KIT");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtMenuObjectivesXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_MENUOBJECTIVES");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtPlayerChatMenuXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_PLAYERCHATMENU");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtGlobalValuesXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_GLOBAL_VALUES");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtOptionsXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_OPTIONS");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtDiplomacyMenuXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_DIPLOMACYMENU");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtButtonXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_BUTTON");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtStaticXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_STATIC");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtEditXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_EDIT");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtListBoxXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_LISTBOX");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtScrollBarXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_SCROLLBAR");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtProgressStaticXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_PROGRESS_STATIC");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtDropDownXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_DROPDOWN");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtComboBoxXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_COMBOBOX");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtQueueControlXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_QUEUECONTROL");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtIconXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_ICON");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtHotButtonXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_HOTBUTTON");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtHotStaticXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_HOTSTATIC");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtTabControlXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_TABCONTROL");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtSliderXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_SLIDER");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtDiplomacyButtonXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_DIPLOMACYBUTTON");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtShipSilButtonXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_SHIPSILBUTTON");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtAnimateXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_ANIMATE");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtVfxShapeXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_VFXSHAPE");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtMessageBoxXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_MESSAGEBOX");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtMovieScreenXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_MOVIESCREEN");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtIgOptionsXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_IGOPTIONS");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtNewPlayerXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_NEWPLAYER");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtSpecialAbilitiesXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_SPECIALABILITIES");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtEndgameXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_ENDGAME");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtMenu1XmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_MENU1");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtToolbarXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_TOOLBAR");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

public sealed class GtChatXmlParityTests : UtfDbXmlParityTestBase
{
    public static IEnumerable<object[]> XmlCases() => UtfDbXmlParityTestSupport.GetCasesForType("GT_CHAT");

    [Theory]
    [MemberData(nameof(XmlCases))]
    [Trait("Category", "UtfDbXmlParity")]
    public void Binary_Parsed_Data_Deep_Equals_Xml_Created_Data(UtfDbXmlParityTestSupport.UtfDbXmlCase testCase) => AssertParity(testCase);
}

