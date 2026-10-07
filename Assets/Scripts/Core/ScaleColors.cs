namespace PixelDefense.Core
{
    /// <summary>Color identities shared by dragon scales and cannons. Letters are the level-file codes.</summary>
    public static class ScaleColors
    {
        public const int Count = 12;

        private const string Codes = "ROYGLCBPKWND";

        private static readonly string[] Names =
        {
            "Red", "Orange", "Yellow", "Green", "Lime", "Cyan",
            "Blue", "Purple", "Pink", "White", "Brown", "Dark"
        };

        public static bool TryParse(char code, out byte color)
        {
            int index = Codes.IndexOf(char.ToUpperInvariant(code));
            color = (byte)(index < 0 ? 0 : index);
            return index >= 0;
        }

        public static char ToCode(byte color)
        {
            return color < Count ? Codes[color] : '?';
        }

        public static string ToName(byte color)
        {
            return color < Count ? Names[color] : "Unknown";
        }
    }
}
