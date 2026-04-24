using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PauseMenuController : MonoBehaviour
{
    [Header("Style")]
    [SerializeField] private Color overlayColor = new Color(0.08f, 0.09f, 0.12f, 0.88f);
    [SerializeField] private Vector2 buttonSize = new Vector2(360f, 64f);
    [SerializeField] private float resumeInputLockSeconds = 0.5f;

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private Key pauseKey = Key.Escape;
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    [Header("UI")]
    [SerializeField] private Text titleText;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    private bool isPaused;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (pausePanel == null)
        {
            pausePanel = gameObject;
        }

        Time.timeScale = 1f;
        MoveControlsIntoPausePanel();
        ConfigureUi();
        SetPausePanelVisible(false);
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current[pauseKey].wasPressedThisFrame)
        {
            return;
        }

        TogglePause();
    }

    public void TogglePause()
    {
        if (IsGameOver())
        {
            return;
        }

        if (isPaused)
        {
            Resume();
            return;
        }

        Pause();
    }

    public void Pause()
    {
        if (IsGameOver() || isPaused)
        {
            return;
        }

        isPaused = true;
        Time.timeScale = 0f;
        SetPausePanelVisible(true);
    }

    public void Resume()
    {
        if (!isPaused || IsGameOver())
        {
            return;
        }

        isPaused = false;
        Time.timeScale = 1f;
        PlayerAttack.BlockInputForSeconds(resumeInputLockSeconds);
        SetPausePanelVisible(false);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        GameLaunchFlow.ReturnToMainMenu(mainMenuSceneName);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        Debug.Log("Quit requested from pause menu.", this);
#else
        Application.Quit();
#endif
    }

    private bool IsGameOver()
    {
        return gameStateManager != null && gameStateManager.IsGameOver;
    }

    private void SetPausePanelVisible(bool isVisible)
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(isVisible);
        }
    }

    private void ConfigureUi()
    {
        ConfigurePausePanel();

        if (titleText != null)
        {
            titleText.gameObject.SetActive(true);
            titleText.text = "PAUSED";
            ConfigureTextRect(titleText.rectTransform, new Vector2(0f, 140f), new Vector2(360f, 70f));
        }

        ConfigureButton(resumeButton, "Resume", new Vector2(0f, 45f), buttonSize, Resume);
        ConfigureButton(mainMenuButton, "Quit main menu", new Vector2(0f, -35f), buttonSize, LoadMainMenu);
        SetButtonVisible(restartButton, false);
        SetButtonVisible(quitButton, false);
    }

    private void ConfigurePausePanel()
    {
        if (pausePanel == null)
        {
            return;
        }

        Image panelImage = pausePanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = overlayColor;
            panelImage.raycastTarget = true;
        }
    }

    private void MoveControlsIntoPausePanel()
    {
        if (pausePanel == null)
        {
            return;
        }

        Transform panelTransform = pausePanel.transform;
        MoveUnderPanel(resumeButton, panelTransform);
        MoveUnderPanel(restartButton, panelTransform);
        MoveUnderPanel(mainMenuButton, panelTransform);
        MoveUnderPanel(quitButton, panelTransform);
    }

    private static void MoveUnderPanel(Button button, Transform panelTransform)
    {
        if (button == null || button.transform.parent == panelTransform)
        {
            return;
        }

        button.transform.SetParent(panelTransform, false);
    }

    private static void ConfigureButton(Button button, string label, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.gameObject.SetActive(true);
        ConfigureTextRect(button.GetComponent<RectTransform>(), anchoredPosition, size);

        Text buttonText = button.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.gameObject.SetActive(true);
            buttonText.text = label;
            buttonText.fontSize = 24;
            buttonText.alignment = TextAnchor.MiddleCenter;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void SetButtonVisible(Button button, bool isVisible)
    {
        if (button != null)
        {
            button.gameObject.SetActive(isVisible);
        }
    }

    private static void ConfigureTextRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
    }
}
