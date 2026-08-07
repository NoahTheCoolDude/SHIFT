namespace Shift.Progression
{
    /// <summary>
    /// Three phases, and respawning is deliberately not one of them — falling costs you time,
    /// never your run. That is what makes falling cheap enough to be funny.
    /// </summary>
    public enum RunPhase : byte
    {
        Idle = 0,
        Running = 1,
        Finished = 2
    }
}
