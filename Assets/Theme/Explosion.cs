using UnityEngine;

/// <summary>
/// A destroyed mobile suit: six pixel-art frames (flash, burst, ring, break-up, smoke)
/// at 100 ms each, plus a few debris pixels that keep the unit's motion and fade out.
/// Runs on unscaled time so it keeps playing through the hit-stop freeze.
/// </summary>
public class Explosion : MonoBehaviour
{
    const float FrameSeconds = 0.1f;
    const float DebrisSeconds = 0.6f;
    const int DebrisCount = 8;

    private SpriteRenderer frame;
    private Transform[] debris;
    private Vector3[] debrisVelocity;
    private SpriteRenderer[] debrisRenderers;
    private float startTime;

    /// <param name="drift">World units per second the debris inherits (e.g. the fleet's velocity).</param>
    public static void Spawn(Vector3 position, Vector3 drift)
    {
        var go = new GameObject("Explosion");
        go.transform.position = new Vector3(position.x, position.y, -0.5f);   // in front of the units
        go.AddComponent<Explosion>().Begin(drift);
    }

    void Begin(Vector3 drift)
    {
        startTime = Time.unscaledTime;
        frame = gameObject.AddComponent<SpriteRenderer>();
        frame.sprite = PixelArt.GetSprite(SpriteLibrary.Explosion[0]);
        frame.sortingOrder = 5;

        Color32 grey = PixelArt.Hex("#6E6A8A"), orange = PixelArt.Hex("#F4A340");
        float pixel = 1f / SpriteLibrary.Explosion[0].PixelsPerUnit;
        debris = new Transform[DebrisCount];
        debrisVelocity = new Vector3[DebrisCount];
        debrisRenderers = new SpriteRenderer[DebrisCount];
        for (int i = 0; i < DebrisCount; i++)
        {
            var bit = new GameObject("Debris");
            bit.transform.SetParent(transform, false);
            bit.transform.localScale = Vector3.one * pixel;
            var sr = bit.AddComponent<SpriteRenderer>();
            sr.sprite = PixelArt.Solid(i % 2 == 0 ? grey : orange);
            sr.sortingOrder = 4;
            debris[i] = bit.transform;
            debrisRenderers[i] = sr;
            debrisVelocity[i] = (Vector3)(Random.insideUnitCircle.normalized * Random.Range(2f, 4f)) + drift;
        }
    }

    void Update()
    {
        float age = Time.unscaledTime - startTime;
        float dt = Time.unscaledDeltaTime;

        int index = Mathf.FloorToInt(age / FrameSeconds);
        if (index < SpriteLibrary.Explosion.Length) frame.sprite = PixelArt.GetSprite(SpriteLibrary.Explosion[index]);
        else frame.enabled = false;

        float alpha = 1f - Mathf.Clamp01(age / DebrisSeconds);
        for (int i = 0; i < DebrisCount; i++)
        {
            debris[i].position += debrisVelocity[i] * dt;
            Color c = debrisRenderers[i].color;
            c.a = alpha;
            debrisRenderers[i].color = c;
        }

        if (age > Mathf.Max(DebrisSeconds, FrameSeconds * SpriteLibrary.Explosion.Length)) Destroy(gameObject);
    }
}
