using UnityEngine;
using TMPro;

public class AlienCounter : MonoBehaviour
{
    private TextMeshProUGUI uiText;

    void Awake()
    {
        // Awake (not Start) so the fleet can call SetCount from its own Start safely.
        uiText = GetComponent<TextMeshProUGUI>();
    }

    /// <summary>Rebuilds the label. Call only when the count changes, not every frame.</summary>
    public void SetCount(int count)
    {
        uiText.text = "ZEON FORCES: " + count.ToString("#,0");
    }
}
