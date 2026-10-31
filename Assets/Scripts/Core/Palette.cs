using UnityEngine;

namespace RocketToTheMoon.Core
{
    /// <summary>Project colour palette (see Docs/Style.md).</summary>
    public static class Palette
    {
        /* I choose the colors from palette at https://coolors.co/012840-011826-266573-4e96a6-f2dcb3 */
        public static readonly Color DeepNavy = Hex("012840");
        public static readonly Color Midnight = Hex("011826");
        public static readonly Color Teal = Hex("266573");
        public static readonly Color LightTeal = Hex("4E96A6");
        public static readonly Color Moonlight = Hex("F2DCB3");

        // Turns a hex text like "F2DCB3" into a color.
        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }
    }
}
