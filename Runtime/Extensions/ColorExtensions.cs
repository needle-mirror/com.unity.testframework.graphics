namespace UnityEngine.TestTools.Graphics
{
    static class ColorExtensions
    {
        internal static Color Darken(this Color col, float brightness = 0.1f, float contrast = 0.4f)
        {
            Color.RGBToHSV(col, out var h, out var s, out var v);
            s *= contrast;
            v *= brightness;

            var dark = Color.HSVToRGB(h, s, v);
            dark.a = col.a;
            return dark;
        }
    }
}
