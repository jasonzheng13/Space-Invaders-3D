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
    [SerializeField] private float speed = .1f;        // units per physics tick; sign = direction
    [SerializeField] private float leftBound = -16f;
    [SerializeField] private float rightBound = 16f;
    [SerializeField] private float downTime = .1f;     // seconds spent moving down at each turn

    [Header("Dynamic")]
    public List<GameObject> aliens = new List<GameObject>();
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
                alien.GetComponent<Alien>().fleet = this;
            }
        }
        alienCounter.SetCount(aliens.Count);
    }

    void FixedUpdate()
    {
        // Move every alien and track the fleet's horizontal extent in the same pass.
        float left = float.MaxValue, right = float.MinValue;
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

    /// <summary>Called by an Alien when it is destroyed. Keeps the list and the UI in sync.</summary>
    public void RemoveAlien(GameObject alien)
    {
        aliens.Remove(alien);
        alienCounter.SetCount(aliens.Count);
    }
}
