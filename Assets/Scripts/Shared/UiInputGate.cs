namespace Shift.Shared
{
    /// <summary>
    /// Set while a UI overlay owns the keyboard and mouse. Gameplay input sources check it and
    /// stand down.
    /// </summary>
    /// <remarks>
    /// Static, and deliberately so. This is "does this process's UI have focus", which can never
    /// be a per-player value — NGO, ParrelSync and Unity's Multiplayer Play Mode all run each
    /// virtual player in its own process, so each gets its own copy. It would only break under
    /// split-screen, which is not planned. Keep it a single bool: the moment it grows per-player
    /// state it belongs on a component instead.
    /// </remarks>
    public static class UiInputGate
    {
        public static bool Suppressed { get; set; }
    }
}
