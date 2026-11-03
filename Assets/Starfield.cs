using UnityEngine;

/// <summary>
/// The orbital background, back to front:
///   1. scrolling stars (unlit quads at z = 20)
///   2. Earth's night side with an orange sunrise rim and a pulsing sun glow (z = 19)
///   3. the Side 7 colony cylinder drifting slowly (z = 18)
///   4. distant battle flashes popping in the upper half (z = 17)
///   5. dark debris tumbling past the screen edges only (z = 16)
/// Everything sits behind the gameplay plane (z = 0) and uses negative sorting orders,
/// so it can never cover or collide with the ship, aliens or bullets.
/// Purely cosmetic: it keeps moving even when the round is over.
/// </summary>
public class Starfield : MonoBehaviour
{
    [Header("Inscribed")]
    [SerializeField] private int starCount = 150;
    [SerializeField] private float depth = 20f;              // world z of the star layer
    [SerializeField] private Vector2 sizeRange = new Vector2(0.05f, 0.2f);
    [Tooltip("Scroll speed in units per second for the largest stars; smaller stars move slower for a parallax effect.")]
    [SerializeField] private float maxScrollSpeed = 2f;
    [SerializeField] private Color backgroundColor = Color.black;
    [Tooltip("Optional. If empty, a URP Unlit material is created at runtime.")]
    [SerializeField] private Material starMaterial;

    [Tooltip("Pixels per world unit for the Earth sprites. Lower = bigger planet.")]
    [SerializeField] private float earthPixelsPerUnit = 24f;
    [Tooltip("How far past the bottom-right corner of the view the planet's centre sits.")]
    [SerializeField] private Vector2 earthOffset = new Vector2(2.5f, -3f);

    private Camera cam;
    private float lastAspect;
    private Transform earth, sun, sunHalo, colony;
    private SpriteRenderer sunRenderer, sunHaloRenderer;
    private Vector3 colonyBase;
    private float earthRadius;
    private readonly Transform[] flashes = new Transform[4];
    private readonly SpriteRenderer[] flashRenderers = new SpriteRenderer[4];
    private readonly Transform[] debris = new Transform[4];
    private readonly float[] debrisSpin = new float[4];

    private Transform[] stars;
    private float[] speeds;
    private float halfWidth, halfHeight, centreX, centreY;

    void Start()
    {
        cam = Camera.main;
        if (cam == null) return;

        // Swap the skybox for a flat colour: the palette's "void", a very dark violet.
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = backgroundColor == Color.black ? (Color)PixelArt.Hex("#0D0B18") : backgroundColor;

        // Cover the visible area plus a margin so stars don't pop in at the edges.
        halfHeight = cam.orthographicSize + 1f;
        halfWidth = cam.orthographicSize * cam.aspect + 1f;
        centreX = cam.transform.position.x;
        centreY = cam.transform.position.y;

        Material material = starMaterial != null ? starMaterial : CreateUnlitMaterial();

        stars = new Transform[starCount];
        speeds = new float[starCount];
        for (int i = 0; i < starCount; i++)
        {
            GameObject star = GameObject.CreatePrimitive(PrimitiveType.Quad);
            star.name = "Star";
            Destroy(star.GetComponent<Collider>());   // stars must not take part in physics
            star.transform.SetParent(transform, false);

            float size = Random.Range(sizeRange.x, sizeRange.y);
            star.transform.localScale = Vector3.one * size;
            star.transform.position = new Vector3(
                centreX + Random.Range(-halfWidth, halfWidth),
                centreY + Random.Range(-halfHeight, halfHeight),
                depth);

            Renderer r = star.GetComponent<Renderer>();
            // sharedMaterial: all stars share one material instead of each getting a copy.
            if (material != null) r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;

            stars[i] = star.transform;
            speeds[i] = maxScrollSpeed * (size / sizeRange.y);   // bigger = "closer" = faster
        }

        BuildBackdrop();
        Anchor();
    }

    // ---------------- layers 2-5 ----------------

    void BuildBackdrop()
    {
        // Earth: three stacked circles make a dark planet with an ember-to-orange rim.
        earth = new GameObject("Earth").transform;
        earth.SetParent(transform, false);
        AddSprite(earth, PixelArt.Circle(620, PixelArt.Hex("#B5503F"), earthPixelsPerUnit), Vector3.zero, -50);
        AddSprite(earth, PixelArt.Circle(596, PixelArt.Hex("#F4A340"), earthPixelsPerUnit), new Vector3(0, 0, -0.01f), -49);
        AddSprite(earth, PixelArt.Circle(572, PixelArt.Hex("#1B2142"), earthPixelsPerUnit), new Vector3(0, 0, -0.02f), -48);
        earthRadius = 310f / earthPixelsPerUnit;

        // The sun just breaking over the limb, with a soft halo.
        sunHalo = new GameObject("SunHalo").transform;
        sunHalo.SetParent(transform, false);
        sunHaloRenderer = AddSprite(sunHalo, PixelArt.Circle(160, PixelArt.Hex("#F6B35A"), earthPixelsPerUnit), Vector3.zero, -47);
        sun = new GameObject("Sun").transform;
        sun.SetParent(transform, false);
        sunRenderer = AddSprite(sun, PixelArt.Circle(90, PixelArt.Hex("#FFE3A8"), earthPixelsPerUnit), Vector3.zero, -46);

        // Side 7 colony cylinder, built from flat blocks (sizes from the Stages board, 40 px = 1 unit).
        colony = new GameObject("Colony").transform;
        colony.SetParent(transform, false);
        Block(colony, "#6E6A8A", 0, 8, 240, 44, -45);
        Block(colony, "#A9A6C0", 24, 14, 190, 6, -44);
        Block(colony, "#2A2540", 24, 26, 190, 8, -44);
        Block(colony, "#4A4458", 24, 40, 190, 4, -44);
        Block(colony, "#8FB0FF", 36, -4, 54, 8, -44);    // the three long mirrors
        Block(colony, "#8FB0FF", 100, -4, 54, 8, -44);
        Block(colony, "#8FB0FF", 164, -4, 54, 8, -44);
        Block(colony, "#D9463B", 236, 26, 6, 6, -43);    // navigation light

        Sprite flash = PixelArt.Circle(18, PixelArt.Hex("#FFF1C9"), earthPixelsPerUnit);
        Sprite chunk = PixelArt.Solid(PixelArt.Hex("#4A4458"));
        for (int i = 0; i < 4; i++)
        {
            flashes[i] = new GameObject("Flash").transform;
            flashes[i].SetParent(transform, false);
            flashRenderers[i] = AddSprite(flashes[i], flash, Vector3.zero, -42);

            debris[i] = new GameObject("Debris").transform;
            debris[i].SetParent(transform, false);
            AddSprite(debris[i], chunk, Vector3.zero, -41);
            debris[i].localScale = Vector3.one * Random.Range(0.2f, 0.4f);
            debrisSpin[i] = Random.Range(-90f, 90f);
        }
    }

    /// <summary>Places everything relative to the current view; called again if the window changes shape.</summary>
    void Anchor()
    {
        lastAspect = cam.aspect;
        float viewH = cam.orthographicSize, viewW = viewH * cam.aspect;
        halfWidth = viewW + 1f;   // stars wrap across the new width too
        float right = centreX + viewW, bottom = centreY - viewH;

        Vector3 c = new Vector3(right + earthOffset.x, bottom + earthOffset.y, 19f);
        earth.position = c;

        // The sun sits where the planet's limb meets the right edge of the screen.
        float dx = Mathf.Min(right - c.x, earthRadius * 0.95f);
        float y = c.y + Mathf.Sqrt(earthRadius * earthRadius - dx * dx);
        sun.position = new Vector3(c.x + dx, y, 18.95f);
        sunHalo.position = new Vector3(c.x + dx, y, 18.97f);

        colonyBase = new Vector3(centreX - viewW * 0.55f, centreY + viewH * 0.45f, 18f);

        for (int i = 0; i < 4; i++)
        {
            ResetFlash(i);
            ResetDebris(i, Random.Range(bottom, centreY + viewH));
        }
    }

    void ResetFlash(int i)
    {
        float viewH = cam.orthographicSize, viewW = viewH * cam.aspect;
        flashes[i].position = new Vector3(centreX + Random.Range(-viewW, viewW), centreY + Random.Range(0f, viewH), 17f);
    }

    void ResetDebris(int i, float y)
    {
        // Only in the outer 15% of the view on either side, so it never crosses the action.
        float viewW = cam.orthographicSize * cam.aspect;
        float band = Random.Range(0.85f, 1f) * viewW * (i % 2 == 0 ? -1f : 1f);
        debris[i].position = new Vector3(centreX + band, y, 16f);
    }

    void UpdateBackdrop()
    {
        if (cam == null) return;
        if (!Mathf.Approximately(cam.aspect, lastAspect)) Anchor();
        float t = Time.time;

        // Sun glow pulses between 45% and 100% opacity every 1.3 s.
        float pulse = Mathf.Lerp(0.45f, 1f, 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / 1.3f));
        sunRenderer.color = new Color(1f, 1f, 1f, pulse);
        sunHaloRenderer.color = new Color(1f, 1f, 1f, pulse * 0.35f);

        // Colony drifts one unit left and right over 14 s.
        colony.position = colonyBase + new Vector3(Mathf.Sin(t * Mathf.PI * 2f / 14f), 0f, 0f);

        // Each flash pops and fades during the last part of its own 2.6 s cycle.
        for (int i = 0; i < 4; i++)
        {
            float cycle = (t / 2.6f + i * 0.25f) % 1f;
            float k = Mathf.InverseLerp(0.7f, 0.9f, cycle);
            bool on = cycle >= 0.7f && cycle < 0.9f;
            flashRenderers[i].enabled = on;
            flashes[i].localScale = Vector3.one * Mathf.Lerp(0.4f, 1.6f, k);
            flashRenderers[i].color = new Color(1f, 1f, 1f, 1f - k);
            if (cycle < 0.05f && !on) ResetFlash(i);   // new spot for the next pop
        }

        // Debris tumbles slowly downward and wraps to the top.
        for (int i = 0; i < 4; i++)
        {
            debris[i].position += new Vector3(0f, -1.2f * Time.deltaTime, 0f);
            debris[i].Rotate(0f, 0f, debrisSpin[i] * Time.deltaTime);
            if (debris[i].position.y < centreY - halfHeight) ResetDebris(i, centreY + halfHeight);
        }
    }

    static SpriteRenderer AddSprite(Transform parent, Sprite sprite, Vector3 localPos, int order)
    {
        var go = new GameObject("Sprite");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    /// <summary>A coloured rectangle given in Stages-board pixels (40 px = 1 unit), top-left origin.</summary>
    static void Block(Transform parent, string hex, float x, float y, float w, float h, int order)
    {
        const float pxPerUnit = 40f;
        var sr = AddSprite(parent, PixelArt.Solid(PixelArt.Hex(hex)),
            new Vector3((x + w / 2f - 120f) / pxPerUnit, (30f - (y + h / 2f)) / pxPerUnit, 0f), order);
        sr.transform.localScale = new Vector3(w / pxPerUnit, h / pxPerUnit, 1f);
    }

    void Update()
    {
        if (stars == null) return;
        UpdateBackdrop();

        for (int i = 0; i < stars.Length; i++)
        {
            Vector3 p = stars[i].position;
            p.y -= speeds[i] * Time.deltaTime;

            // Wrap a star that leaves the bottom back to the top at a new random x.
            if (p.y < centreY - halfHeight)
            {
                p.y += 2f * halfHeight;
                p.x = centreX + Random.Range(-halfWidth, halfWidth);
            }
            stars[i].position = p;
        }
    }

    private static Material CreateUnlitMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) return null;   // keep the primitive's default material
        Material m = new Material(shader);
        m.color = Color.white;
        return m;
    }
}
