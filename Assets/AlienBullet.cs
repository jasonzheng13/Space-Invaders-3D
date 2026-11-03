using UnityEngine;

public class AlienBullet : MonoBehaviour
{
    static float speed = .15f;       // units per physics tick (was .1)
    static float despawnY = -10f;    // just below the bottom of the orthographic camera

    void Start()
    {
        PixelSkin.Apply(gameObject, SpriteLibrary.Skins.ZakuTracer);   // yellow machine-gun tracer
    }

    void FixedUpdate()
    {
        Vector3 position = transform.position;
        position.y -= speed;
        transform.position = position;
        if (position.y < despawnY)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision coll)
    {
        Destroy(gameObject);

        // Darts are destroyed on contact. The Spaceship is not: it handles the hit itself
        // (see Spaceship.OnCollisionEnter) so lives can be deducted instead.
        if (!coll.gameObject.TryGetComponent<Spaceship>(out _))
        {
            // A dart shot this bullet down: small pop, no freeze (that is saved for kills).
            if (coll.gameObject.TryGetComponent<Dart>(out _)) Explosion.Spawn(transform.position, Vector3.zero);
            Destroy(coll.gameObject);
        }
    }
}
