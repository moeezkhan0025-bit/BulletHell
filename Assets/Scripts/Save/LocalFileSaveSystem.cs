using System;
using System.IO;
using UnityEngine;

namespace BulletHell.Save
{
    /// <summary>
    /// The one save slot as a JSON file. Writes go to a temp file first and are then swapped in, so a crash mid-write
    /// leaves the previous save intact. A file that can't be read is reported as "no usable save", never thrown.
    /// </summary>
    public sealed class LocalFileSaveSystem : ISaveSystem
    {
        private readonly string path;
        private readonly string tempPath;

        public LocalFileSaveSystem(string filePath)
        {
            path = filePath;
            tempPath = filePath + ".tmp";
        }

        public bool HasSave => File.Exists(path);

        public bool TryLoad(out SaveData data)
        {
            data = null;
            if (!File.Exists(path))
                return false;

            try
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Save file could not be read ({path}): {e.Message}");
                return false;
            }

            if (data == null || data.version != SaveData.CurrentVersion)
            {
                Debug.LogWarning($"Save file has an unsupported version ({data?.version}); expected {SaveData.CurrentVersion}.");
                data = null;
                return false;
            }
            return true;
        }

        public void Save(SaveData data)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));
                if (File.Exists(path))
                    File.Replace(tempPath, path, null);
                else
                    File.Move(tempPath, path);
            }
            catch (Exception e)
            {
                Debug.LogError($"Saving failed ({path}): {e.Message}");
            }
        }

        public void Delete()
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Deleting the save failed ({path}): {e.Message}");
            }
        }
    }
}
