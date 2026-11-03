using UnityEngine;
using UnityEngine.InputSystem;

public class Spaceship : MonoBehaviour
{
    [Header("Inscribed")]
    public GameObject dartPrefab;
    public float speed = 12f;   // units per second (was 10; compensates for faster alien bullets)
    public static float shotCooldown = .35f;   // was .05, which allowed near-unlimited spam
    [Tooltip("Extra gap kept between the ship and the screen edge, in world units.")]
    [SerializeField] private float edgePadding = 0.25f;
    [Tooltip("Seconds of invulnerability after a hit, so overlapping bullets cost only one life.")]
    [SerializeField] private float invulnerableSeconds = 1f;

    [Header("Dynamic")]
    [SerializeField] private float lastShot = 0;
    [SerializeField] private float lastHit = -999f;

    private Camera cam;
    private float halfWidth;   // half the ship's visual width, measured from its sprite
    private PixelSkin skin;

    void Start()
    {
        cam = Camera.main;

        // Draw the ship as the pixel-art Gundam. The clamp uses the sprite's width,
        // because the original cylinders are now hidden.
        skin = PixelSkin.Apply(gameObject, SpriteLibrary.Skins.Gundam);
        halfWidth = skin.BodySizeWorld.x / 2f;
    }

    void Update()
    {
        if (!GameManager.IsPlaying) return;   // ignore input after the round ends

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;   // no keyboard attached (e.g. some web/mobile setups)

        // -1, 0 or +1 depending on which arrow keys are held.
        float direction = 0f;
        if (keyboard.rightArrowKey.isPressed) direction += 1f;
        if (keyboard.leftArrowKey.isPressed) direction -= 1f;

        skin.SetLean(direction);   // lean into the move; 0 levels out again

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
            skin.Fire();   // muzzle flash and recoil
        }
    }

    /// <summary>
    /// Only alien bullets (and aliens themselves) can collide with the ship, per the physics
    /// layer matrix. The ship is not destroyed; it reports the hit and the GameManager
    /// decides whether a life is lost or the round is over.
    /// </summary>
    void OnCollisionEnter(Collision coll)
    {
        if (Time.time - lastHit < invulnerableSeconds) return;   // within the grace period after the last hit
        lastHit = Time.time;
        GameManager.ReportShipHit();
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
