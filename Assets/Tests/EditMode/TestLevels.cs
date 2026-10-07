using NUnit.Framework;
using PixelDefense.Core;

namespace PixelDefense.Tests.EditMode
{
    /// <summary>Builds single test levels from compact text through the real parser.</summary>
    internal static class TestLevels
    {
        public static LevelDefinition Make(string body)
        {
            LevelPack pack = LevelParser.Parse("version 1\nlevel T\n" + body + "\nend\n", "test");
            Assert.That(pack.HasErrors, Is.False, string.Join("\n", pack.Issues));
            Assert.That(pack.Levels.Count, Is.EqualTo(1));
            return pack.Levels[0];
        }

        public static BattleSettings Settings()
        {
            return new BattleSettings();
        }
    }
}
