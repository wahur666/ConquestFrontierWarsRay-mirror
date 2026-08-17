using System;
using System.Runtime.InteropServices;

namespace ConquestFrontierWarsRay;

public sealed class AegisArchetype
{
    public string Name { get; set; } = string.Empty;

    public BtAegisData? Data { get; set; }

    public int ArchetypeIndex { get; set; } = EngineConstants.InvalidInstanceIndex;

    public IMeshArchetype? MeshArchetype { get; set; }

    ~AegisArchetype()
    {
        if (ArchetypeIndex != EngineConstants.InvalidInstanceIndex)
        {
            EngineRuntime.Instance.ReleaseArchetype(ArchetypeIndex);
        }
    }
}

public interface BASE_AEGIS_SAVELOAD {
    public bool ShieldOn { get; set; }

    public bool NetworkShieldOn { get; set; }
}

public sealed class Aegis : ObjectTransform<IBaseObject, AegisSaveLoad, AegisArchetype>, IAoeWeapon, ISaveLoad, ILauncher, BASE_AEGIS_SAVELOAD
{
    public int InstanceIndex { get; set; }

    private BtAegisData? aegisData;
    private readonly LaunchOwnerHandle launchOwner = new();
    private float accumulatedSupplies;

    public override void PhysicalUpdate(float deltaTime)
    {
    }

    public override bool Update()
    {
        var partState = new MissionPartStateReadonly(launchOwner.Target);
        if (partState.IsValid() && !partState.Value!.Caps.SpecialAbilityOk)
        {
            var currentData = aegisData ?? Archetype?.Data;
            if (currentData is not null)
            {
                if (currentData.NeededTech.RaceId != 0)
                {
                    if (GlobalTechTree.GetCurrentTechLevel(launchOwner.Target!.GetPlayerId()).HasTech(currentData.NeededTech))
                    {
                        partState.Value.Caps.SpecialAbilityOk = true;
                    }
                }
                else
                {
                    partState.Value.Caps.SpecialAbilityOk = true;
                }
            }
        }

        if (GameMatrix.Instance.IsMaster() && ShieldOn && aegisData is not null && launchOwner.Target is not null)
        {
            accumulatedSupplies += aegisData.SupplyPerSecond * EngineTime.ElapsedTime;
            if (accumulatedSupplies >= 1.0f)
            {
                var supplyUsage = (int)accumulatedSupplies;
                accumulatedSupplies -= supplyUsage;
                launchOwner.Target.UseSupplies(supplyUsage);
            }

            var partStateWritable = new MissionPartState(launchOwner.Target);
            if (partStateWritable.Value!.Supplies == 0 || partStateWritable.Value.FieldFlags.SuppliesLocked())
            {
                ShieldOn = false;
                launchOwner.Target.EffectFlags.AegisShield = false;
            }
        }

        if (ShieldOn && launchOwner.Target is not null)
        {
            var shipDamage = launchOwner.Target.QueryInterface<IShipDamage>();
            shipDamage.FancyShieldRender();
        }

        return true;
    }

    public override void Render()
    {
    }

    public override uint GetPartId()
    {
        return launchOwner.Target?.GetPartId() ?? 0;
    }

    public void InitWeapon(IBaseObject owner, Transform orientation, IBaseObject? target, uint flags = 0, System.Numerics.Vector3? position = null)
    {
    }

    public uint GetAffectedUnits(uint[] partIds, uint[] damage)
    {
        return 0;
    }

    public void SetAffectedUnits(uint[] partIds, uint[] damage)
    {
    }

    public void InitLauncher(IBaseObject owner, int ownerIndex, int animationArchetypeIndex, float range)
    {
        ArgumentNullException.ThrowIfNull(owner);
        launchOwner.Target = owner.QueryInterface<ILaunchOwner>();
    }

    public void AttackPosition(GridVector position, bool isSpecial)
    {
        throw new NotSupportedException("AttackPosition is not supported for Aegis.");
    }

    public void AttackObject(IBaseObject target)
    {
    }

    public void AttackMultiSystem(GridVector position, uint targetSystemId)
    {
    }

    public void WormAttack(IBaseObject target)
    {
    }

    public void CreateWormhole(uint systemId)
    {
    }

    public bool TestFightersRetracted()
    {
        return true;
    }

    public void SetFighterStance(FighterStance stance)
    {
    }

    public void HandlePreTakeover(uint newMissionId, uint troopId)
    {
    }

    public void TakeoverSwitchId(uint newMissionId)
    {
    }

    public uint GetSyncDataSize()
    {
        return 1;
    }

    public uint GetSyncData(Span<byte> buffer)
    {
        if (buffer.IsEmpty)
        {
            throw new ArgumentException("Sync buffer must have at least one byte.", nameof(buffer));
        }

        if (ShieldOn != NetworkShieldOn)
        {
            NetworkShieldOn = ShieldOn;
            buffer[0] = ShieldOn ? (byte)1 : (byte)0;
            return 1;
        }

        return 0;
    }

    public void PutSyncData(ReadOnlySpan<byte> buffer)
    {
        if (buffer.IsEmpty)
        {
            throw new ArgumentException("Sync buffer must have at least one byte.", nameof(buffer));
        }

        NetworkShieldOn = buffer[0] != 0;
        if (ShieldOn != NetworkShieldOn)
        {
            ShieldOn = NetworkShieldOn;
            if (launchOwner.Target is not null)
            {
                launchOwner.Target.EffectFlags.AegisShield = ShieldOn;
            }
        }
    }

    public void DoSpecialAbility(uint specialId)
    {
        if (!GameMatrix.Instance.IsMaster() || launchOwner.Target is null)
        {
            return;
        }

        if (ShieldOn)
        {
            ShieldOn = false;
            launchOwner.Target.EffectFlags.AegisShield = false;
            return;
        }

        var partState = new MissionPartState(launchOwner.Target);
        if (partState.Value!.Supplies > 0)
        {
            launchOwner.Target.EffectFlags.AegisShield = true;
            ShieldOn = true;
        }
    }

    public void DoSpecialAbility(IBaseObject target)
    {
    }

    public void DoCloak()
    {
    }

    public void SpecialAttackObject(IBaseObject target)
    {
        throw new NotSupportedException("SpecialAttackObject is not supported for Aegis.");
    }

    public void GetSpecialAbility(out UnitSpecialAbility ability, out bool isSpecialEnabled)
    {
        var partState = new MissionPartState(launchOwner.Target);
        ability = UnitSpecialAbility.Aegis;
        isSpecialEnabled = partState.Value?.Caps.SpecialAbilityOk ?? false;
    }

    public uint GetApproxDamagePerSecond()
    {
        return 0;
    }

    public void InformOfCancel()
    {
    }

    public void LauncherOpCreated(uint agentId, byte[]? buffer, uint bufferSize)
    {
    }

    public void LauncherReceiveOpData(uint agentId, byte[]? buffer, uint bufferSize)
    {
    }

    public void LauncherOpCompleted(uint agentId)
    {
    }

    public bool CanCloak()
    {
        return false;
    }

    public bool IsToggle()
    {
        return true;
    }

    public bool CanToggle()
    {
        var partState = new MissionPartState(launchOwner.Target);
        return partState.IsValid() && partState.Value!.Supplies > 0;
    }

    public bool IsOn()
    {
        return ShieldOn;
    }

    public void OnAllianceChange(uint allyMask)
    {
    }

    public IBaseObject? FindChildTarget(uint childId)
    {
        return null;
    }

    public bool Save(IFileSystem outputFile)
    {
        var file = outputFile.CreateInstance(new FileDescription("AEGIS_SAVELOAD")
        {
            Implementation = "DOS",
            DesiredAccess = FileAccessMode.Read | FileAccessMode.Write,
            ShareMode = 0,
            CreationDisposition = FileCreationDisposition.CreateAlways,
        });

        var saveData = CreateSaveLoadState();
        FrameSave(ref saveData);
        var bytes = BinaryStructSerializer.Serialize(saveData);
        file.WriteFile(0, bytes, bytes.Length, out _, null);
        return true;
    }

    public bool Load(IFileSystem inputFile)
    {
        var file = inputFile.CreateInstance(new FileDescription("AEGIS_SAVELOAD")
        {
            Implementation = "DOS",
        });

        var buffer = new byte[Marshal.SizeOf<AegisSaveLoad>()];
        file.ReadFile(0, buffer, buffer.Length, out _, null);
        var loadData = MissionRuntime.Instance.CorrelateSymbol("AEGIS_SAVELOAD", buffer, BinaryStructSerializer.Deserialize<AegisSaveLoad>);
        ApplySaveLoadState(loadData);
        FrameLoad(in loadData);
        return true;
    }

    public void ResolveAssociations()
    {
    }

    public void Initialize(AegisArchetype archetype)
    {
        ArgumentNullException.ThrowIfNull(archetype);
        Archetype = archetype;
        aegisData = archetype.Data;
    }

    private AegisSaveLoad CreateSaveLoadState()
    {
        return new AegisSaveLoad
        {
            ShieldOn = ShieldOn ? (byte)1 : (byte)0,
            NetworkShieldOn = NetworkShieldOn ? (byte)1 : (byte)0,
        };
    }

    private void ApplySaveLoadState(AegisSaveLoad state)
    {
        ShieldOn = state.ShieldOn != 0;
        NetworkShieldOn = state.NetworkShieldOn != 0;
    }

    public bool ShieldOn { get; set; }
    public bool NetworkShieldOn { get; set; }
}

public sealed class AegisManager : IObjectFactory
{
    public uint FactoryHandle { get; set; }

    public void Initialize()
    {
        var connection = ObjectListRuntime.Instance.QueryOutgoingInterface<IConnectionPoint>("IObjectFactory");
        FactoryHandle = connection.Advise(this);
    }

    public AegisArchetype? CreateArchetype(string archetypeName, ObjectClass objectClass, object? data)
    {
        if (objectClass != ObjectClass.Weapon || data is not BtAegisData aegisData || aegisData.WeaponClass != WeaponClass.Aegis)
        {
            return null;
        }

        return new AegisArchetype
        {
            Name = archetypeName,
            Data = aegisData,
        };
    }

    public bool DestroyArchetype(AegisArchetype archetype)
    {
        GC.SuppressFinalize(archetype);
        return true;
    }

    public IBaseObject CreateInstance(AegisArchetype archetype)
    {
        var instance = new Aegis
        {
            ObjectClass = ObjectClass.Effect,
        };

        instance.Initialize(archetype);
        return instance;
    }

    public void EditorCreateInstance(AegisArchetype archetype, MissionInfo missionInfo)
    {
    }
}

public static class AegisBootstrap
{
    public static AegisManager CreateManager()
    {
        return new AegisManager();
    }
}
