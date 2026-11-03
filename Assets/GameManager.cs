using UnityEngine;
using TMPro;

/// <summary>
/// Tracks whether the round is still being played and ends it on a win or a loss.
/// One instance lives in the scene; other scripts reach it through the static helpers
/// so they never need a reference wired in the Inspector.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum State { Playing, Won, Lost }

    [Header("Inscribed")]
    [Tooltip("Optional. If set, the win/lose message is written here when the round ends.")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Dynamic")]
    [SerializeField] private State state = State.Playing;

    private static GameManager instance;

    /// <summary>True while gameplay should run. Also true if no GameManager exists, so the game still works without one.</summary>
    public static bool IsPlaying => instance == null || instance.state == State.Playing;

    void Awake()
    {
        instance = this;
        state = State.Playing;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static void ReportWin()
    {
        if (instance != null) instance.EndRound(State.Won, "YOU WIN!");
    }

    public static void ReportLoss(string reason)
    {
        if (instance != null) instance.EndRound(State.Lost, "GAME OVER\n" + reason);
    }

    private void EndRound(State result, string message)
    {
        // Only the first outcome counts; e.g. a stray bullet hitting the ship after a win is ignored.
        if (state != State.Playing) return;

        state = result;
        Debug.Log(message);
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
