using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Cassette;

[NetSerializable, Serializable]
public sealed class CassetteCustomTrackCountEvent(NetEntity tape, int count) : EntityEventArgs
{
    public readonly NetEntity Tape = tape;

    public readonly int Count = count;
}
