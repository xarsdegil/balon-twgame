using System;
using System.IO;
using UnityEngine;

namespace BalloonGame
{
    public static class BalloonUserSettingsStore
    {
        private const string FileName = "balloon-user-settings.json";
        private static string SettingsPath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool TryLoad(out BalloonUserSettings settings)
        {
            settings = null;
            try
            {
                if (!File.Exists(SettingsPath))
                    return false;
                settings = JsonUtility.FromJson<BalloonUserSettings>(File.ReadAllText(SettingsPath));
                return settings != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("User settings could not be loaded: " + exception.Message);
                return false;
            }
        }

        public static void Save(BalloonUserSettings settings)
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(SettingsPath, JsonUtility.ToJson(settings, true));
        }

        public static void Delete()
        {
            if (File.Exists(SettingsPath))
                File.Delete(SettingsPath);
        }
    }
}

