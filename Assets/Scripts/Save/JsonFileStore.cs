using System;
using System.IO;
using UnityEngine;

namespace BulletHell.Save
{
    /// <summary>
    /// One JSON file holding one plain serializable object (the settings file, the profile file). Same safety as the run
    /// save: written to a temp file first and swapped in, and a file that can't be read is "no data", never an exception.
    /// Version checks belong to the caller, since only it knows the current version.
    /// </summary>
    public sealed class JsonFileStore<T> where T : class
    {
        private readonly string path;
        private readonly string tempPath;

        public JsonFileStore(string filePath)
        {
            path = filePath;
            tempPath = filePath + ".tmp";
        }

        public bool Exists => File.Exists(path);

        public bool TryLoad(out T data)
        {
            data = null;
            if (!File.Exists(path))
                return false;

            try
            {
                data = JsonUtility.FromJson<T>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"File could not be read ({path}): {e.Message}");
                data = null;
                return false;
            }
            return data != null;
        }

        public void Save(T data)
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
                Debug.LogError($"Deleting failed ({path}): {e.Message}");
            }
        }
    }
}
