using System;
using System.Collections.Generic;

namespace PixelDefense.Core
{
    public sealed class SolveResult
    {
        public bool Solved;
        public bool BudgetExceeded;
        public int Nodes;

        /// <summary>Column tapped for each deploy, in order.</summary>
        public int[] Moves = Array.Empty<int>();

        /// <summary>Cannon id each move sends (the front of <see cref="Moves"/>' column at that point).</summary>
        public int[] Cannons = Array.Empty<int>();

        /// <summary>True when always tapping a column whose color is currently exposed (else the leftmost) wins.</summary>
        public bool GreedyWins;
    }

    /// <summary>
    /// Proves a level can be won when the player deploys only after the board settles (dragon frozen in place).
    /// Explores deploy orders depth-first with memoized states; time pressure is checked separately by
    /// <see cref="BotPlayer"/>.
    /// </summary>
    public static class LevelSolver
    {
        private const float SettleSeconds = 600f;

        public static SolveResult Solve(LevelDefinition level, BattleSettings settings, int nodeBudget)
        {
            var root = new Battle(level, settings, float.MaxValue / 4f) { MovementEnabled = false };
            SolveResult result = SolveSettled(root, nodeBudget);
            result.GreedyWins = PlayGreedy(level, settings);
            return result;
        }

        /// <summary>
        /// Plans the rest of a battle in progress: settles a frozen planning copy of <paramref name="live"/> (shots
        /// land, cannons finish firing) and searches from there. Safe to call off the main thread with a copy
        /// made by <see cref="Battle.CloneForPlanning"/>.
        /// </summary>
        public static SolveResult SolveFrom(Battle live, int nodeBudget)
        {
            Battle root = live.CloneForPlanning();
            root.RunUntilQuiescent(SettleSeconds);
            return SolveSettled(root, nodeBudget);
        }

        private static SolveResult SolveSettled(Battle root, int nodeBudget)
        {
            var result = new SolveResult();
            var visited = new HashSet<ulong>();
            var search = new SearchState(root.CannonCount, root.ColumnCount);
            result.Solved = Search(root, search, visited, nodeBudget, result);
            if (result.Solved)
            {
                result.Moves = search.Moves.ToArray();
                result.Cannons = search.Cannons.ToArray();
            }
            return result;
        }

        private sealed class SearchState
        {
            public readonly List<int> Moves;
            public readonly List<int> Cannons;
            public readonly int[] Order;
            public readonly int[] Scores;

            public SearchState(int cannons, int columns)
            {
                Moves = new List<int>(cannons);
                Cannons = new List<int>(cannons);
                Order = new int[columns];
                Scores = new int[columns];
            }
        }

        private static bool Search(Battle state, SearchState search, HashSet<ulong> visited, int budget, SolveResult result)
        {
            if (state.Phase == BattlePhase.Won)
            {
                return true;
            }

            if (state.Phase != BattlePhase.Playing || !state.HasFreeSlot)
            {
                return false;
            }

            if (++result.Nodes > budget)
            {
                result.BudgetExceeded = true;
                return false;
            }

            if (!visited.Add(state.StateHash))
            {
                return false;
            }

            int count = OrderColumns(state, search.Order, search.Scores);
            var localOrder = new int[count];
            Array.Copy(search.Order, localOrder, count);

            for (int k = 0; k < localOrder.Length; k++)
            {
                int column = localOrder[k];
                int cannon = state.FrontCannon(column);
                Battle next = state.Clone();
                if (next.Deploy(column) != DeployResult.Deployed)
                {
                    continue;
                }

                next.RunUntilQuiescent(SettleSeconds);
                search.Moves.Add(column);
                search.Cannons.Add(cannon);
                if (Search(next, search, visited, budget, result))
                {
                    return true;
                }
                search.Moves.RemoveAt(search.Moves.Count - 1);
                search.Cannons.RemoveAt(search.Cannons.Count - 1);

                if (result.BudgetExceeded)
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>Columns whose front cannon can fire right now come first, larger ammo breaking ties.</summary>
        private static int OrderColumns(Battle state, int[] order, int[] scores)
        {
            int count = 0;
            for (int c = 0; c < state.ColumnCount; c++)
            {
                int cannon = state.FrontCannon(c);
                if (cannon < 0)
                {
                    continue;
                }

                CannonSpec spec = state.GetCannon(cannon);
                int score = state.Dragon.HasTarget(spec.Color) ? 100000 : 0;
                score += spec.Ammo;
                order[count] = c;
                scores[count] = score;
                count++;
            }

            for (int i = 1; i < count; i++)
            {
                int column = order[i];
                int score = scores[i];
                int j = i - 1;
                while (j >= 0 && scores[j] < score)
                {
                    order[j + 1] = order[j];
                    scores[j + 1] = scores[j];
                    j--;
                }
                order[j + 1] = column;
                scores[j + 1] = score;
            }
            return count;
        }

        private static bool PlayGreedy(LevelDefinition level, BattleSettings settings)
        {
            var battle = new Battle(level, settings, float.MaxValue / 4f) { MovementEnabled = false };
            for (int guard = 0; guard < level.CannonCount + 1 && battle.Phase == BattlePhase.Playing; guard++)
            {
                if (!battle.HasFreeSlot)
                {
                    return false;
                }

                int pick = -1;
                for (int c = 0; c < battle.ColumnCount; c++)
                {
                    int cannon = battle.FrontCannon(c);
                    if (cannon < 0)
                    {
                        continue;
                    }

                    if (pick < 0)
                    {
                        pick = c;
                    }

                    if (battle.Dragon.HasTarget(battle.GetCannon(cannon).Color))
                    {
                        pick = c;
                        break;
                    }
                }

                if (pick < 0)
                {
                    return false;
                }

                battle.Deploy(pick);
                battle.RunUntilQuiescent(SettleSeconds);
            }
            return battle.Phase == BattlePhase.Won;
        }
    }
}
