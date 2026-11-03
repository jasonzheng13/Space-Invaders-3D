using UnityEngine;

public class Dart : MonoBehaviour
{
    static float speed = .3f;        // units per physics tick
    static float despawnY = 10f;     // just above the top of the orthographic camera

    void FixedUpdate()
    {
        Vector3 position = transform.position;
        position.y += speed;
        transform.position = position;
        if (position.y > despawnY)
        {
            Destroy(gameObject);
        }
    }
}
