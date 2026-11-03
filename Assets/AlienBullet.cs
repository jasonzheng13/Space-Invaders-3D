using UnityEngine;

public class AlienBullet : MonoBehaviour
{
    static float speed = .1f;        // units per physics tick
    static float despawnY = -10f;    // just below the bottom of the orthographic camera

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
        Destroy(coll.gameObject);
    }
}
