using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class GameStateManager : MonoBehaviour
{
    private enum GameState
    {
        Playing,
        Won,
        Lost
    }

    [SerializeField] private ArtifactHealth artifactHealth;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private EndGameOverlayUI endGameOverlay;
    [SerializeField] private Key restartKey = Key.R;
    [SerializeField] private bool stopTimeOnGameOver = true;

    private GameState state = GameState.Playing;

    public bool IsGameOver => state != GameState.Playing;
    public bool IsVictory => state == GameState.Won;
    public bool IsDefeat => state == GameState.Lost;

    private void Awake()
    {
        Time.timeScale = 1f;

        if (endGameOverlay != null)
        {
            endGameOverlay.Hide();
        }
    }

    private void OnEnable()
    {
        if (artifactHealth != null)
        {
            artifactHealth.Destroyed += Lose;
        }

        if (waveManager != null)
        {
            waveManager.AllWavesCompleted += Win;
        }
    }

    private void OnDisable()
    {
        if (artifactHealth != null)
        {
            artifactHealth.Destroyed -= Lose;
        }

        if (waveManager != null)
        {
            waveManager.AllWavesCompleted -= Win;
        }
    }

    private void Update()
    {
        if (!IsGameOver || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[restartKey].wasPressedThisFrame)
        {
            Restart();
        }
    }

    private void Win()
    {
        if (IsGameOver)
        {
            return;
        }

        if (artifactHealth != null && artifactHealth.IsDestroyed)
        {
            Lose();
            return;
        }

        state = GameState.Won;
        if (endGameOverlay != null)
        {
            endGameOverlay.ShowVictory();
        }

        EndGame("Victory");
    }

    private void Lose()
    {
        if (IsGameOver)
        {
            return;
        }

        state = GameState.Lost;
        if (endGameOverlay != null)
        {
            endGameOverlay.ShowDefeat();
        }

        EndGame("Defeat");
    }

    private void EndGame(string result)
    {
        Debug.Log($"{result}. Press {restartKey} to restart.", this);

        if (stopTimeOnGameOver)
        {
            Time.timeScale = 0f;
        }
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
