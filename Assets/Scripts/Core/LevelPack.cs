using System.Collections.Generic;

namespace PixelDefense.Core
{
    /// <summary>A parsed level pack plus every problem found while parsing or validating it.</summary>
    public sealed class LevelPack
    {
        public const int SupportedVersion = 1;

        public LevelPack(string name, int version, List<LevelDefinition> levels, List<LevelIssue> issues)
        {
            Name = name;
            Version = version;
            Levels = levels;
            Issues = issues;
        }

        public string Name { get; }
        public int Version { get; }
        public IReadOnlyList<LevelDefinition> Levels { get; }
        public IReadOnlyList<LevelIssue> Issues { get; }

        public bool HasErrors
        {
            get
            {
                for (int i = 0; i < Issues.Count; i++)
                {
                    if (Issues[i].IsError)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public int IndexOf(string levelId)
        {
            for (int i = 0; i < Levels.Count; i++)
            {
                if (string.Equals(Levels[i].Id, levelId, System.StringComparison.Ordinal))
                {
                    return i;
                }
            }
            return -1;
        }
    }

    public readonly struct LevelIssue
    {
        public readonly string Pack;
        public readonly string LevelId;
        public readonly int Line;
        public readonly string Message;
        public readonly bool IsError;

        public LevelIssue(string pack, string levelId, int line, string message, bool isError)
        {
            Pack = pack;
            LevelId = levelId;
            Line = line;
            Message = message;
            IsError = isError;
        }

        public override string ToString()
        {
            return (IsError ? "error" : "warning") + " [" + Pack + "/" + (LevelId ?? "-") + " line " + Line + "] " + Message;
        }
    }
}
