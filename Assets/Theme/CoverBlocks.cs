using UnityEngine;

/// <summary>Marks one destructible cell of a cover block (Alien.cs checks for it).</summary>
public class CoverCell : MonoBehaviour { }

/// <summary>
/// Classic Space Invaders bunkers, styled as Federation armor plating. Each block is a grid
/// of small cells, and each cell is its own collider so damage chips away one cell at a time.
///
/// Cells sit on the Spaceship physics layer, which gives the right behaviour with no change to
/// the collision matrix: alien bullets hit them (AlienBullet already destroys what it hits,
/// except the ship itself), and the player's darts pass straight through, because
/// Bullet-Spaceship collisions are disabled.
/// </summary>
public static class CoverBlocks
{
    const float CellSize = 0.35f;   // world units per cell

    // From the AddOns board: C = plate, c = darker rivet, . = empty.
    static readonly string[] Shape =
    {
        ".CCCCCC.",
        "CCCCCCCC",
        "CCcCCcCC",
        "CCCCCCCC",
        "CC....CC"
    };

    /// <param name="y">Height of the blocks' centres.</param>
    /// <param name="xs">Centre x of each block.</param>
    public static void Build(float y, float[] xs)
    {
        var root = new GameObject("CoverBlocks").transform;
        int layer = LayerMask.NameToLayer("Spaceship");
        Sprite plate = PixelArt.Solid(PixelArt.Hex("#6E7FB0"));
        Sprite rivet = PixelArt.Solid(PixelArt.Hex("#4A5A8A"));
        int w = Shape[0].Length, h = Shape.Length;

        foreach (float x in xs)
        {
            for (int row = 0; row < h; row++)
            {
                for (int col = 0; col < w; col++)
                {
                    char ch = Shape[row][col];
                    if (ch == '.') continue;
                    var cell = new GameObject("CoverCell");
                    cell.layer = layer;
                    cell.transform.SetParent(root, false);
                    cell.transform.position = new Vector3(
                        x + (col - (w - 1) / 2f) * CellSize,
                        y + ((h - 1) / 2f - row) * CellSize,
                        0f);
                    cell.transform.localScale = Vector3.one * CellSize;
                    cell.AddComponent<SpriteRenderer>().sprite = ch == 'c' ? rivet : plate;
                    cell.AddComponent<BoxCollider>();   // 1x1x1, scaled to the cell
                    cell.AddComponent<CoverCell>();
                }
            }
        }
    }
}
