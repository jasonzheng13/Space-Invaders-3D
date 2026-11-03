using UnityEngine;

/// <summary>
/// Char's red Zaku streaking across the top of the screen: the bonus target, like the
/// classic Space Invaders UFO. It is not part of the fleet, so it never affects the
/// win condition. Two fading afterimages trail behind it.
/// It sits on the Alien physics layer, so darts can hit it and alien bullets ignore it.
/// </summary>
public class CharFlyover : MonoBehaviour
{
    const float Speed = 18f;           // units per second (about 3x the fleet's starting pace)
    const int Points = 150;
    const float GhostSpacing = 0.6f;

    private float direction;           // +1 = moving right, -1 = moving left
    private float exitX;
    private Rigidbody body;

    public static void Spawn(float y, float direction)
    {
        Camera cam = Camera.main;
        float viewW = cam.orthographicSize * cam.aspect;
        float cx = cam.transform.position.x;

        var go = new GameObject("CharFlyover");
        go.layer = LayerMask.NameToLayer("Alien");
        go.transform.position = new Vector3(cx - direction * (viewW + 2f), y, 0f);

        // A kinematic rigidbody so OnCollisionEnter fires when a dart (which has a normal rigidbody) touches it.
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        go.AddComponent<SphereCollider>();   // resized to the sprite by PixelSkin

        CharFlyover f = go.AddComponent<CharFlyover>();
        f.direction = direction;
        f.exitX = cx + direction * (viewW + 2f);
        f.body = rb;

        PixelSkin skin = PixelSkin.Apply(go, SpriteLibrary.Skins.CharZaku);
        f.AddGhost(skin, 1, 0.35f);
        f.AddGhost(skin, 2, 0.15f);
    }

    void AddGhost(PixelSkin skin, int n, float alpha)
    {
        var ghost = new GameObject("Afterimage");
        ghost.transform.SetParent(transform, false);
        ghost.transform.localPosition = new Vector3(-direction * GhostSpacing * n, 0f, 0.05f * n);
        var sr = ghost.AddComponent<SpriteRenderer>();
        sr.sprite = PixelArt.GetSprite(SpriteLibrary.CharZaku);
        sr.color = new Color(1f, 1f, 1f, alpha);
        sr.sortingOrder = -n;
    }

    void FixedUpdate()
    {
        if (!GameManager.IsPlaying) return;
        Vector3 next = body.position + new Vector3(direction * Speed * Time.fixedDeltaTime, 0f, 0f);
        body.MovePosition(next);
        if ((direction > 0f && next.x > exitX) || (direction < 0f && next.x < exitX)) Destroy(gameObject);
    }

    void OnCollisionEnter(Collision coll)
    {
        if (!coll.gameObject.TryGetComponent<Dart>(out _)) return;
        Explosion.Spawn(transform.position, new Vector3(direction * Speed * 0.5f, 0f, 0f));
        GameFeel.HitStop(0.05f);
        GameManager.AddScore(Points);
        Destroy(coll.gameObject);
        Destroy(gameObject);
    }
}
