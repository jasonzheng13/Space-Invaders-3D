using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only check for the hand-typed pixel-art grids in SpriteLibrary.
/// Menu: Tools > Space Invaders > Validate Sprites.
/// A single typo in a grid (a missing pixel, an unknown colour letter) would otherwise
/// only show up as a garbled sprite or an exception at runtime, so this runs every
/// definition through the same rasterizer the game uses and checks known sizes.
/// </summary>
public static class SpriteValidator
{
    [MenuItem("Tools/Space Invaders/Validate Sprites")]
    static void Validate()
    {
        List<string> failures = Run(out int count);
        if (failures.Count == 0)
        {
            Debug.Log("Sprite check passed: " + count + " sprites");
        }
        else
        {
            foreach (string f in failures) Debug.LogError("Sprite check failed: " + f);
        }
    }

    /// <summary>Returns one message per problem found; empty means everything passed.</summary>
    public static List<string> Run(out int count)
    {
        var failures = new List<string>();
        count = 0;

        // 1. Every definition must rasterize: equal-width rows, every letter in the palette.
        foreach (SpriteDef def in SpriteLibrary.All)
        {
            count++;
            try
            {
                PixelArt.Rasterize(def.Rows, def.Palette, out _, out _);
            }
            catch (Exception e)
            {
                failures.Add(def.Id + ": " + e.Message);
            }
        }

        // 2. Known sizes (width x height in pixels), taken from the approved concept canvas.
        ExpectSize(failures, SpriteLibrary.GundamBack, 28, 26);
        ExpectSize(failures, SpriteLibrary.Zaku, 20, 19);
        ExpectSize(failures, SpriteLibrary.RickDom, 20, 15);
        ExpectSize(failures, SpriteLibrary.CharZaku, 20, 20);
        if (SpriteLibrary.Explosion.Length != 6)
        {
            failures.Add("Explosion: expected 6 frames, got " + SpriteLibrary.Explosion.Length);
        }
        foreach (SpriteDef frame in SpriteLibrary.Explosion) ExpectSize(failures, frame, 8, 8);

        // 3. The helpers themselves.
        string[] patched = PixelArt.Patch(new[] { "...." }, 1, 0, new[] { "A.B" });
        if (patched[0] != ".A.B") failures.Add("PixelArt.Patch: expected \".A.B\", got \"" + patched[0] + "\"");

        bool threw = false;
        try { PixelArt.Rasterize(new[] { "A.", "..." }, new Dictionary<char, Color32> { { 'A', Color.white } }, out _, out _); }
        catch (ArgumentException) { threw = true; }
        if (!threw) failures.Add("PixelArt.Rasterize: ragged rows did not throw ArgumentException");

        return failures;
    }

    static void ExpectSize(List<string> failures, SpriteDef def, int w, int h)
    {
        int rows = def.Rows.Length;
        int cols = rows > 0 ? def.Rows[0].Length : 0;
        if (cols != w || rows != h)
        {
            failures.Add(def.Id + ": expected " + w + "x" + h + ", got " + cols + "x" + rows);
        }
    }
}
