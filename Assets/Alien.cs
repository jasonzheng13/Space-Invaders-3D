using UnityEngine;

public class Alien : MonoBehaviour
{
    [Header("Inscribed")]
    public GameObject bulletPrefab;
    public AlienFleet fleet;   // set by AlienFleet when it spawns this alien
    public int points = 10;    // score for shooting this alien down; set by AlienFleet

    static float bulletSpawnRate = .0025f;  // base chance to fire per physics tick (was .002)
    public static float shotCooldown = .1f;

    [Header("Dynamic")]
    [SerializeField] private float lastShot = 0;
    private PixelSkin skin;

    /// <summary>Called by AlienFleet right after spawning: which Zeon suit this alien is drawn as.</summary>
    void Awake()
    {
        // The fleet moves aliens by setting their position. Freezing the body stops the physics
        // solver from pushing an alien out of formation when it overlaps a cover cell; collisions
        // are still reported because the body stays dynamic.
        if (TryGetComponent(out Rigidbody rb)) rb.constraints = RigidbodyConstraints.FreezeAll;
    }

    public void Skin(SkinDef def)
    {
        skin = PixelSkin.Apply(gameObject, def);
    }

    void FixedUpdate()
    {
        // Mono-eyes scan faster as the fleet speeds up, so the tension shows on screen.
        if (skin != null && fleet != null) skin.EyeSpeed = fleet.SpeedRatio;

        if (!GameManager.IsPlaying) return;   // no shooting after the round ends

        // Survivors fire more often as the fleet shrinks (see AlienFleet.FireRateMultiplier).
        float fireChance = bulletSpawnRate * (fleet != null ? fleet.FireRateMultiplier : 1f);
        if (UnityEngine.Random.value < fireChance && Time.time - lastShot > shotCooldown)
        {
            GameObject bullet = Instantiate<GameObject>(bulletPrefab);
            bullet.transform.position = transform.position;
            lastShot = Time.time;
        }
    }

    void OnCollisionEnter(Collision coll)
    {
        // Descending into cover erases the cell but the alien survives (no kill, no score).
        if (coll.gameObject.TryGetComponent<CoverCell>(out _))
        {
            Destroy(coll.gameObject);
            return;
        }

        // Only a dart scores; touching the ship does not.
        if (coll.gameObject.TryGetComponent<Dart>(out _)) GameManager.AddScore(points);

        // Burst into pixels that keep drifting with the fleet, and freeze for a beat so the kill lands.
        Explosion.Spawn(transform.position, fleet != null ? fleet.Velocity : Vector3.zero);
        GameFeel.HitStop(0.05f);

        // Let the fleet own its list; it also updates the on-screen counter.
        if (fleet != null)
        {
            fleet.RemoveAlien(gameObject);
        }
        Destroy(gameObject);
        Destroy(coll.gameObject);
    }
}
