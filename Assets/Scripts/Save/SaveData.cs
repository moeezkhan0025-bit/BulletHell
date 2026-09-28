using System;

namespace BulletHell.Save
{
    /// <summary>
    /// What is written to the save file. Plain serializable classes and asset IDs only, never ScriptableObject
    /// references (the AssetRegistry maps IDs back to assets). An empty string means "nothing here".
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>Bump when the layout changes in a way old files can't be read.</summary>
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        /// <summary>The round whose Shop the player is in (Continue resumes at that Shop).</summary>
        public int round = 1;
        public int currency;
        /// <summary>8 slots: N, NE, E, SE, S, SW, W, NW. An empty slot has armId "".</summary>
        public ArmSave[] loadout = new ArmSave[0];
        public ArmSave[] spareArms = new ArmSave[0];
        public string[] armamentInventory = new string[0];
        /// <summary>4 ammo slots, "" = empty.</summary>
        public string[] ammoSlots = new string[0];
        public int activeAmmoSlot = -1;
    }

    /// <summary>One arm instance: its arm type and the armaments in its 3 slots.</summary>
    [Serializable]
    public sealed class ArmSave
    {
        public string armId = "";
        public string[] armamentIds = new string[0];
    }
}
