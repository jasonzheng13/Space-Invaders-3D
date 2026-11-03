using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Small effects that make hits feel heavy: a brief freeze on kills (hit stop) and
/// camera shake when the player is hit. Creates itself the first time it is used.
/// Both effects run on unscaled (real) time, because hit stop sets Time.timeScale to 0.
/// </summary>
public class GameFeel : MonoBehaviour
{
    private static GameFeel instance;

    private Camera cam;
    private Vector3 camRest;           // where the camera sits when not shaking
    private float stopUntil = -1f;     // realtime at which the freeze ends
    private float shakeUntil = -1f, shakeAmplitude;

    static GameFeel Instance
    {
        get
        {
            if (instance == null) instance = new GameObject("GameFeel").AddComponent<GameFeel>();
            return instance;
        }
    }

    void Awake()
    {
        cam = Camera.main;
        if (cam != null) camRest = cam.transform.position;
    }

    /// <summary>Freezes the game for a moment. Overlapping calls extend the freeze instead of stacking.</summary>
    public static void HitStop(float seconds)
    {
        GameFeel g = Instance;
        g.stopUntil = Mathf.Max(g.stopUntil, Time.unscaledTime + seconds);
        Time.timeScale = 0f;
    }

    /// <summary>Jitters the camera by up to amplitude world units, then puts it back exactly.</summary>
    public static void Shake(float seconds, float amplitude)
    {
        GameFeel g = Instance;
        g.shakeUntil = Time.unscaledTime + seconds;
        g.shakeAmplitude = amplitude;
    }

    void LateUpdate()
    {
        float now = Time.unscaledTime;
        if (stopUntil > 0f && now >= stopUntil)
        {
            Time.timeScale = 1f;
            stopUntil = -1f;
        }

        if (cam == null) return;
        cam.transform.position = now < shakeUntil
            ? camRest + (Vector3)(Random.insideUnitCircle * shakeAmplitude)
            : camRest;
    }

    void OnDestroy()
    {
        // A restart (scene reload) can happen mid-freeze or mid-shake; never leave the game frozen.
        Time.timeScale = 1f;
        if (cam != null) cam.transform.position = camRest;
        if (instance == this) instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ResetOnSceneLoad()
    {
        SceneManager.sceneLoaded += (scene, mode) => Time.timeScale = 1f;
    }
}
