using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class EndGameOverlayUI : MonoBehaviour
{
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Text titleText;
    [FormerlySerializedAs("messageText")]
    [SerializeField] private Text subtitleText;
    [SerializeField] private Text restartText;

    private void Awake()
    {
        ConfigureVisuals();
        Hide();
    }

    public void ShowVictory()
    {
        Show("VICTORY", "All waves completed", "Press R to restart");
    }

    public void ShowDefeat()
    {
        Show("DEFEAT", "The crystal has been destroyed", "Press R to restart");
    }

    public void Hide()
    {
        SetVisible(false);
    }

    private void Show(string title, string message, string restartPrompt)
    {
        ConfigureVisuals();

        if (titleText != null)
        {
            titleText.text = title;
        }

        if (subtitleText != null)
        {
            subtitleText.text = message;
        }

        if (restartText != null)
        {
            restartText.text = restartPrompt;
        }

        SetVisible(true);
    }

    private void SetVisible(bool isVisible)
    {
        GameObject target = overlayRoot != null ? overlayRoot : gameObject;
        target.SetActive(isVisible);
    }

    private void ConfigureVisuals()
    {
        GameObject target = overlayRoot != null ? overlayRoot : gameObject;

        if (target.TryGetComponent(out RectTransform panelRect))
        {
            StretchToFullscreen(panelRect);
        }

        if (backgroundImage == null)
        {
            backgroundImage = target.GetComponent<Image>();
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(0f, 0f, 0f, 0.72f);
            backgroundImage.raycastTarget = true;
        }

        ConfigureText(titleText, 72, new Vector2(0f, 90f), new Vector2(900f, 110f));
        ConfigureText(subtitleText, 32, new Vector2(0f, 10f), new Vector2(900f, 60f));
        ConfigureText(restartText, 28, new Vector2(0f, -60f), new Vector2(900f, 50f));
    }

    private static void StretchToFullscreen(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void ConfigureText(Text text, int fontSize, Vector2 anchoredPosition, Vector2 size)
    {
        if (text == null)
        {
            return;
        }

        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        RectTransform rectTransform = text.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
    }
}
