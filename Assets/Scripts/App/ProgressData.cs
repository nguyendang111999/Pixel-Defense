using System;
using System.Collections.Generic;

namespace PixelDefense.App
{
    /// <summary>Saved player progress. Levels are keyed by stable ids, never by index.</summary>
    [Serializable]
    public sealed class ProgressData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public string CurrentLevelId;
        public int LevelNumber = 1;
        public int Coins;
        public int Freeze;
        public int Bomb;
        public int Slot;
        public List<LevelRecord> Records = new List<LevelRecord>();

        public int StarsFor(string levelId)
        {
            for (int i = 0; i < Records.Count; i++)
            {
                if (string.Equals(Records[i].Id, levelId, StringComparison.Ordinal))
                {
                    return Records[i].Stars;
                }
            }
            return 0;
        }

        public void Record(string levelId, int stars)
        {
            for (int i = 0; i < Records.Count; i++)
            {
                if (string.Equals(Records[i].Id, levelId, StringComparison.Ordinal))
                {
                    Records[i].Stars = Math.Max(Records[i].Stars, stars);
                    return;
                }
            }
            Records.Add(new LevelRecord { Id = levelId, Stars = stars });
        }
    }

    [Serializable]
    public sealed class LevelRecord
    {
        public string Id;
        public int Stars;
    }
}
