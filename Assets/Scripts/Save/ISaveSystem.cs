namespace BulletHell.Save
{
    /// <summary>
    /// Single-slot save storage. The local file version lives in Scripts/Save; platform save APIs (Steam Cloud,
    /// iCloud, console save data) plug in later as other implementations, under Scripts/Platform.
    /// </summary>
    public interface ISaveSystem
    {
        /// <summary>True when a save exists (it may still turn out to be unreadable; see TryLoad).</summary>
        bool HasSave { get; }

        /// <summary>Loads the save. Returns false when there is none or it can't be read.</summary>
        bool TryLoad(out SaveData data);

        void Save(SaveData data);

        void Delete();
    }
}
