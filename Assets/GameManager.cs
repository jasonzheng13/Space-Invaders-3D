using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Owns the round's lifecycle: Ready (start screen) -> Playing -> Won / Lost (end screen).
/// One instance lives in the scene; other scripts reach it through the static helpers
/// so they never need a reference wired in the Inspector.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum State { Ready, Playing, Won, Lost }

    [Header("Inscribed")]
    [Tooltip("Shown before the round starts. Hidden when the player presses Enter.")]
    [SerializeField] private GameObject startPanel;
    [Tooltip("Shown when the round ends (win or lose).")]
    [SerializeField] private GameObject endPanel;
    [Tooltip("Label inside the end panel that receives the result message.")]
    [SerializeField] private TextMeshProUGUI resultText;
    [Tooltip("HUD label showing remaining lives.")]
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private int startingLives = 3;

    [Header("Dynamic")]
    [SerializeField] private State state = State.Ready;
    [SerializeField] private int lives;

    private static GameManager instance;

    /// <summary>True while gameplay should run. Also true if no GameManager exists, so the game still works without one.</summary>
    public static bool IsPlaying => instance == null || instance.state == State.Playing;

    void Awake()
    {
        instance = this;
        state = State.Ready;
        lives = startingLives;
        UpdateLivesText();
        SetActiveSafe(startPanel, true);
        SetActiveSafe(endPanel, false);
        SetStartText("<color=#F4A340>GUNDAM INVADERS</color>\nPress ENTER to launch");
    }

    /// <summary>Replaces the start screen's label (rich text allowed). The label is found under the start panel.</summary>
    public static void SetStartText(string richText)
    {
        if (instance == null || instance.startPanel == null) return;
        TextMeshProUGUI label = instance.startPanel.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null) label.text = richText;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Enter starts; R restarts. Space is deliberately not used because it also fires a dart.
        if (state == State.Ready && keyboard.enterKey.wasPressedThisFrame)
        {
            StartGame();
        }
        else if ((state == State.Won || state == State.Lost) && keyboard.rKey.wasPressedThisFrame)
        {
            RestartGame();
        }
    }

    /// <summary>Public so a uGUI Button's OnClick can call it as well as the keyboard.</summary>
    public void StartGame()
    {
        if (state != State.Ready) return;
        state = State.Playing;
        SetActiveSafe(startPanel, false);
    }

    /// <summary>Reloads the scene, which rebuilds the fleet, ship and UI from scratch.</summary>
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void ReportWin()
    {
        if (instance != null) instance.EndRound(State.Won, "MISSION COMPLETE");
    }

    public static void ReportLoss(string reason)
    {
        if (instance != null) instance.EndRound(State.Lost, "MOBILE SUIT LOST\n" + reason);
    }

    /// <summary>Called by the Spaceship when an alien bullet hits it. Costs one life; the last one ends the round.</summary>
    public static void ReportShipHit()
    {
        if (instance == null || instance.state != State.Playing) return;

        instance.lives--;
        instance.UpdateLivesText();
        if (instance.lives <= 0)
        {
            instance.EndRound(State.Lost, "MOBILE SUIT LOST\nThe Gundam was destroyed");
        }
    }

    private void UpdateLivesText()
    {
        // Armor pips: one filled square per remaining life, one hollow square per life lost.
        if (livesText != null)
        {
            int lost = Mathf.Max(0, startingLives - lives);
            livesText.text = "ARMOR " + new string('\u25A0', Mathf.Max(0, lives)) + new string('\u25A1', lost);
        }
    }

    private void EndRound(State result, string message)
    {
        // Only the first outcome counts; e.g. a stray bullet hitting the ship after a win is ignored.
        if (state != State.Playing) return;

        state = result;
        Debug.Log(message);
        if (resultText != null)
        {
            resultText.text = message + "\n\nPress R to restart";
        }
        SetActiveSafe(endPanel, true);
    }

    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }
}
