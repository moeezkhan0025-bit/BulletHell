using System;

namespace BulletHell.Enemies
{
    /// <summary>
    /// An optional component on an enemy that plays its own death (a boss). When it takes the death, the Enemy keeps
    /// its body visible and only finishes (hides, raises Defeated) when the sequence calls back.
    /// </summary>
    public interface IDeathSequence
    {
        /// <summary>Starts the sequence. Returns false to let the enemy die the ordinary way at once.</summary>
        bool TryBegin(Action onFinished);
    }
}
