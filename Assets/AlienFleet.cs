using UnityEngine;
using System.Collections.Generic;

public class AlienFleet : MonoBehaviour
{
    [Header("Inscribed")]
    public GameObject alienPrefab;
    public AlienCounter alienCounter;

    [SerializeField] private int columns = 10;
    [SerializeField] private int rows = 3;
    [SerializeField] private float spacing = 2f;
    [SerializeField] private Vector2 gridOrigin = new Vector2(-12f, 10f); // top-left alien
    [SerializeField] private float baseSpeed = .12f;   // units per physics tick with a full fleet (was .1)
    [Tooltip("Fractional speed increase per alien destroyed. 0.04 = 4% faster per kill, so the last alien of 30 moves ~2.16x base speed.")]
    [SerializeField] private float speedUpPerKill = 0.04f;   // was .05
    [SerializeField] private float leftBound = -16f;
    [SerializeField] private float rightBound = 16f;
    [SerializeField] private float downTime = .1f;     // seconds spent moving down at each turn
    [Tooltip("When the lowest alien's y drops to this value the player can no longer win and the round ends.")]
    [SerializeField] private float gameOverY = -4.5f;

    [Header("Dynamic")]
    public List<GameObject> aliens = new List<GameObject>();
    [SerializeField] private float speed;   // current speed; sign = direction. Grows as aliens die.
    private int startingCount;
    private bool movingDown = false;

    void Start()
    {
        for (int col = 0; col < columns; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                GameObject alien = Instantiate<GameObject>(alienPrefab);
                alien.transform.position = new Vector3(
                    gridOrigin.x + spacing * col,
                    gridOrigin.y - spacing * row,
                    0);
                aliens.Add(alien);
                Alien a = alien.GetComponent<Alien>();
                a.fleet = this;
                // Heavy Rick Doms hold the top row; Zakus fill the rows below.
                a.Skin(row == 0 ? SpriteLibrary.Skins.RickDom : SpriteLibrary.Skins.Zaku);
            }
        }
        startingCount = aliens.Count;
        speed = baseSpeed;
        alienCounter.SetCount(aliens.Count);
    }

    void FixedUpdate()
    {
        if (!GameManager.IsPlaying) return;   // freeze the fleet once the round has ended

        // Move every alien and track the fleet's extent in the same pass.
        float left = float.MaxValue, right = float.MinValue, lowest = float.MaxValue;
        foreach (GameObject alien in aliens)
        {
            Vector3 position = alien.transform.position;
            if (!movingDown)
            {
                position.x += speed;
            }
            else
            {
                position.y -= Mathf.Abs(speed);
            }
            alien.transform.position = position;

            left = Mathf.Min(left, position.x);
            right = Mathf.Max(right, position.x);
            lowest = Mathf.Min(lowest, position.y);
        }

        // The invasion succeeded: aliens reached the player's row.
        if (lowest <= gameOverY)
        {
            GameManager.ReportLoss("The Zeon fleet broke through");
            return;
        }

        // Reverse direction and step down when the outermost alien hits a side bound.
        if ((speed > 0 && right >= rightBound) || (speed < 0 && left <= leftBound))
        {
            speed = -speed;
            movingDown = true;
            Invoke(nameof(StartSidewaysMotion), downTime);
        }
    }

    void StartSidewaysMotion()
    {
        movingDown = false;
    }

    /// <summary>Current speed relative to a full fleet: 1 at the start, higher as aliens die.</summary>
    public float SpeedRatio => Mathf.Abs(speed) / baseSpeed;

    /// <summary>The fleet's movement in units per second (debris from kills drifts with it).</summary>
    public Vector3 Velocity => movingDown
        ? new Vector3(0f, -Mathf.Abs(speed) / Time.fixedDeltaTime, 0f)
        : new Vector3(speed / Time.fixedDeltaTime, 0f, 0f);

    /// <summary>
    /// How much more often each surviving alien fires compared with a full fleet.
    /// Square root of (starting / remaining): 1x with all 30, ~5.5x with one left.
    /// Without this, total enemy fire falls to almost nothing as the fleet thins out.
    /// </summary>
    public float FireRateMultiplier =>
        aliens.Count > 0 ? Mathf.Sqrt((float)startingCount / aliens.Count) : 1f;

    /// <summary>Called by an Alien when it is destroyed. Keeps the list and the UI in sync.</summary>
    public void RemoveAlien(GameObject alien)
    {
        aliens.Remove(alien);
        alienCounter.SetCount(aliens.Count);
        UpdateSpeed();

        if (aliens.Count == 0)
        {
            GameManager.ReportWin();
        }
    }

    /// <summary>
    /// Classic Space Invaders feel: fewer aliens = faster fleet.
    /// Speed scales linearly with the number destroyed; the sign (current direction) is kept.
    /// </summary>
    private void UpdateSpeed()
    {
        int destroyed = startingCount - aliens.Count;
        float magnitude = baseSpeed * (1f + speedUpPerKill * destroyed);
        speed = Mathf.Sign(speed) * magnitude;
    }
}
