namespace Shift.Core
{
    /// <summary>What a set of primitives is being authored for. See the "Applies to" column in
    /// Docs/primitives.md.</summary>
    /// <remarks>
    /// Values are serialized into scenes and assets — never renumber. A "Field" is a trigger
    /// volume; in CLAUDE.md a "Zone" is a level, so the two must not share a name.
    /// </remarks>
    public enum PrimitiveSubject : byte
    {
        Player = 0,
        Prop = 1,
        Surface = 2,
        Field = 3
    }
}
