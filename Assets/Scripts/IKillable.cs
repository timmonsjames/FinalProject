using UnityEngine;

/// <summary>
/// Implemented by any ant class that can be killed by the player's spray,
/// the Exterminator's spray, or a borax trap.
/// </summary>
public interface IKillable
{
    /// <summary>
    /// Called when the ant is killed. The implementation should handle
    /// reward bookkeeping, episode end, deactivation, and game-state updates.
    /// </summary>
    void GetCaught();
}
