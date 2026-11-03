using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Every pixel-art image in the game, copied from the approved concept canvas.
/// Each letter in a grid is a colour from that sprite's palette; '.' is transparent.
/// Run Tools > Space Invaders > Validate Sprites after editing anything here.
/// </summary>
public static class SpriteLibrary
{
    static Dictionary<char, Color32> Pal(params string[] pairs)
    {
        // pairs alternate: "Y", "#F2C230", "W", "#E9E6EF", ...
        var p = new Dictionary<char, Color32>();
        for (int i = 0; i < pairs.Length; i += 2) p[pairs[i][0]] = PixelArt.Hex(pairs[i + 1]);
        return p;
    }

    static string[] Join(string[] a, string[] b)
    {
        var r = new string[a.Length + b.Length];
        a.CopyTo(r, 0);
        b.CopyTo(r, a.Length);
        return r;
    }

    // ---------------- RX-78-2 Gundam ----------------

    static readonly Dictionary<char, Color32> GundamPalette = Pal(
        "Y", "#F2C230", "W", "#E9E6EF", "w", "#A9A6C0", "R", "#D9463B", "r", "#8E2F2A",
        "B", "#3E5FB8", "K", "#1A1730", "E", "#2A2540", "G", "#5A5870", "g", "#3A3850",
        "O", "#F4A340", "H", "#C9C6D9");   // O = orange rim light from the sunrise; E = visor (off)

    static readonly string[] GundamLegs =
    {
        ".........WWWW..WWWW.........",
        ".........WWWW..WWWW.........",
        ".........wWWw..wWWw.........",
        ".........WWWW..WWWW.........",
        "........WWWWW..WWWWW........",
        "........WWWWw..wWWWW........",
        "........wWWWw..wWWWw........",
        "........WWWWW..WWWWW........",
        "........RRRRR..RRRRR........",
        ".......rRRRRr..rRRRRr......."
    };

    /// <summary>Seen from behind, as the player sees it: backpack, beam saber hilts, rifle raised.</summary>
    public static readonly SpriteDef GundamBack = new SpriteDef
    {
        Id = "GundamBack",
        PixelsPerUnit = 14,
        Palette = GundamPalette,
        Rows = PixelArt.Patch(PixelArt.Patch(Join(new[]
        {
            "........Y..........Y........",
            ".........Y........Y.........",
            "......H...YWWWWWWY...H......",
            "......H....WWWWWW....H......",
            ".......H...wWWWWw...H.......",
            "....WWWWHWWwwwwwwWWHWWWW....",
            "...WWWWWWGGGGGGGGGGWWWWWWO..",
            "...WWWWWWGggggggggGWWWWWWO..",
            "...wWWWWwGgBBBBBBgGwWWWWwO..",
            "...wwWWwBGgBBBBBBgGBwWWwwO..",
            "....WWWW.GGGGGGGGGG.WWWW....",
            "....WWWW..KGGKKGGK..WWWW....",
            "....wWWw..KggKKggK..wWWw....",
            "....WWWW...RRRRRR...WWWW....",
            "....GGGG..WWWYYWWW..GGGG....",
            "..........BWWWWWWB.........."
        }, GundamLegs),
            24, 3, new[] { ".G", ".G", ".G", "GG", "GG", "Gg", "Gg", "GG", "GG", "GK", "G." }),           // beam rifle
            1, 8, new[] { "RRRRRR", "RggggR", "RgGGgR", "RgGGgR", "RggggR", "RggggR", "RggggR", "rRggRr", ".rRRr.", "..rr.." })  // shield back
    };

    /// <summary>Facing the camera, for the intro. The chest hatch is at pixels (12,10) to (15,12).</summary>
    public static readonly SpriteDef GundamFront = new SpriteDef
    {
        Id = "GundamFront",
        PixelsPerUnit = 14,
        Palette = GundamPalette,
        Rows = PixelArt.Patch(PixelArt.Patch(Join(new[]
        {
            "........Y..........Y........",
            ".........Y...RR...Y.........",
            "..........YYWRRWYY..........",
            "...........WWWWWW...........",
            "...........WEKKEW...........",
            "...........wWRRWO...........",
            "....WWWWWW..wRRw..WWWWWWO...",
            "...WWWWWWWBBBBBBBBWWWWWWWO..",
            "...wWWWWWBYYBBBBYYBWWWWWWO..",
            "...wwWWWWBYYBBBBYYBWWWWWwO..",
            "....WWWW.BBBBKKBBBB.WWWWO...",
            "....WWWW.BBBBKKBBBB.WWWWO...",
            "....wWWw.RRRRRRRRRR.wWWwO...",
            "....WWWW..rRRRRRRr..WWWWO...",
            "....GGGG..WWWYYWWW..GGGG....",
            "..........BWWWWWWB.........."
        }, GundamLegs),
            2, 12, new[] { "GG", "GG", "Gg", "Gg", "GG", ".G", ".G", ".G", ".G" }),                       // rifle lowered
            21, 8, new[] { "RRRRRR", "RWWWWR", "RWYYWR", "RYYYYR", "RWYYWR", "RWWWWR", "RWWWWR", "RWWWWR", "rRWWRr", ".rRRr.", "..rr.." })  // shield face
    };

    public static readonly SpriteDef Pilot = new SpriteDef
    {
        Id = "Pilot",
        PixelsPerUnit = 16,
        Palette = Pal("h", "#6B3E2E", "k", "#E8B894", "W", "#F3ECE2", "B", "#3E5FB8", "b", "#2A3F80"),
        Rows = new[] { ".hhh.", ".hkh.", "..k..", ".WWW.", "WWWWW", ".WBW.", ".W.W.", ".b.b." }
    };

    // ---------------- Zeon ----------------

    // The visor is left solid black (K); the pink mono-eye is a separate moving part (see PixelSkin).
    static readonly string[] ZakuRows =
    {
        ".......GGGGGG.......",
        "......GGGGGGGG......",
        "......GKKKKKKG......",
        "......GKKKKKKG......",
        ".......gGTTGg.......",
        "..SS..TGGGGGGT.gggg.",
        ".SGGS.TgGGGGgT.gGGGg",
        ".SGGGSGGGGGGGGGgGGGg",
        "..GGG.gGGGGGGg.gGGg.",
        "..gGg.TgGGGGgT..GG..",
        "..MMM..gggggg...GG..",
        ".MMMMM.dGGGGd...gg..",
        "..M.M..GG..GG.......",
        "....M..GG..GG.......",
        "....M..GG..GG.......",
        "......gGG..GGg......",
        "......GGG..GGG......",
        ".....gGGG..GGGg.....",
        ".....dddd..dddd....."
    };

    public static readonly SpriteDef Zaku = new SpriteDef
    {
        Id = "Zaku",
        PixelsPerUnit = 12,
        Palette = Pal("G", "#5E8C4A", "g", "#3F6633", "d", "#2C4A24", "K", "#141225",
                      "T", "#8A8F9C", "S", "#C9C6D9", "M", "#5A5870"),
        Rows = ZakuRows
    };

    /// <summary>Char Aznable's commander Zaku: red, with the commander's horn on top.</summary>
    public static readonly SpriteDef CharZaku = new SpriteDef
    {
        Id = "CharZaku",
        PixelsPerUnit = 12,
        Palette = Pal("G", "#C8463D", "g", "#8E2F2A", "d", "#5E1F1C", "K", "#141225",
                      "T", "#E8A09A", "S", "#F3C7C2", "M", "#5A5870", "H", "#F08A80"),
        Rows = Join(new[] { ".........HH........." }, ZakuRows)
    };

    public static readonly SpriteDef RickDom = new SpriteDef
    {
        Id = "RickDom",
        PixelsPerUnit = 12,
        Palette = Pal("D", "#6B5A9E", "d", "#463A70", "x", "#2E2550", "K", "#141225", "Z", "#5A5870"),
        Rows = PixelArt.Patch(new[]
        {
            "........DDDD........",
            "......DDDDDDDD......",
            ".....DKKKKKKKKD.....",
            ".....DDDDKKDDDD.....",
            "...dDDDDDDDDDDDDd...",
            "..dDDDDDDDDDDDDDDd..",
            "..dDDdDDDDDDDDdDDd..",
            "...dDDDDDDDDDDDDd...",
            "....DDDDDDDDDDDD....",
            "...xDDDDDDDDDDDDx...",
            "..xxDDDDDDDDDDDDxx..",
            "..xDDDDDDDDDDDDDDx..",
            ".xDDDDDDDDDDDDDDDDx.",
            ".xxxxxxxxxxxxxxxxxx.",
            "...KKK........KKK..."
        }, 1, 3, new[] { "ZZZZ", "ZKKZ", "ZKKZ", "ZZZZ", ".ZZ." })   // giant bazooka on the shoulder
    };

    // ---------------- Support ----------------

    public static readonly SpriteDef Haro = new SpriteDef
    {
        Id = "Haro",
        PixelsPerUnit = 8,
        Palette = Pal("G", "#7FD86B", "g", "#4E9A40", "R", "#D9463B", "k", "#2E5A28"),
        Rows = new[] { "..gGGGGg..", ".GGGGGGGG.", "GGGGGGGGGG", "GGRGGGGRGG", "GGGGGGGGGG", "gGGkkkkGGg", ".gGGGGGGg.", "..gggggg.." }
    };

    public static readonly SpriteDef BeamShot = new SpriteDef
    {
        Id = "BeamShot",
        PixelsPerUnit = 16,
        Palette = Pal("C", "#FFD1E6", "P", "#FF6FAE"),
        Rows = new[] { ".C.", "PCP", "PCP", "PCP", "PCP", "PCP", "PCP", "PCP", "PCP", ".P." }
    };

    public static readonly SpriteDef ZakuTracer = new SpriteDef
    {
        Id = "ZakuTracer",
        PixelsPerUnit = 16,
        Palette = Pal("T", "#F2C230"),
        Rows = new[] { "TT", "TT", "TT", "TT", "TT", "TT" }
    };

    // ---------------- Explosion: 6 frames, 100 ms each ----------------

    static readonly Dictionary<char, Color32> BoomPalette = Pal(
        "W", "#FFFFFF", "Y", "#F2C230", "O", "#F4A340", "R", "#D9463B", "s", "#6E6A8A", "g", "#3A3557");

    static SpriteDef Boom(int n, string[] rows)
    {
        return new SpriteDef { Id = "Explosion" + n, PixelsPerUnit = 6, Palette = BoomPalette, Rows = rows };
    }

    public static readonly SpriteDef[] Explosion =
    {
        Boom(1, new[] { "........", "........", "...WW...", "..WWWW..", "..WWWW..", "...WW...", "........", "........" }),
        Boom(2, new[] { "...YY...", "...YY...", ".YYWWYY.", "YYWWWWYY", "YYWWWWYY", ".YYWWYY.", "...YY...", "...YY..." }),
        Boom(3, new[] { "..OOOO..", ".OYYYYO.", "OYY..YYO", "OY....YO", "OY....YO", "OYY..YYO", ".OYYYYO.", "..OOOO.." }),
        Boom(4, new[] { ".O....O.", "O.R..R.O", "..R..R..", "R......R", "R......R", "..R..R..", "O.R..R.O", ".O....O." }),
        Boom(5, new[] { "g......g", "...s....", ".s....s.", "........", "....s...", ".s....s.", "...s....", "g......g" }),
        Boom(6, new[] { "........", "...s....", "........", ".s....s.", "........", "....s...", "........", "........" })
    };

    public static IEnumerable<SpriteDef> All
    {
        get
        {
            yield return GundamBack;
            yield return GundamFront;
            yield return Pilot;
            yield return Zaku;
            yield return CharZaku;
            yield return RickDom;
            yield return Haro;
            yield return BeamShot;
            yield return ZakuTracer;
            foreach (SpriteDef frame in Explosion) yield return frame;
        }
    }
}
