using UnityEngine;

/// <summary>
/// Replaces the default sky with a black background and a slowly scrolling field of stars.
/// Stars are small unlit quads placed far behind the gameplay plane (z = 0), so they can
/// never collide with or hide the ship, aliens or bullets.
/// Purely cosmetic: it keeps scrolling even when the round is over.
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

    private Transform[] stars;
    private float[] speeds;
    private float halfWidth, halfHeight, centreX, centreY;

    void Start()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Swap the skybox for a flat colour.
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = backgroundColor;

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
    }

    void Update()
    {
        if (stars == null) return;

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
