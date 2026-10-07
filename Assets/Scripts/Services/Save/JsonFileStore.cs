using System;
using System.IO;
using UnityEngine;

namespace PixelDefense.Services.Save
{
    /// <summary>
    /// JsonUtility persistence in <see cref="Application.persistentDataPath"/>. Writes go to a temp file that
    /// replaces the original, keeping the previous version as a .bak used when the main file is unreadable.
    /// </summary>
    public sealed class JsonFileStore
    {
        private readonly string _directory;

        public JsonFileStore() : this(Application.persistentDataPath)
        {
        }

        public JsonFileStore(string directory)
        {
            _directory = directory;
        }

        public bool TryLoad<T>(string name, out T data) where T : class
        {
            string path = PathFor(name);
            if (TryRead(path, out data) || TryRead(path + ".bak", out data))
            {
                return true;
            }

            data = null;
            return false;
        }

        public void Save<T>(string name, T data) where T : class
        {
            string path = PathFor(name);
            string temp = path + ".tmp";
            try
            {
                Directory.CreateDirectory(_directory);
                File.WriteAllText(temp, JsonUtility.ToJson(data));
                if (File.Exists(path))
                {
                    File.Replace(temp, path, path + ".bak");
                }
                else
                {
                    File.Move(temp, path);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("Save failed for '" + name + "': " + exception.Message);
            }
        }

        private string PathFor(string name)
        {
            return Path.Combine(_directory, name + ".json");
        }

        private static bool TryRead<T>(string path, out T data) where T : class
        {
            data = null;
            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }

                data = JsonUtility.FromJson<T>(File.ReadAllText(path));
                return data != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Could not read '" + path + "': " + exception.Message);
                return false;
            }
        }
    }
}
