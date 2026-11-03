using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// How one unit looks and moves. Rects and points are in sprite pixels measured from the
/// sprite's top-left corner, so they can be read straight off the grid in SpriteLibrary.
/// </summary>
public sealed class SkinDef
{
    public SpriteDef Body;
    public RectInt Eye;               // starting position of the mono-eye; width 0 = no eye
    public int EyeSweepPx;            // how far the eye travels to the right before coming back
    public Color32 EyeColor;
    public RectInt[] Flames = new RectInt[0];   // thruster flames, flickering downward from their top edge
    public float BobAmplitude;        // world units
    public float BobPeriod = 2.4f;    // seconds for one full hover cycle
    public Vector2Int Muzzle = new Vector2Int(-1, -1);   // top-left of the 3x3 muzzle flash; x < 0 = none
    public bool FitCollider;          // resize a SphereCollider to match the sprite (mobile suits only)
}

/// <summary>
/// Replaces a GameObject's primitive meshes with an animated pixel-art sprite.
/// The original colliders and gameplay scripts are untouched; only what you see changes.
///
/// Hierarchy built under the target:
///   Skin        (SpriteRenderer for the body; bob, recoil and lean are applied here)
///     Flame N   (pivot at the flame's top edge so scaling stretches it downward)
///     Eye, Muzzle
/// </summary>
public class PixelSkin : MonoBehaviour
{
    [Header("Dynamic")]
    [SerializeField] private float leanAngle;

    /// <summary>Multiplies the eye sweep speed. The fleet raises it as it speeds up.</summary>
    public float EyeSpeed { get; set; } = 1f;

    /// <summary>Visible size of the body sprite in world units.</summary>
    public Vector2 BodySizeWorld { get; private set; }

    const float FlickerInterval = 0.08f;
    const float MaxLean = 8f;             // degrees
    const float LeanResponse = 10f;       // higher = snappier lean
    const float MuzzleSeconds = 0.06f;
    const float RecoilDistance = 0.05f;   // world units
    const float RecoilRecover = 0.1f;     // seconds
    const float EyePassSeconds = 1.4f;    // one pass left-to-right

    private SkinDef def;
    private Transform skin;
    private SpriteRenderer body;
    private Sprite normalSprite, flashSprite;
    private readonly List<Transform> flames = new List<Transform>();
    private Transform eye;
    private Vector3 eyeStart;
    private GameObject muzzle;
    private SpriteRenderer[] allRenderers;

    private float eyePhase;   // accumulated, so a change in EyeSpeed doesn't make the eye jump
    private float bobPhase, targetLean, fireTime = -999f, flashUntil = -1f, blinkUntil = -1f;
    private bool blinking;

    public static PixelSkin Apply(GameObject target, SkinDef skin)
    {
        PixelSkin ps = target.AddComponent<PixelSkin>();
        ps.Build(skin);
        return ps;
    }

    void Build(SkinDef skinDef)
    {
        def = skinDef;
        float ppu = def.Body.PixelsPerUnit;
        int w = def.Body.Rows[0].Length, h = def.Body.Rows.Length;
        BodySizeWorld = new Vector2(w / ppu, h / ppu);
        bobPhase = Random.value * Mathf.PI * 2f;   // so a fleet doesn't bob in lockstep

        // Hide the original primitives but keep their colliders.
        foreach (MeshRenderer mr in GetComponentsInChildren<MeshRenderer>()) mr.enabled = false;

        // Undo the parent's scale so 1 unit on the skin = 1 world unit, and sizes follow PPU.
        Vector3 s = transform.lossyScale;
        skin = new GameObject("Skin").transform;
        skin.SetParent(transform, false);
        skin.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
        skin.localPosition = new Vector3(0f, 0f, -0.1f / s.z);

        body = skin.gameObject.AddComponent<SpriteRenderer>();
        normalSprite = PixelArt.GetSprite(def.Body);
        flashSprite = PixelArt.GetSilhouette(def.Body);
        body.sprite = normalSprite;

        Color32 flameColor = PixelArt.Hex("#F4A340"), coreColor = PixelArt.Hex("#FFF1C9");
        foreach (RectInt r in def.Flames)
        {
            // Pivot object sits at the flame's top-centre; the coloured blocks hang below it.
            Transform pivot = new GameObject("Flame").transform;
            pivot.SetParent(skin, false);
            pivot.localPosition = PixelToLocal(r.x + r.width / 2f, r.y);
            AddBlock(pivot, "Outer", flameColor, r.width / ppu, r.height / ppu, -1);
            AddBlock(pivot, "Core", coreColor, Mathf.Max(1, r.width - 2) / ppu, r.height * 0.6f / ppu, 0);
            flames.Add(pivot);
        }

        if (def.Eye.width > 0)
        {
            eye = AddBlock(skin, "Eye", def.EyeColor, def.Eye.width / ppu, def.Eye.height / ppu, 2).transform;
            eyeStart = PixelToLocal(def.Eye.x + def.Eye.width / 2f, def.Eye.y + def.Eye.height / 2f);
            eye.localPosition = eyeStart;
        }

        if (def.Muzzle.x >= 0)
        {
            muzzle = AddBlock(skin, "Muzzle", coreColor, 3f / ppu, 3f / ppu, 3);
            muzzle.transform.localPosition = PixelToLocal(def.Muzzle.x + 1.5f, def.Muzzle.y + 1.5f);
            muzzle.SetActive(false);
        }

        allRenderers = skin.GetComponentsInChildren<SpriteRenderer>(true);

        // The old sphere was smaller than the new sprite, so shots that visibly hit a
        // mech's shoulder would pass through. Match the hitbox to what the player sees.
        if (def.FitCollider && TryGetComponent(out SphereCollider sphere))
        {
            float worldRadius = 0.4f * Mathf.Min(BodySizeWorld.x, BodySizeWorld.y);
            sphere.radius = worldRadius / Mathf.Max(s.x, s.y);
        }
    }

    /// <summary>Sprite pixel coordinates (from top-left) to a position relative to the sprite centre.</summary>
    Vector3 PixelToLocal(float px, float py)
    {
        float ppu = def.Body.PixelsPerUnit;
        int w = def.Body.Rows[0].Length, h = def.Body.Rows.Length;
        return new Vector3((px - w / 2f) / ppu, (h / 2f - py) / ppu, 0f);
    }

    /// <summary>A solid rectangle whose top edge sits on the parent's origin.</summary>
    GameObject AddBlock(Transform parent, string name, Color32 c, float width, float height, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PixelArt.Solid(c);
        sr.sortingOrder = order;
        go.transform.localScale = new Vector3(width, height, 1f);
        // Flames hang down from their pivot; eyes and muzzles are centred on their position.
        if (name == "Outer" || name == "Core") go.transform.localPosition = new Vector3(0f, -height / 2f, 0f);
        return go;
    }

    void Update()
    {
        float t = Time.time;

        // Hover bob plus recoil, both applied to the whole skin.
        float bob = def.BobAmplitude * Mathf.Sin(bobPhase + t * Mathf.PI * 2f / def.BobPeriod);
        float recoil = RecoilDistance * (1f - Mathf.Clamp01((t - fireTime) / RecoilRecover));
        Vector3 p = skin.localPosition;
        Vector3 s = transform.lossyScale;
        skin.localPosition = new Vector3(0f, (bob - recoil) / s.y, p.z);

        // Lean eases toward the target angle.
        leanAngle = Mathf.Lerp(leanAngle, targetLean, 1f - Mathf.Exp(-LeanResponse * Time.deltaTime));
        skin.localRotation = Quaternion.Euler(0f, 0f, leanAngle);

        // Flames alternate between short and long every flicker interval, offset from each other.
        int tick = Mathf.FloorToInt(t / FlickerInterval);
        for (int i = 0; i < flames.Count; i++)
        {
            float y = (tick + i) % 2 == 0 ? 0.6f : 1.2f;
            flames[i].localScale = new Vector3(1f, y, 1f);
        }

        // Mono-eye sweeps right and back, easing at each end.
        if (eye != null)
        {
            eyePhase += Time.deltaTime * EyeSpeed / EyePassSeconds;
            float pass = Mathf.PingPong(eyePhase, 1f);
            float travel = def.EyeSweepPx / def.Body.PixelsPerUnit;
            eye.localPosition = eyeStart + new Vector3(Mathf.SmoothStep(0f, 1f, pass) * travel, 0f, 0f);
        }

        if (muzzle != null && muzzle.activeSelf && t - fireTime > MuzzleSeconds) muzzle.SetActive(false);

        body.sprite = t < flashUntil ? flashSprite : normalSprite;

        if (blinking)
        {
            bool visible = t >= blinkUntil || Mathf.FloorToInt(t / FlickerInterval) % 2 == 0;
            foreach (SpriteRenderer sr in allRenderers) sr.enabled = visible;
            if (t >= blinkUntil) blinking = false;
        }
    }

    /// <summary>Muzzle flash and a small kick backwards.</summary>
    public void Fire()
    {
        fireTime = Time.time;
        if (muzzle != null) muzzle.SetActive(true);
    }

    /// <summary>-1 = moving left, 1 = moving right, 0 = level.</summary>
    public void SetLean(float direction)
    {
        targetLean = -MaxLean * Mathf.Clamp(direction, -1f, 1f);
    }

    /// <summary>Draws the body solid white for a moment.</summary>
    public void Flash(float seconds)
    {
        flashUntil = Time.time + seconds;
    }

    /// <summary>Flickers the whole unit on and off, then leaves it visible.</summary>
    public void Blink(float seconds)
    {
        blinkUntil = Time.time + seconds;
        blinking = true;
    }
}
