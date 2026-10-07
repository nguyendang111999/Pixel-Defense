using System.Collections.Generic;

namespace PixelDefense.Core
{
    /// <summary>Structural rules every level must satisfy before it can be played.</summary>
    public static class LevelValidator
    {
        public const int MaxWidth = 5;
        public const int MaxWindow = 8;
        public const int MaxSlots = 7;
        public const int MaxColumns = 4;
        public const float MaxSpeed = 20f;

        /// <summary>Appends issues for <paramref name="level"/>; returns false if any are errors.</summary>
        public static bool Validate(LevelDefinition level, string pack, int line, List<LevelIssue> issues)
        {
            bool ok = true;

            void Fail(string message)
            {
                ok = false;
                issues.Add(new LevelIssue(pack, level.Id, line, message, true));
            }

            if (level.CubeCount == 0)
            {
                Fail("Dragon body is empty.");
            }

            if (level.Window < 1 || level.Window > MaxWindow)
            {
                Fail("Window must be 1-" + MaxWindow + ".");
            }

            if (level.Slots < 1 || level.Slots > MaxSlots)
            {
                Fail("Slots must be 1-" + MaxSlots + ".");
            }

            if (!(level.CrawlSpeed > 0f) || level.CrawlSpeed > MaxSpeed)
            {
                Fail("Speed must be in (0, " + MaxSpeed + "].");
            }

            if (level.Start < 0f)
            {
                Fail("Start must not be negative.");
            }

            if (level.ColumnCount == 0)
            {
                Fail("Level has no cannon columns.");
            }
            else if (level.ColumnCount > MaxColumns)
            {
                Fail("At most " + MaxColumns + " cannon columns (found " + level.ColumnCount + ").");
            }

            for (int c = 0; c < level.ColumnCount; c++)
            {
                if (level.ColumnLength(c) == 0)
                {
                    Fail("Column " + (c + 1) + " is empty.");
                }
            }

            var cubes = new int[ScaleColors.Count];
            var ammo = new int[ScaleColors.Count];
            level.CountCubesPerColor(cubes);
            level.CountAmmoPerColor(ammo);
            for (int color = 0; color < ScaleColors.Count; color++)
            {
                if (cubes[color] != ammo[color])
                {
                    Fail(ScaleColors.ToName((byte)color) + ": " + cubes[color] + " scales but " + ammo[color] + " ammo (must match).");
                }
            }

            return ok;
        }
    }
}
