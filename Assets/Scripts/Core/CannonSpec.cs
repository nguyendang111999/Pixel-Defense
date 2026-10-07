using System.Globalization;

namespace PixelDefense.Core
{
    /// <summary>One cannon waiting in a column: its color, ammo and whether its color is hidden until it reaches the front.</summary>
    public readonly struct CannonSpec
    {
        public readonly byte Color;
        public readonly int Ammo;
        public readonly bool Hidden;

        public CannonSpec(byte color, int ammo, bool hidden)
        {
            Color = color;
            Ammo = ammo;
            Hidden = hidden;
        }

        public override string ToString()
        {
            return (Hidden ? "?" : string.Empty) + ScaleColors.ToCode(Color) + Ammo.ToString(CultureInfo.InvariantCulture);
        }
    }
}
