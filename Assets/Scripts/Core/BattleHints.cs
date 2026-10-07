namespace PixelDefense.Core
{
    /// <summary>Move suggestions shared by the in-game hint and the autoplay fallback.</summary>
    public static class BattleHints
    {
        /// <summary>Column whose front cannon can fire right away, preferring the most exposed matching scales; -1 if none.</summary>
        public static int BestColumn(Battle battle)
        {
            int best = -1;
            int bestScore = 0;
            DragonBody body = battle.Dragon;
            for (int c = 0; c < battle.ColumnCount; c++)
            {
                int id = battle.FrontCannon(c);
                if (id < 0)
                {
                    continue;
                }

                byte color = battle.GetCannon(id).Color;
                int score = 0;
                for (int w = 0; w < body.WindowCount; w++)
                {
                    int slice = body.WindowSlice(w);
                    for (int lane = 0; lane < body.Width; lane++)
                    {
                        if (body.IsAlive(slice, lane) && body.ColorAt(slice, lane) == color)
                        {
                            score++;
                        }
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }
            return best;
        }

        /// <summary>Leftmost column that still has cannons; -1 when every column is empty.</summary>
        public static int FirstNonEmptyColumn(Battle battle)
        {
            for (int c = 0; c < battle.ColumnCount; c++)
            {
                if (battle.FrontCannon(c) >= 0)
                {
                    return c;
                }
            }
            return -1;
        }
    }
}
