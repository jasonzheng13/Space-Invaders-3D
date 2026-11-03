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

    void FixedUpdate()
    {
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
        // Let the fleet own its list; it also updates the on-screen counter.
        if (fleet != null)
        {
            fleet.RemoveAlien(gameObject);
        }
        Destroy(gameObject);
        Destroy(coll.gameObject);
    }
}
