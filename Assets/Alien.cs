using UnityEngine;

public class Alien : MonoBehaviour
{
    [Header("Inscribed")]
    public GameObject bulletPrefab;
    public AlienFleet fleet;   // set by AlienFleet when it spawns this alien

    static float bulletSpawnRate = .002f;   // chance to fire per physics tick
    public static float shotCooldown = .1f;

    [Header("Dynamic")]
    [SerializeField] private float lastShot = 0;

    void FixedUpdate()
    {
        if (UnityEngine.Random.value < bulletSpawnRate && Time.time - lastShot > shotCooldown)
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
