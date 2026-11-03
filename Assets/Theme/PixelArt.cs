using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One pixel-art image written as text: each string is a row of pixels (row 0 = top),
/// each character picks a colour from Palette, and '.' is transparent.
/// </summary>
public sealed class SpriteDef
{
    public string Id;
    public string[] Rows;
    public Dictionary<char, Color32> Palette;
    public float PixelsPerUnit;   // how many pixels make one world unit
}

/// <summary>
/// Turns SpriteDefs into Unity Sprites at runtime, so the game needs no image files.
/// Every result is cached: 30 aliens share one texture instead of building 30.
/// </summary>
public static class PixelArt
{
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    /// <summary>"#RRGGBB" or "#RRGGBBAA" to Color32.</summary>
    public static Color32 Hex(string hex)
    {
        if (!ColorUtility.TryParseHtmlString(hex, out Color c))
        {
            throw new ArgumentException("Bad colour: " + hex);
        }
        return c;
    }

    /// <summary>
    /// Returns a copy of rows with patch drawn on top at (x, y). '.' in the patch leaves
    /// the pixel underneath alone, so a rifle or shield can be layered onto a body.
    /// The result grows if the patch reaches past the right edge of a row.
    /// </summary>
    public static string[] Patch(string[] rows, int x, int y, string[] patch)
    {
        string[] result = (string[])rows.Clone();
        for (int dy = 0; dy < patch.Length; dy++)
        {
            int row = y + dy;
            if (row < 0 || row >= result.Length) continue;
            char[] line = result[row].PadRight(x + patch[dy].Length, '.').ToCharArray();
            for (int dx = 0; dx < patch[dy].Length; dx++)
            {
                if (patch[dy][dx] != '.') line[x + dx] = patch[dy][dx];
            }
            result[row] = new string(line);
        }
        return result;
    }

    /// <summary>
    /// Converts a grid to pixels in Texture2D order (bottom row first).
    /// Throws ArgumentException for ragged rows or a letter missing from the palette,
    /// which is how the validator catches typos.
    /// </summary>
    public static Color32[] Rasterize(string[] rows, IReadOnlyDictionary<char, Color32> palette,
                                      out int width, out int height)
    {
        if (rows == null || rows.Length == 0) throw new ArgumentException("Sprite has no rows");
        height = rows.Length;
        width = rows[0].Length;
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            string row = rows[y];
            if (row.Length != width)
            {
                throw new ArgumentException("Row " + y + " is " + row.Length + " wide, expected " + width);
            }
            int textureRow = height - 1 - y;   // textures count rows from the bottom
            for (int x = 0; x < width; x++)
            {
                char ch = row[x];
                Color32 c;
                if (ch == '.') c = new Color32(0, 0, 0, 0);
                else if (!palette.TryGetValue(ch, out c))
                {
                    throw new ArgumentException("Unknown colour '" + ch + "' at row " + y + ", column " + x);
                }
                pixels[textureRow * width + x] = c;
            }
        }
        return pixels;
    }

    public static Sprite GetSprite(SpriteDef def)
    {
        return Cached("sprite:" + def.Id, () => Build(def.Rows, def.Palette, def.PixelsPerUnit));
    }

    /// <summary>Same shape, every opaque pixel white. Swapped in for a hit flash.</summary>
    public static Sprite GetSilhouette(SpriteDef def)
    {
        return Cached("silhouette:" + def.Id, () =>
        {
            var white = new Dictionary<char, Color32>();
            foreach (char key in def.Palette.Keys) white[key] = new Color32(255, 255, 255, 255);
            return Build(def.Rows, white, def.PixelsPerUnit);
        });
    }

    /// <summary>A single pixel of one colour, one world unit across. Scale it to make rectangles.</summary>
    public static Sprite Solid(Color32 c)
    {
        return Cached("solid:" + c.r + "," + c.g + "," + c.b + "," + c.a, () =>
            Build(new[] { "X" }, new Dictionary<char, Color32> { { 'X', c } }, 1f));
    }

    /// <summary>A filled pixel circle, for planets and glows.</summary>
    public static Sprite Circle(int diameterPx, Color32 c, float ppu)
    {
        return Cached("circle:" + diameterPx + ":" + c.r + "," + c.g + "," + c.b + "," + c.a + ":" + ppu, () =>
        {
            var rows = new string[diameterPx];
            float r = diameterPx / 2f;
            for (int y = 0; y < diameterPx; y++)
            {
                var line = new char[diameterPx];
                for (int x = 0; x < diameterPx; x++)
                {
                    float dx = x + 0.5f - r, dy = y + 0.5f - r;
                    line[x] = dx * dx + dy * dy <= r * r ? 'X' : '.';
                }
                rows[y] = new string(line);
            }
            return Build(rows, new Dictionary<char, Color32> { { 'X', c } }, ppu);
        });
    }

    static Sprite Cached(string key, Func<Sprite> make)
    {
        // A sprite whose texture was destroyed compares equal to null in Unity, so rebuild it.
        if (cache.TryGetValue(key, out Sprite s) && s != null) return s;
        s = make();
        cache[key] = s;
        return s;
    }

    static Sprite Build(string[] rows, IReadOnlyDictionary<char, Color32> palette, float ppu)
    {
        Color32[] pixels = Rasterize(rows, palette, out int w, out int h);
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,     // keep pixels crisp instead of blurring them
            wrapMode = TextureWrapMode.Clamp
        };
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);
    }
}
