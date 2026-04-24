using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);
    [SerializeField] private Text titleText;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private Image controlsBackgroundImage;

    [Header("Main Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button quitButton;

    [Header("Controls UI")]
    [SerializeField] private Text controlsText;
    [SerializeField] private Button backButton;

    private const string ControlsDescription =
        "Artifact Defense\n"
        + "Protect the crystal from enemy waves.\n\n"
        + "WASD - Move\n"
        + "Space / LMB - Primary Attack\n"
        + "Right Shift / RMB - Secondary Attack\n"
        + "E - Hero Special\n"
        + "Q - Crystal Shield\n"
        + "Esc - Pause\n"
        + "R - Restart after win/lose";

    private void Awake()
    {
        Time.timeScale = 1f;
        ConfigureUi();
        HideControls();
    }

    public void StartGame()
    {
        GameLaunchFlow.StartGame(gameSceneName);
    }

    public void Play()
    {
        StartGame();
    }

    public void ShowControls()
    {
        SetMainButtonsVisible(false);
        SetControlsVisible(true);
    }

    public void HideControls()
    {
        SetControlsVisible(false);
        SetMainButtonsVisible(true);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        Debug.Log("Quit requested from main menu.", this);
#else
        Application.Quit();
#endif
    }

    private void SetControlsVisible(bool isVisible)
    {
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(isVisible);
        }
    }

    private void ConfigureUi()
    {
        ConfigureBackground();
        HideUnusedPlaceholderTexts();

        if (titleText != null)
        {
            titleText.gameObject.SetActive(true);
            titleText.text = "ARTIFACT DEFENSE";
            titleText.color = Color.white;
            titleText.fontSize = 42;
            titleText.alignment = TextAnchor.MiddleCenter;
            ConfigureRect(titleText.rectTransform, new Vector2(0f, 120f), new Vector2(520f, 70f));
        }

        ConfigureButton(startGameButton, "StartGame", new Vector2(0f, 30f), StartGame);
        ConfigureButton(controlsButton, "Controls", new Vector2(0f, -20f), ShowControls);
        ConfigureButton(backButton, "Back", new Vector2(0f, -180f), HideControls);
        SetButtonVisible(quitButton, false);

        if (controlsText != null)
        {
            controlsText.text = ControlsDescription;
            controlsText.color = Color.white;
            controlsText.fontSize = 24;
            controlsText.alignment = TextAnchor.MiddleCenter;
            controlsText.horizontalOverflow = HorizontalWrapMode.Overflow;
            controlsText.verticalOverflow = VerticalWrapMode.Overflow;
            ConfigureRect(controlsText.rectTransform, new Vector2(0f, 35f), new Vector2(760f, 320f));
        }
    }

    private void ConfigureBackground()
    {
        if (backgroundImage == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                backgroundImage = canvas.GetComponent<Image>();
                if (backgroundImage == null)
                {
                    backgroundImage = canvas.gameObject.AddComponent<Image>();
                }
            }
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = backgroundColor;
            backgroundImage.raycastTarget = false;
        }

        if (controlsPanel != null)
        {
            if (controlsBackgroundImage == null)
            {
                controlsBackgroundImage = controlsPanel.GetComponent<Image>();
            }

            if (controlsBackgroundImage == null)
            {
                controlsBackgroundImage = controlsPanel.AddComponent<Image>();
            }

            controlsBackgroundImage.color = backgroundColor;
            controlsBackgroundImage.raycastTarget = true;

            RectTransform controlsRect = controlsPanel.GetComponent<RectTransform>();
            if (controlsRect != null)
            {
                controlsRect.anchorMin = Vector2.zero;
                controlsRect.anchorMax = Vector2.one;
                controlsRect.offsetMin = Vector2.zero;
                controlsRect.offsetMax = Vector2.zero;
            }
        }
    }

    private void SetMainButtonsVisible(bool isVisible)
    {
        SetTextVisible(titleText, isVisible);
        SetButtonVisible(startGameButton, isVisible);
        SetButtonVisible(controlsButton, isVisible);
    }

    private static void ConfigureButton(Button button, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.gameObject.SetActive(true);
        ConfigureRect(button.GetComponent<RectTransform>(), anchoredPosition, new Vector2(180f, 34f));

        Text buttonText = button.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.gameObject.SetActive(true);
            buttonText.text = label;
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

    private static void SetTextVisible(Text text, bool isVisible)
    {
        if (text != null)
        {
            text.gameObject.SetActive(isVisible);
        }
    }

    private void HideUnusedPlaceholderTexts()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        Text[] texts = canvas.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            Text text = texts[i];
            if (text == titleText
                || text == controlsText
                || IsButtonText(text, startGameButton)
                || IsButtonText(text, controlsButton)
                || IsButtonText(text, backButton))
            {
                continue;
            }

            if (text.text == "New Text" || text.text == "Quit")
            {
                text.gameObject.SetActive(false);
            }
        }
    }

    private static bool IsButtonText(Text text, Button button)
    {
        return button != null && text.transform.IsChildOf(button.transform);
    }

    private static void ConfigureRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
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
