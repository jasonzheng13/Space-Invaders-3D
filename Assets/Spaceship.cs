using UnityEngine;
using UnityEngine.InputSystem;

public class Spaceship : MonoBehaviour
{
    [Header("Inscribed")]
    public GameObject dartPrefab;
    public float speed = 10f;   // units per second
    public static float shotCooldown = .05f;
    [Tooltip("Extra gap kept between the ship and the screen edge, in world units.")]
    [SerializeField] private float edgePadding = 0.25f;

    [Header("Dynamic")]
    [SerializeField] private float lastShot = 0;

    private Camera cam;
    private float halfWidth;   // half the ship's visual width, measured from its renderers

    void Start()
    {
        cam = Camera.main;

        // Measure the ship from its child renderers so the clamp hugs the visible model,
        // not the empty root transform. Falls back to 1 unit if there are no renderers.
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
            halfWidth = bounds.extents.x;
        }
        else
        {
            halfWidth = 1f;
        }
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;   // no keyboard attached (e.g. some web/mobile setups)

        // -1, 0 or +1 depending on which arrow keys are held.
        float direction = 0f;
        if (keyboard.rightArrowKey.isPressed) direction += 1f;
        if (keyboard.leftArrowKey.isPressed) direction -= 1f;

        if (direction != 0f)
        {
            Vector3 position = transform.position;
            position.x += direction * speed * Time.deltaTime;
            float limit = HorizontalLimit();
            float centre = cam != null ? cam.transform.position.x : 0f;
            position.x = Mathf.Clamp(position.x, centre - limit, centre + limit);
            transform.position = position;
        }

        if (keyboard.spaceKey.wasPressedThisFrame && Time.time - lastShot > shotCooldown)
        {
            GameObject dart = Instantiate<GameObject>(dartPrefab);
            dart.transform.position = transform.position;
            lastShot = Time.time;
        }
    }

    /// <summary>
    /// Farthest distance from the camera's centre the ship may reach while staying fully on screen.
    /// Recomputed each call so resizing the Game window / browser keeps the clamp correct.
    /// For an orthographic camera, half the visible width = orthographicSize * aspect.
    /// </summary>
    private float HorizontalLimit()
    {
        if (cam == null) return 16f;   // sensible fallback if there is no Main Camera
        float halfScreenWidth = cam.orthographicSize * cam.aspect;
        return halfScreenWidth - halfWidth - edgePadding;
    }
}
