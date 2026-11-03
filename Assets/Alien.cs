using UnityEngine;

public class Alien : MonoBehaviour
{
    [Header("Inscribed")]
    public GameObject bulletPrefab;
    public AlienFleet fleet;   // set by AlienFleet when it spawns this alien

    static float bulletSpawnRate = .0025f;  // base chance to fire per physics tick (was .002)
    public static float shotCooldown = .1f;

    [Header("Dynamic")]
    [SerializeField] private float lastShot = 0;
    private PixelSkin skin;

    /// <summary>Called by AlienFleet right after spawning: which Zeon suit this alien is drawn as.</summary>
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
