using System;

namespace BulletHell.Cosmetics
{
    /// <summary>
    /// What the profile file stores: the chosen look (one cosmetic ID per slot). Unlocks will join it later; all items
    /// are unlocked for now. Kept apart from the run save so Game Over and New Game never touch it.
    /// </summary>
    [Serializable]
    public sealed class ProfileData
    {
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;
        public string[] cosmetics = new string[CosmeticSlots.Count];
    }
}
