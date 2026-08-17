using System;
using System.Runtime.InteropServices;

namespace ConquestFrontierWarsRay;

public static class EngineConstants
{
    public const int InvalidInstanceIndex = -1;
}

[StructLayout(LayoutKind.Sequential)]
public struct AegisSaveLoad
{
    public byte ShieldOn;
    public byte NetworkShieldOn;
}

public enum ObjectClass
{
    Weapon,
    Effect,
}

public enum WeaponClass
{
    Aegis,
}

public enum FighterStance
{
    Default,
}

public enum UnitSpecialAbility
{
    Aegis,
}

[Flags]
public enum FileAccessMode
{
    Read = 1,
    Write = 2,
}

public enum FileCreationDisposition
{
    CreateAlways,
}

public sealed class FileDescription
{
    public FileDescription(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public string? Implementation { get; set; }

    public FileAccessMode DesiredAccess { get; set; }

    public uint ShareMode { get; set; }

    public FileCreationDisposition CreationDisposition { get; set; }
}

public sealed class BtAegisData
{
    public TechRequirement NeededTech { get; set; } = new();

    public float SupplyPerSecond { get; set; }

    public WeaponClass WeaponClass { get; set; }
}

public sealed class TechRequirement
{
    public uint RaceId { get; set; }
}

public sealed class TechLevel
{
    public bool HasTech(TechRequirement techRequirement)
    {
        throw new NotImplementedException();
    }
}

public static class GlobalTechTree
{
    public static TechLevel GetCurrentTechLevel(uint playerId)
    {
        throw new NotImplementedException();
    }
}

public static class EngineTime
{
    public static float ElapsedTime => throw new NotImplementedException();
}

public sealed class GameMatrix
{
    public static GameMatrix Instance { get; } = new();

    public bool IsMaster()
    {
        throw new NotImplementedException();
    }
}

public sealed class EngineRuntime
{
    public static EngineRuntime Instance { get; } = new();

    public void ReleaseArchetype(int archetypeIndex)
    {
        throw new NotImplementedException();
    }
}

public sealed class ObjectListRuntime
{
    public static ObjectListRuntime Instance { get; } = new();

    public T QueryOutgoingInterface<T>(string name)
        where T : class
    {
        throw new NotImplementedException();
    }
}

public sealed class MissionRuntime
{
    public static MissionRuntime Instance { get; } = new();

    public T CorrelateSymbol<T>(string symbolName, byte[] buffer, Func<byte[], T> deserializer)
    {
        return deserializer(buffer);
    }
}

public static class BinaryStructSerializer
{
    public static byte[] Serialize<T>(T value)
        where T : unmanaged
    {
        var bytes = new byte[Marshal.SizeOf<T>()];
        MemoryMarshal.Write(bytes, in value);
        return bytes;
    }

    public static T Deserialize<T>(byte[] buffer)
        where T : unmanaged
    {
        return MemoryMarshal.Read<T>(buffer);
    }
}

public struct GridVector
{
}

public struct Transform
{
}

public sealed class MissionInfo
{
}

public interface IMeshArchetype
{
}

public interface IBaseObject
{
    ObjectClass ObjectClass { get; set; }

    EffectFlags EffectFlags { get; }

    FieldFlags FieldFlags { get; }

    MissionPartCaps Caps { get; }

    int Supplies { get; set; }

    uint GetPartId();

    uint GetPlayerId();

    T QueryInterface<T>()
        where T : class;

    void UseSupplies(int amount);
}

public interface IAoeWeapon
{
    void InitWeapon(IBaseObject owner, Transform orientation, IBaseObject? target, uint flags = 0, System.Numerics.Vector3? position = null);

    uint GetAffectedUnits(uint[] partIds, uint[] damage);

    void SetAffectedUnits(uint[] partIds, uint[] damage);
}

public interface ISaveLoad
{
    bool Save(IFileSystem outputFile);

    bool Load(IFileSystem inputFile);

    void ResolveAssociations();
}

public interface ILauncher
{
    void InitLauncher(IBaseObject owner, int ownerIndex, int animationArchetypeIndex, float range);

    void AttackPosition(GridVector position, bool isSpecial);

    void AttackObject(IBaseObject target);

    void AttackMultiSystem(GridVector position, uint targetSystemId);

    void WormAttack(IBaseObject target);

    void CreateWormhole(uint systemId);

    bool TestFightersRetracted();

    void SetFighterStance(FighterStance stance);

    void HandlePreTakeover(uint newMissionId, uint troopId);

    void TakeoverSwitchId(uint newMissionId);

    uint GetSyncDataSize();

    uint GetSyncData(Span<byte> buffer);

    void PutSyncData(ReadOnlySpan<byte> buffer);

    void DoSpecialAbility(uint specialId);

    void DoSpecialAbility(IBaseObject target);

    void DoCloak();

    void SpecialAttackObject(IBaseObject target);

    void GetSpecialAbility(out UnitSpecialAbility ability, out bool isSpecialEnabled);

    uint GetApproxDamagePerSecond();

    void InformOfCancel();

    void LauncherOpCreated(uint agentId, byte[]? buffer, uint bufferSize);

    void LauncherReceiveOpData(uint agentId, byte[]? buffer, uint bufferSize);

    void LauncherOpCompleted(uint agentId);

    bool CanCloak();

    bool IsToggle();

    bool CanToggle();

    bool IsOn();

    void OnAllianceChange(uint allyMask);

    IBaseObject? FindChildTarget(uint childId);
}

public interface ILaunchOwner : IBaseObject
{
}

public interface IShipDamage
{
    void FancyShieldRender();
}

public interface IObjectFactory
{
    AegisArchetype? CreateArchetype(string archetypeName, ObjectClass objectClass, object? data);

    bool DestroyArchetype(AegisArchetype archetype);

    IBaseObject CreateInstance(AegisArchetype archetype);

    void EditorCreateInstance(AegisArchetype archetype, MissionInfo missionInfo);
}

public interface IConnectionPoint
{
    uint Advise(IObjectFactory factory);
}

public interface IFileSystem
{
    IFileSystem CreateInstance(FileDescription description);

    void WriteFile(long offset, byte[] buffer, int bytesToWrite, out uint bytesWritten, object? overlapped);

    void ReadFile(long offset, byte[] buffer, int bytesToRead, out uint bytesRead, object? overlapped);
}

public sealed class LaunchOwnerHandle
{
    public ILaunchOwner? Target { get; set; }
}

public sealed class EffectFlags
{
    public bool AegisShield { get; set; }
}

public sealed class FieldFlags
{
    public bool SuppliesLocked()
    {
        throw new NotImplementedException();
    }
}

public sealed class MissionPartCaps
{
    public bool SpecialAbilityOk { get; set; }
}

public sealed class MissionPartState
{
    public MissionPartState(ILaunchOwner? value)
    {
        Value = value;
    }

    public ILaunchOwner? Value { get; }

    public bool IsValid()
    {
        return Value is not null;
    }
}

public sealed class MissionPartStateReadonly
{
    public MissionPartStateReadonly(ILaunchOwner? value)
    {
        Value = value;
    }

    public ILaunchOwner? Value { get; }

    public bool IsValid()
    {
        return Value is not null;
    }
}

public abstract class BaseObject : IBaseObject
{
    public ObjectClass ObjectClass { get; set; }

    public EffectFlags EffectFlags { get; } = new();

    public FieldFlags FieldFlags { get; } = new();

    public MissionPartCaps Caps { get; } = new();

    public int Supplies { get; set; }

    public virtual uint GetPartId()
    {
        throw new NotImplementedException();
    }

    public virtual uint GetPlayerId()
    {
        throw new NotImplementedException();
    }

    public virtual T QueryInterface<T>()
        where T : class
    {
        if (this is T typedThis)
        {
            return typedThis;
        }

        throw new NotImplementedException();
    }

    public virtual void UseSupplies(int amount)
    {
        throw new NotImplementedException();
    }
}

public abstract class ObjectTransform<TBase, TSaveLoad, TArchetype> : BaseObject
    where TBase : class
    where TSaveLoad : unmanaged
    where TArchetype : class
{
    protected TArchetype? Archetype { get; set; }

    public virtual void PhysicalUpdate(float deltaTime)
    {
        throw new NotImplementedException();
    }

    public virtual bool Update()
    {
        throw new NotImplementedException();
    }

    public virtual void Render()
    {
        throw new NotImplementedException();
    }

    protected void FrameSave(ref TSaveLoad saveData)
    {
    }

    protected void FrameLoad(in TSaveLoad loadData)
    {
    }
}
