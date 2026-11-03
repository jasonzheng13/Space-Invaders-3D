using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The opening cutscene, played once per run before the start screen:
///   0.0 s  White Base hangar; the pilot floats beside the Gundam's head
///   1.5 s  the chest hatch opens and the pilot drifts down into it
///   3.0 s  the hatch slams shut (small camera shake)
///   3.8 s  the visor ignites and the hangar switches to red launch alert
///   4.8 s  catapult launch seen from behind, with speed lines and a banner
///   6.0 s  cut to the play view; 7.0 s the start screen appears
/// Enter skips it. Everything is built from sprites at runtime and laid out in the
/// Intro board's 368 x 200 frame, scaled to fit the camera, in front of the game.
/// </summary>
public class IntroSequence : MonoBehaviour
{
    /// <summary>True once the intro has run; an R restart reloads the scene but skips it.</summary>
    public static bool HasPlayed { get; private set; }

    /// <summary>True while the cutscene is on screen.</summary>
    public static bool IsPlaying { get; private set; }

    const float Z = -5f;                 // in front of the gameplay plane (z = 0)
    const int Order = 100;               // and above every gameplay sprite's sorting order
    const float BoardW = 368f, BoardH = 200f;

    private float u;                     // world units per board pixel
    private Vector2 topLeft;
    private float elapsed;
    private Transform root, hangar, launch, pilot, eyeGlow;
    private SpriteRenderer hatch, lightBar, alert, eyeRenderer;
    private readonly List<Transform> speedLines = new List<Transform>();
    private bool hatchShaken;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        // With "Enter Play Mode Options" (no domain reload) statics survive between Play sessions.
        HasPlayed = false;
        IsPlaying = false;
    }

    void Start()
    {
        Camera cam = Camera.main;
        float viewH = cam.orthographicSize, viewW = viewH * cam.aspect;
        u = Mathf.Max(viewW * 2f / BoardW, viewH * 2f / BoardH);   // cover the whole view
        topLeft = new Vector2(cam.transform.position.x - BoardW / 2f * u, cam.transform.position.y + BoardH / 2f * u);
        IsPlaying = true;

        root = new GameObject("Intro").transform;
        BuildHangar();
        BuildLaunch();
        launch.gameObject.SetActive(false);
    }

    // ---------------- building ----------------

    void BuildHangar()
    {
        hangar = Group("Hangar");
        Rect(hangar, 0, 0, BoardW, BoardH, "#1E1B2E", 0);
        Rect(hangar, 232, 22, 126, 140, "#3A3557", 1);               // door frame
        Rect(hangar, 236, 26, 118, 132, "#0D0B18", 2);               // open space beyond
        for (int i = 0; i < 14; i++) Rect(hangar, Random.Range(242f, 350f), Random.Range(32f, 152f), 2, 2, "#F3ECE2", 3);
        // Parts that change colour use a white sprite tinted through SpriteRenderer.color
        // (sprite colour and tint multiply, so a coloured sprite could not be re-tinted).
        lightBar = Rect(hangar, 0, 0, BoardW, 10, "#FFFFFF", 4);
        alert = Rect(hangar, 0, 0, BoardW, BoardH, "#FFFFFF", 5);
        alert.enabled = false;
        Rect(hangar, 0, 160, BoardW, 40, "#2A2540", 6);              // deck
        for (int i = 0; i < 24; i++) Rect(hangar, i * 16, 160, 16, 8, i % 2 == 0 ? "#F2C230" : "#141225", 7);

        // Front-view Gundam: 3 board pixels per sprite pixel, top-left at (50, 70).
        const float cell = 3f;
        var gundam = SpriteAt(hangar, PixelArt.GetSprite(SpriteLibrary.GundamFront), 50 + 14 * cell, 70 + 13 * cell, cell, 8);
        hatch = Rect(hangar, 50 + 12 * cell, 70 + 10 * cell, 4 * cell, 3 * cell, "#FFFFFF", 9);
        hatch.enabled = false;
        eyeRenderer = Rect(hangar, 50 + 12 * cell, 70 + 4 * cell, 4 * cell, cell, "#C79BFF", 10);
        eyeGlow = Rect(hangar, 50 + 10 * cell, 70 + 2.5f * cell, 8 * cell, 4 * cell, "#C79BFF", 9).transform;
        eyeRenderer.enabled = false;
        eyeGlow.gameObject.SetActive(false);

        pilot = SpriteAt(hangar, PixelArt.GetSprite(SpriteLibrary.Pilot), 123, 64, 2f, 11).transform;
    }

    void BuildLaunch()
    {
        launch = Group("Launch");
        Rect(launch, 0, 0, BoardW, BoardH, "#1E1B2E", 0);
        Rect(launch, 0, 150, BoardW, 10, "#3A3557", 1);              // catapult rail
        for (int i = 0; i < 9; i++)
        {
            var line = Rect(launch, Random.Range(200f, 360f), Random.Range(20f, 145f), Random.Range(30f, 90f), 2, "#B8AFC9", 2);
            speedLines.Add(line.transform);
        }

        // Back-view Gundam with its normal animated thrusters, lifted above the hangar sprites.
        var go = new GameObject("LaunchGundam");
        go.transform.SetParent(launch, false);
        go.transform.position = Board(150 + 42, 58 + 39, -0.5f);
        PixelSkin.Apply(go, SpriteLibrary.Skins.Gundam);
        // PixelSkin cancels its parent's scale, so enlarge its Skin child instead: 3 board px per sprite pixel.
        go.transform.Find("Skin").localScale = Vector3.one * (3f * u * SpriteLibrary.GundamBack.PixelsPerUnit);
        foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>(true)) sr.sortingOrder += Order + 10;

        Rect(launch, 0, 164, BoardW, 32, "#141225", 20);             // banner bar
        var text = new GameObject("Banner").AddComponent<TextMeshPro>();
        text.transform.SetParent(launch, false);
        text.transform.position = Board(BoardW / 2f, 180, -0.6f);
        text.rectTransform.sizeDelta = new Vector2(BoardW * u, 26 * u);
        text.text = "RX-78-2, LAUNCH!";
        text.color = PixelArt.Hex("#F2C230");
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.enableAutoSizing = true;
        text.fontSizeMin = 1f;
        text.fontSizeMax = 500f;
        text.sortingOrder = Order + 30;
    }

    Transform Group(string name)
    {
        var t = new GameObject(name).transform;
        t.SetParent(root, false);
        return t;
    }

    /// <summary>Board pixel (from the top-left of the 368 x 200 frame) to a world position.</summary>
    Vector3 Board(float px, float py, float dz = 0f)
    {
        return new Vector3(topLeft.x + px * u, topLeft.y - py * u, Z + dz);
    }

    /// <summary>A solid rectangle in board pixels. Higher layer = drawn on top.</summary>
    SpriteRenderer Rect(Transform parent, float x, float y, float w, float h, string hex, int layer)
    {
        var go = new GameObject("Rect");
        go.transform.SetParent(parent, false);
        go.transform.position = Board(x + w / 2f, y + h / 2f, -layer * 0.001f);
        go.transform.localScale = new Vector3(w * u, h * u, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PixelArt.Solid(PixelArt.Hex(hex));
        sr.sortingOrder = Order + layer;
        return sr;
    }

    /// <summary>A pixel-art sprite centred at a board position, cell board pixels per sprite pixel.</summary>
    SpriteRenderer SpriteAt(Transform parent, Sprite sprite, float cx, float cy, float cell, int layer)
    {
        var go = new GameObject("Sprite");
        go.transform.SetParent(parent, false);
        go.transform.position = Board(cx, cy, -layer * 0.001f);
        go.transform.localScale = Vector3.one * (cell * u * sprite.pixelsPerUnit);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = Order + layer;
        return sr;
    }

    // ---------------- playing ----------------

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.enterKey.wasPressedThisFrame)
        {
            Finish();
            return;
        }

        elapsed += Time.deltaTime;
        float t = elapsed;

        if (t < 4.8f)
        {
            // Pilot: float and wobble, then drift along a curve into the open hatch.
            Vector2 start = new Vector2(123, 64), hatchPos = new Vector2(93, 100), control = new Vector2(150, 110);
            Vector2 p;
            if (t < 1.5f) p = start + new Vector2(Mathf.Sin(t * 2.1f) * 3f, Mathf.Sin(t * 1.7f) * 2.5f);
            else
            {
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 1.5f) / 1.5f));
                p = (1 - k) * (1 - k) * start + 2 * (1 - k) * k * control + k * k * hatchPos;   // quadratic curve
            }
            pilot.position = Board(p.x, p.y, -0.011f);
            pilot.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 2f) * 8f);
            pilot.gameObject.SetActive(t < 3f);

            hatch.enabled = t >= 1.5f;
            hatch.color = t < 3f ? (Color)PixelArt.Hex("#F4A340") : (Color)PixelArt.Hex("#3E5FB8");
            if (t >= 3f && !hatchShaken)
            {
                hatchShaken = true;
                GameFeel.Shake(0.15f, 0.06f);
            }

            bool lit = t >= 3.8f;
            eyeRenderer.enabled = lit;
            eyeGlow.gameObject.SetActive(lit);
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / 1.1f);
            eyeGlow.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.15f + 0.25f * pulse);
            alert.enabled = lit;
            Color red = PixelArt.Hex("#D9463B");
            red.a = 0.2f + 0.35f * pulse;
            alert.color = red;
            lightBar.color = lit ? (Color)PixelArt.Hex("#D9463B") : (Color)PixelArt.Hex("#F3ECE2");
        }
        else if (t < 6f)
        {
            hangar.gameObject.SetActive(false);
            launch.gameObject.SetActive(true);
            foreach (Transform line in speedLines)
            {
                line.position += Vector3.left * (543f * u * Time.deltaTime);   // 380 board px per 0.7 s
                if (line.position.x < topLeft.x - 100 * u) line.position = Board(Random.Range(260f, 400f), Random.Range(20f, 145f), -0.002f);
            }
        }
        else if (t < 7f)
        {
            root.gameObject.SetActive(false);   // the play view shows through for a beat
        }
        else
        {
            Finish();
        }
    }

    void Finish()
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        HasPlayed = true;
        if (root != null) Destroy(root.gameObject);
        GameManager.OnIntroFinished();
        Destroy(this);
    }

    void OnDestroy()
    {
        // Leaving the scene mid-intro must not leave the flag stuck on.
        IsPlaying = false;
        if (root != null) Destroy(root.gameObject);
    }
}
