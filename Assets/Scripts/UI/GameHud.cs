using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameHud : MonoBehaviour
{
    [Header("Style")]
    [SerializeField] private Color textColor = Color.white;

    [Header("Sources")]
    [SerializeField] private ArtifactHealth artifactHealth;
    [SerializeField] private ArtifactEnergy artifactEnergy;
    [SerializeField] private ArtifactShieldAbility artifactShieldAbility;
    [SerializeField] private WaveManager waveManager;

    [Header("Health UI")]
    [SerializeField] private Text artifactHealthText;
    [SerializeField] private Slider artifactHealthSlider;

    [Header("Wave UI")]
    [SerializeField] private Text waveText;
    [SerializeField] private Text waveStatusText;

    [Header("Energy UI")]
    [SerializeField] private Text energyText;
    [SerializeField] private Slider energySlider;
    [SerializeField] private Text specialReadyText;

    [Header("Shield UI")]
    [SerializeField] private Text shieldStatusText;

    private void Awake()
    {
        ResolveMissingSources();
        EnsureWaveStatusText();
        ApplyTextStyle();
    }

    private void Update()
    {
        UpdateHealth();
        UpdateWave();
        UpdateEnergy();
        UpdateShield();
    }

    private void UpdateHealth()
    {
        if (artifactHealth == null)
        {
            return;
        }

        if (artifactHealthText != null)
        {
            artifactHealthText.text = $"Crystal HP: {artifactHealth.CurrentHealth} / {artifactHealth.MaxHealth}";
        }

        if (artifactHealthSlider != null)
        {
            artifactHealthSlider.maxValue = artifactHealth.MaxHealth;
            artifactHealthSlider.value = artifactHealth.CurrentHealth;
        }
    }

    private void UpdateWave()
    {
        if (waveManager == null || (waveText == null && waveStatusText == null))
        {
            return;
        }

        string waveLine = $"Wave: {waveManager.CurrentWaveNumber} / {waveManager.TotalWaves}";
        string statusLine = GetWaveStatus();

        if (waveStatusText != null)
        {
            if (waveText != null)
            {
                waveText.text = waveLine;
            }

            waveStatusText.text = statusLine;
            return;
        }

        waveText.text = string.IsNullOrEmpty(statusLine)
            ? waveLine
            : $"{waveLine}\n{statusLine}";
    }

    private string GetWaveStatus()
    {
        if (waveManager.IsFinalVictoryPending)
        {
            return "Wave completed";
        }

        if (!waveManager.IsBetweenWaves)
        {
            return string.Empty;
        }

        int seconds = Mathf.CeilToInt(waveManager.NextWaveCountdown);
        if (waveManager.CurrentWaveNumber <= 0)
        {
            return $"Next wave in {seconds}...";
        }

        return $"Wave completed\nNext wave in {seconds}...";
    }

    private void UpdateEnergy()
    {
        if (artifactEnergy == null)
        {
            return;
        }

        if (energyText != null)
        {
            energyText.text = $"Energy: {artifactEnergy.CurrentEnergy} / {artifactEnergy.MaxEnergy}";
        }

        if (energySlider != null)
        {
            energySlider.maxValue = artifactEnergy.MaxEnergy;
            energySlider.value = artifactEnergy.CurrentEnergy;
        }

        if (specialReadyText != null)
        {
            specialReadyText.text = artifactEnergy.IsFull ? "SPECIAL READY [E]" : "Special charging...";
        }
    }

    private void UpdateShield()
    {
        if (artifactShieldAbility == null)
        {
            ResolveMissingSources();
        }

        string shieldStatus = artifactShieldAbility != null
            ? GetShieldStatus()
            : "Shield: not linked";

        if (artifactShieldAbility == null)
        {
            WriteShieldStatus(shieldStatus);
            return;
        }

        WriteShieldStatus(shieldStatus);
    }

    private void WriteShieldStatus(string shieldStatus)
    {
        if (shieldStatusText != null)
        {
            shieldStatusText.text = shieldStatus;
            return;
        }

        if (specialReadyText != null)
        {
            specialReadyText.text = $"{specialReadyText.text}\n{shieldStatus}";
        }
    }

    private string GetShieldStatus()
    {
        if (artifactShieldAbility.IsOnCooldown)
        {
            int cooldownSeconds = Mathf.CeilToInt(artifactShieldAbility.CooldownRemaining);
            return $"Shield cooldown: {cooldownSeconds}s";
        }

        if (artifactShieldAbility.IsReady)
        {
            return "SHIELD READY [Q]";
        }

        if (artifactShieldAbility.IsUnderDamagePressure)
        {
            int secondsRemaining = Mathf.CeilToInt(artifactShieldAbility.SustainedDamageRemaining);
            return $"Shield charging: {secondsRemaining}s";
        }

        return "Shield: waiting for sustained damage";
    }

    private void ResolveMissingSources()
    {
        if (artifactShieldAbility != null)
        {
            return;
        }

        if (artifactHealth != null)
        {
            artifactShieldAbility = artifactHealth.GetComponent<ArtifactShieldAbility>();
        }

        if (artifactShieldAbility == null)
        {
            artifactShieldAbility = FindAnyObjectByType<ArtifactShieldAbility>();
        }
    }

    private void ApplyTextStyle()
    {
        ApplyTextColor(artifactHealthText);
        ApplyTextColor(waveText);
        ApplyTextColor(waveStatusText);
        ApplyTextColor(energyText);
        ApplyTextColor(specialReadyText);
        ApplyTextColor(shieldStatusText);
        ApplyWaveStatusTextStyle();
    }

    private void ApplyTextColor(Text text)
    {
        if (text != null)
        {
            text.color = textColor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }
    }

    private void EnsureWaveStatusText()
    {
        if (waveStatusText == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            GameObject statusObject = new GameObject("WaveStatusText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            statusObject.layer = canvas.gameObject.layer;
            statusObject.transform.SetParent(canvas.transform, false);
            waveStatusText = statusObject.GetComponent<Text>();
        }

        ApplyWaveStatusTextStyle();
    }

    private void ApplyWaveStatusTextStyle()
    {
        if (waveStatusText == null)
        {
            return;
        }

        waveStatusText.color = textColor;
        waveStatusText.fontSize = 42;
        waveStatusText.fontStyle = FontStyle.Bold;
        waveStatusText.alignment = TextAnchor.MiddleCenter;
        waveStatusText.raycastTarget = false;
        waveStatusText.horizontalOverflow = HorizontalWrapMode.Wrap;
        waveStatusText.verticalOverflow = VerticalWrapMode.Overflow;

        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform rectTransform = waveStatusText.rectTransform;
        if (canvas != null && rectTransform.parent != canvas.transform)
        {
            rectTransform.SetParent(canvas.transform, false);
        }

        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0f, -90f);
        rectTransform.sizeDelta = new Vector2(900f, 100f);
    }
}
