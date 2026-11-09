using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class FlightUI : MonoBehaviour
{
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private GameObject resultsRoot;
    [SerializeField] private RectTransform progressBadge;
    [SerializeField] private RectTransform resultCard;
    [SerializeField] private CanvasGroup resultsGroup;
    [SerializeField] private Text ringValue;
    [SerializeField] private Text timerValue;
    [SerializeField] private Text medalValue;
    [SerializeField] private Text finalTimeValue;
    [SerializeField] private Text thresholdValue;
    [SerializeField] private Image medalImage;
    [SerializeField] private Sprite goldMedal;
    [SerializeField] private Sprite silverMedal;
    [SerializeField] private Sprite bronzeMedal;
    [SerializeField] private Button retryButton;

    private Action restartAction;
    private int displayedRing;
    private float progressChangedAt;
    private float resultsShownAt;

    private void Awake()
    {
        retryButton.onClick.AddListener(OnRetryClicked);
        resultsRoot.SetActive(false);
        EnsureEventSystem();
    }

    private void OnDestroy()
    {
        if (retryButton != null) retryButton.onClick.RemoveListener(OnRetryClicked);
    }

    private void Update()
    {
        if (hudRoot.activeSelf)
        {
            float elapsed = Time.unscaledTime - progressChangedAt;
            float bump = Mathf.Clamp01(1f - elapsed / 0.35f);
            progressBadge.localScale = Vector3.one * (1f + 0.09f * bump * bump);
        }

        if (resultsRoot.activeSelf)
        {
            float t = Mathf.Clamp01((Time.unscaledTime - resultsShownAt) / 0.45f);
            resultsGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
            resultCard.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, Mathf.SmoothStep(0f, 1f, t));
        }
    }

    public void SetRestartAction(Action action)
    {
        restartAction = action;
    }

    public void ShowProgress(int nextRing, int totalRings, float elapsedSeconds)
    {
        resultsRoot.SetActive(false);
        hudRoot.SetActive(true);
        if (displayedRing != nextRing)
        {
            displayedRing = nextRing;
            progressChangedAt = Time.unscaledTime;
        }

        ringValue.text = $"{nextRing} OF {totalRings}";
        timerValue.text = $"{elapsedSeconds:F1}s";
    }

    public void ShowResults(float finalTimeSeconds, string medal, float goldTimeSeconds, float silverTimeSeconds)
    {
        hudRoot.SetActive(false);
        resultsRoot.SetActive(true);
        medalValue.text = $"{medal.ToUpperInvariant()} MEDAL";
        medalImage.sprite = medal == "Gold" ? goldMedal : medal == "Silver" ? silverMedal : bronzeMedal;
        finalTimeValue.text = $"{finalTimeSeconds:F1} SECONDS";
        thresholdValue.text = $"GOLD  {goldTimeSeconds:F0}s OR LESS     SILVER  {silverTimeSeconds:F0}s OR LESS";
        resultsShownAt = Time.unscaledTime;
        resultsGroup.alpha = 0f;
        resultCard.localScale = Vector3.one * 0.82f;
    }

    private void OnRetryClicked()
    {
        restartAction?.Invoke();
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        GameObject eventSystem = new GameObject("Fly UI Event System", typeof(EventSystem));
        InputSystemUIInputModule inputModule = eventSystem.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
    }
}
