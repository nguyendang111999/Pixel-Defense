using System;
using System.Collections.Generic;
using System.Globalization;

namespace PixelDefense.Core
{
    /// <summary>
    /// Parses the plain-text level pack format:
    /// <code>
    /// version 1
    /// level L01
    ///   speed 1.2
    ///   body O*10 OYO*4 G*6     # head to tail: pattern*slices, pattern = 1 color or `width` colors
    ///   col O30 ?G18           # one line per column, front first; '?' hides the color until it reaches the front
    /// end
    /// </code>
    /// Tolerates a UTF-8 BOM, CRLF, blank lines and '#' comments. Invalid levels are skipped and reported.
    /// </summary>
    public static class LevelParser
    {
        private static readonly char[] Whitespace = { ' ', '\t' };

        public static LevelPack Parse(string text, string packName)
        {
            var levels = new List<LevelDefinition>();
            var issues = new List<LevelIssue>();
            int version = 0;

            if (text == null)
            {
                issues.Add(new LevelIssue(packName, null, 0, "Level pack text is null.", true));
                return new LevelPack(packName, version, levels, issues);
            }

            if (text.Length > 0 && text[0] == '﻿')
            {
                text = text.Substring(1);
            }

            string[] lines = text.Split('\n');
            Builder current = null;
            var seenIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < lines.Length; i++)
            {
                int lineNumber = i + 1;
                string line = lines[i];
                int comment = line.IndexOf('#');
                if (comment >= 0)
                {
                    line = line.Substring(0, comment);
                }

                string[] tokens = line.Trim().Split(Whitespace, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0)
                {
                    continue;
                }

                string key = tokens[0].ToLowerInvariant();

                if (current == null)
                {
                    switch (key)
                    {
                        case "version":
                            if (tokens.Length < 2 || !int.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out version))
                            {
                                issues.Add(new LevelIssue(packName, null, lineNumber, "Invalid version line.", true));
                            }
                            else if (version > LevelPack.SupportedVersion)
                            {
                                issues.Add(new LevelIssue(packName, null, lineNumber,
                                    "Pack version " + version + " is newer than supported version " + LevelPack.SupportedVersion + ".", true));
                                return new LevelPack(packName, version, levels, issues);
                            }
                            break;
                        case "pack":
                            break;
                        case "level":
                            if (tokens.Length < 2)
                            {
                                issues.Add(new LevelIssue(packName, null, lineNumber, "Level line needs an id.", true));
                                current = new Builder(null, lineNumber);
                            }
                            else
                            {
                                current = new Builder(tokens[1], lineNumber);
                            }
                            break;
                        default:
                            issues.Add(new LevelIssue(packName, null, lineNumber, "Unexpected '" + tokens[0] + "' outside a level.", false));
                            break;
                    }
                    continue;
                }

                if (key == "end")
                {
                    Finish(current, packName, levels, issues, seenIds);
                    current = null;
                    continue;
                }

                if (key == "level")
                {
                    issues.Add(new LevelIssue(packName, current.Id, lineNumber, "Missing 'end' before next level.", false));
                    Finish(current, packName, levels, issues, seenIds);
                    current = new Builder(tokens.Length > 1 ? tokens[1] : null, lineNumber);
                    continue;
                }

                ParseLevelLine(current, key, tokens, lineNumber, packName, issues);
            }

            if (current != null)
            {
                issues.Add(new LevelIssue(packName, current.Id, lines.Length, "Missing 'end' at end of file.", false));
                Finish(current, packName, levels, issues, seenIds);
            }

            if (version == 0)
            {
                issues.Add(new LevelIssue(packName, null, 1, "Missing 'version' line; assuming 1.", false));
                version = 1;
            }

            return new LevelPack(packName, version, levels, issues);
        }

        private static void ParseLevelLine(Builder level, string key, string[] tokens, int line, string pack, List<LevelIssue> issues)
        {
            switch (key)
            {
                case "name":
                    level.Name = string.Join(" ", tokens, 1, tokens.Length - 1);
                    break;
                case "track":
                    level.Track = RequireWord(tokens, level, line, pack, issues) ?? level.Track;
                    break;
                case "skin":
                    level.Skin = RequireWord(tokens, level, line, pack, issues) ?? level.Skin;
                    break;
                case "tutorial":
                    level.Tutorial = RequireWord(tokens, level, line, pack, issues);
                    break;
                case "speed":
                    level.Speed = ParseFloat(tokens, level.Speed, level, line, pack, issues);
                    break;
                case "start":
                    level.Start = ParseFloat(tokens, level.Start, level, line, pack, issues);
                    break;
                case "width":
                    level.Width = ParseInt(tokens, level.Width, level, line, pack, issues);
                    break;
                case "window":
                    level.Window = ParseInt(tokens, level.Window, level, line, pack, issues);
                    break;
                case "slots":
                    level.Slots = ParseInt(tokens, level.Slots, level, line, pack, issues);
                    break;
                case "body":
                    for (int t = 1; t < tokens.Length; t++)
                    {
                        level.BodyTokens.Add(new Token(tokens[t], line));
                    }
                    break;
                case "col":
                    var column = new List<CannonSpec>();
                    for (int t = 1; t < tokens.Length; t++)
                    {
                        if (TryParseCannon(tokens[t], out CannonSpec cannon))
                        {
                            column.Add(cannon);
                        }
                        else
                        {
                            level.Error(issues, pack, line, "Bad cannon token '" + tokens[t] + "' (expected e.g. O30 or ?O30).");
                        }
                    }
                    level.Columns.Add(column);
                    break;
                default:
                    issues.Add(new LevelIssue(pack, level.Id, line, "Unknown key '" + tokens[0] + "'.", false));
                    break;
            }
        }

        public static bool TryParseCannon(string token, out CannonSpec cannon)
        {
            cannon = default;
            int index = 0;
            bool hidden = false;
            if (token.Length > 0 && token[0] == '?')
            {
                hidden = true;
                index = 1;
            }

            if (token.Length < index + 2 || !ScaleColors.TryParse(token[index], out byte color))
            {
                return false;
            }

            if (!int.TryParse(token.Substring(index + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int ammo) || ammo <= 0)
            {
                return false;
            }

            cannon = new CannonSpec(color, ammo, hidden);
            return true;
        }

        private static void Finish(Builder level, string pack, List<LevelDefinition> levels, List<LevelIssue> issues, HashSet<string> seenIds)
        {
            if (level.Id == null)
            {
                return;
            }

            if (!seenIds.Add(level.Id))
            {
                issues.Add(new LevelIssue(pack, level.Id, level.Line, "Duplicate level id.", true));
                return;
            }

            byte[] body = BuildBody(level, pack, issues);
            var columns = new CannonSpec[level.Columns.Count][];
            for (int c = 0; c < columns.Length; c++)
            {
                columns[c] = level.Columns[c].ToArray();
            }

            if (level.HasErrors || body == null)
            {
                return;
            }

            var definition = new LevelDefinition(
                level.Id, level.Name ?? level.Id, level.Track, level.Skin, level.Speed, level.Width, level.Window,
                level.Slots, level.Start, level.Tutorial, body, columns);

            if (LevelValidator.Validate(definition, pack, level.Line, issues))
            {
                levels.Add(definition);
            }
        }

        private static byte[] BuildBody(Builder level, string pack, List<LevelIssue> issues)
        {
            if (level.Width < 1 || level.Width > LevelValidator.MaxWidth)
            {
                level.Error(issues, pack, level.Line, "Width must be 1-" + LevelValidator.MaxWidth + ".");
                return null;
            }

            var cubes = new List<byte>();
            var pattern = new byte[level.Width];
            for (int t = 0; t < level.BodyTokens.Count; t++)
            {
                Token token = level.BodyTokens[t];
                string text = token.Text;
                int star = text.IndexOf('*');
                string patternText = star >= 0 ? text.Substring(0, star) : text;
                int count = 1;
                if (star >= 0 && !int.TryParse(text.Substring(star + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out count))
                {
                    level.Error(issues, pack, token.Line, "Bad slice count in '" + text + "'.");
                    return null;
                }

                if (count <= 0 || (patternText.Length != 1 && patternText.Length != level.Width))
                {
                    level.Error(issues, pack, token.Line, "Pattern '" + patternText + "' must have 1 or " + level.Width + " colors and a positive count.");
                    return null;
                }

                for (int lane = 0; lane < level.Width; lane++)
                {
                    char code = patternText[patternText.Length == 1 ? 0 : lane];
                    if (!ScaleColors.TryParse(code, out pattern[lane]))
                    {
                        level.Error(issues, pack, token.Line, "Unknown color '" + code + "'.");
                        return null;
                    }
                }

                for (int s = 0; s < count; s++)
                {
                    cubes.AddRange(pattern);
                }
            }

            return cubes.ToArray();
        }

        private static string RequireWord(string[] tokens, Builder level, int line, string pack, List<LevelIssue> issues)
        {
            if (tokens.Length >= 2)
            {
                return tokens[1];
            }
            level.Error(issues, pack, line, "'" + tokens[0] + "' needs a value.");
            return null;
        }

        private static float ParseFloat(string[] tokens, float fallback, Builder level, int line, string pack, List<LevelIssue> issues)
        {
            if (tokens.Length >= 2 && float.TryParse(tokens[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                return value;
            }
            level.Error(issues, pack, line, "'" + tokens[0] + "' needs a number.");
            return fallback;
        }

        private static int ParseInt(string[] tokens, int fallback, Builder level, int line, string pack, List<LevelIssue> issues)
        {
            if (tokens.Length >= 2 && int.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                return value;
            }
            level.Error(issues, pack, line, "'" + tokens[0] + "' needs an integer.");
            return fallback;
        }

        private readonly struct Token
        {
            public readonly string Text;
            public readonly int Line;

            public Token(string text, int line)
            {
                Text = text;
                Line = line;
            }
        }

        private sealed class Builder
        {
            public readonly string Id;
            public readonly int Line;
            public readonly List<Token> BodyTokens = new List<Token>();
            public readonly List<List<CannonSpec>> Columns = new List<List<CannonSpec>>();
            public string Name;
            public string Track = LevelDefinition.DefaultTrack;
            public string Skin = LevelDefinition.DefaultSkin;
            public string Tutorial;
            public float Speed = LevelDefinition.DefaultSpeed;
            public float Start = LevelDefinition.DefaultStart;
            public int Width = LevelDefinition.DefaultWidth;
            public int Window = LevelDefinition.DefaultWindow;
            public int Slots = LevelDefinition.DefaultSlots;
            public bool HasErrors;

            public Builder(string id, int line)
            {
                Id = id;
                Line = line;
            }

            public void Error(List<LevelIssue> issues, string pack, int line, string message)
            {
                HasErrors = true;
                issues.Add(new LevelIssue(pack, Id, line, message, true));
            }
        }
    }
}
