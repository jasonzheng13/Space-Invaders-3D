using TMPro;
using UnityEngine;

/// <summary>
/// Haro, the little green robot, calls out key moments in a speech bubble.
/// It appears on the right edge, just above the ship's lane so it never hides the Gundam,
/// and creates itself the first time something calls Say.
/// </summary>
public class HaroCallout : MonoBehaviour
{
    const float ShowSeconds = 2f;
    const float BounceInterval = 0.15f;
    // Below gameplay sprites (so incoming tracers are never hidden) but above the backdrop (-41 and lower).
    const int Order = -10;
    static readonly Vector2 BubbleSize = new Vector2(7.4f, 1f);

    private static HaroCallout instance;

    private Transform haro;
    private Vector3 haroRest;
    private GameObject bubble;
    private TextMeshPro text;
    private float hideAt = -1f;

    /// <summary>Shows a line for two seconds; a new line replaces the current one.</summary>
    public static void Say(string line)
    {
        if (instance == null) instance = new GameObject("Haro").AddComponent<HaroCallout>().Build();
        instance.text.text = line;
        instance.bubble.SetActive(true);
        instance.hideAt = Time.unscaledTime + ShowSeconds;
    }

    HaroCallout Build()
    {
        Camera cam = Camera.main;
        float viewH = cam.orthographicSize, viewW = viewH * cam.aspect;
        Vector3 c = cam.transform.position;
        float right = c.x + viewW, bottom = c.y - viewH;

        haro = new GameObject("HaroSprite").transform;
        haro.SetParent(transform, false);
        haroRest = new Vector3(right - 1.2f, bottom + 4.2f, -1f);
        haro.position = haroRest;
        var sr = haro.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = PixelArt.GetSprite(SpriteLibrary.Haro);
        sr.sortingOrder = Order + 1;

        // Bubble to Haro's left: a light panel with dark text.
        bubble = new GameObject("Bubble");
        bubble.transform.SetParent(transform, false);
        Vector3 centre = new Vector3(haroRest.x - 1f - BubbleSize.x / 2f, haroRest.y, -1f);
        var panel = new GameObject("Panel");
        panel.transform.SetParent(bubble.transform, false);
        panel.transform.position = centre;
        panel.transform.localScale = new Vector3(BubbleSize.x, BubbleSize.y, 1f);
        var psr = panel.AddComponent<SpriteRenderer>();
        psr.sprite = PixelArt.Solid(PixelArt.Hex("#F3ECE2"));
        psr.sortingOrder = Order;

        text = new GameObject("Line").AddComponent<TextMeshPro>();
        text.transform.SetParent(bubble.transform, false);
        text.transform.position = centre + new Vector3(0f, 0f, -0.01f);
        text.rectTransform.sizeDelta = BubbleSize - new Vector2(0.4f, 0.2f);
        text.color = PixelArt.Hex("#141225");
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.enableAutoSizing = true;
        text.fontSizeMin = 1f;
        text.fontSizeMax = 8f;
        text.sortingOrder = Order + 2;

        bubble.SetActive(false);
        return this;
    }

    void Update()
    {
        bool talking = bubble.activeSelf;
        if (talking && Time.unscaledTime >= hideAt) bubble.SetActive(false);

        // Bounce while talking, rest otherwise.
        float lift = talking && Mathf.FloorToInt(Time.unscaledTime / BounceInterval) % 2 == 0 ? 0.12f : 0f;
        haro.position = haroRest + new Vector3(0f, lift, 0f);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
