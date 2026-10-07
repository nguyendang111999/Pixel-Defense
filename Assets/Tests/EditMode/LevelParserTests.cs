using NUnit.Framework;
using PixelDefense.Core;

namespace PixelDefense.Tests.EditMode
{
    public class LevelParserTests
    {
        [Test]
        public void Parse_ValidLevel_BuildsBodyAndColumns()
        {
            LevelDefinition level = TestLevels.Make("width 2\nwindow 1\nspeed 1.5\nbody R*2 G*1\ncol R4 G2");

            Assert.That(level.Width, Is.EqualTo(2));
            Assert.That(level.Window, Is.EqualTo(1));
            Assert.That(level.CrawlSpeed, Is.EqualTo(1.5f));
            Assert.That(level.SliceCount, Is.EqualTo(3));
            Assert.That(level.BodyColor(0, 0), Is.EqualTo(Color('R')));
            Assert.That(level.BodyColor(2, 1), Is.EqualTo(Color('G')));
            Assert.That(level.ColumnCount, Is.EqualTo(1));
            Assert.That(level.Cannon(0, 1).Ammo, Is.EqualTo(2));
        }

        [Test]
        public void Parse_PatternToken_ExpandsAcrossLanes()
        {
            LevelDefinition level = TestLevels.Make("width 3\nbody OYO*2\ncol O4 Y2");

            Assert.That(level.SliceCount, Is.EqualTo(2));
            Assert.That(level.BodyColor(1, 0), Is.EqualTo(Color('O')));
            Assert.That(level.BodyColor(1, 1), Is.EqualTo(Color('Y')));
            Assert.That(level.BodyColor(1, 2), Is.EqualTo(Color('O')));
        }

        [Test]
        public void Parse_AmmoMismatch_ReportsErrorAndSkipsLevel()
        {
            LevelPack pack = LevelParser.Parse("version 1\nlevel A\nwidth 1\nbody R*3\ncol R2\nend", "test");

            Assert.That(pack.Levels.Count, Is.EqualTo(0));
            Assert.That(pack.HasErrors, Is.True);
            Assert.That(pack.Issues[0].LevelId, Is.EqualTo("A"));
        }

        [Test]
        public void Parse_BomCrlfAndComments_AreTolerated()
        {
            string text = "﻿# pack header\r\nversion 1\r\n\r\nlevel B # trailing\r\nwidth 1\r\nbody R*2\r\ncol R2\r\nend\r\n";
            LevelPack pack = LevelParser.Parse(text, "test");

            Assert.That(pack.HasErrors, Is.False, string.Join("\n", pack.Issues));
            Assert.That(pack.Levels.Count, Is.EqualTo(1));
            Assert.That(pack.Levels[0].Id, Is.EqualTo("B"));
        }

        [Test]
        public void Parse_HiddenCannon_SetsHiddenFlag()
        {
            LevelDefinition level = TestLevels.Make("width 1\nbody R*2 G*1\ncol R2 ?G1");

            Assert.That(level.Cannon(0, 0).Hidden, Is.False);
            Assert.That(level.Cannon(0, 1).Hidden, Is.True);
            Assert.That(level.Cannon(0, 1).Color, Is.EqualTo(Color('G')));
        }

        [Test]
        public void Parse_DuplicateId_SkipsSecondLevel()
        {
            string level = "level D\nwidth 1\nbody R*1\ncol R1\nend\n";
            LevelPack pack = LevelParser.Parse("version 1\n" + level + level, "test");

            Assert.That(pack.Levels.Count, Is.EqualTo(1));
            Assert.That(pack.HasErrors, Is.True);
        }

        [Test]
        public void Parse_NewerPackVersion_IsRejected()
        {
            LevelPack pack = LevelParser.Parse("version 99\nlevel E\nwidth 1\nbody R*1\ncol R1\nend", "test");

            Assert.That(pack.Levels.Count, Is.EqualTo(0));
            Assert.That(pack.HasErrors, Is.True);
        }

        [Test]
        public void Parse_MoreThanFourColumns_ReportsErrorAndSkipsLevel()
        {
            LevelPack pack = LevelParser.Parse("version 1\nlevel G\nwidth 1\nbody R*5\ncol R1\ncol R1\ncol R1\ncol R1\ncol R1\nend", "test");

            Assert.That(pack.HasErrors, Is.True);
            Assert.That(pack.Levels.Count, Is.EqualTo(0));
        }

        [Test]
        public void Parse_BadPatternWidth_ReportsError()
        {
            LevelPack pack = LevelParser.Parse("version 1\nlevel F\nwidth 3\nbody RG*2\ncol R2 G2\nend", "test");

            Assert.That(pack.Levels.Count, Is.EqualTo(0));
            Assert.That(pack.HasErrors, Is.True);
        }

        private static byte Color(char code)
        {
            ScaleColors.TryParse(code, out byte color);
            return color;
        }
    }
}
