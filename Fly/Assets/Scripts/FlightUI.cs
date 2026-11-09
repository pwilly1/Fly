using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class FlightUI : MonoBehaviour
{
    private static readonly Color Ink = new Color(0.15f, 0.33f, 0.40f);
    private static readonly Color MutedInk = new Color(0.31f, 0.48f, 0.52f);
    private static readonly Color Ivory = new Color(1f, 0.98f, 0.91f);

    private GameObject hudRoot;
    private GameObject resultsRoot;
    private RectTransform progressBadge;
    private RectTransform resultCard;
    private CanvasGroup resultsGroup;
    private Text ringValue;
    private Text timerValue;
    private Text medalValue;
    private Text finalTimeValue;
    private Text thresholdValue;
    private Image medalImage;
    private Action restartAction;
    private int displayedRing;
    private float progressChangedAt;
    private float resultsShownAt;

    private void Awake()
    {
        BuildInterface();
    }

    private void Update()
    {
        if (hudRoot != null && hudRoot.activeSelf)
        {
            float elapsed = Time.unscaledTime - progressChangedAt;
            float bump = Mathf.Clamp01(1f - elapsed / 0.35f);
            progressBadge.localScale = Vector3.one * (1f + 0.09f * bump * bump);
        }

        if (resultsRoot != null && resultsRoot.activeSelf)
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
        if (hudRoot == null) return;

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
        if (resultsRoot == null) return;

        hudRoot.SetActive(false);
        resultsRoot.SetActive(true);
        medalValue.text = $"{medal.ToUpperInvariant()} MEDAL";
        medalImage.sprite = LoadSprite($"medal_{medal.ToLowerInvariant()}");
        finalTimeValue.text = $"{finalTimeSeconds:F1} SECONDS";
        thresholdValue.text = $"GOLD  {goldTimeSeconds:F0}s OR LESS     SILVER  {silverTimeSeconds:F0}s OR LESS";
        resultsShownAt = Time.unscaledTime;
        resultsGroup.alpha = 0f;
        resultCard.localScale = Vector3.one * 0.82f;
    }

    private void BuildInterface()
    {
        GameObject canvasObject = new GameObject("Fly UI Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        EnsureEventSystem();
        hudRoot = CreateFullScreen(canvas.transform, "Flight HUD").gameObject;
        BuildHud();
        resultsRoot = CreateFullScreen(canvas.transform, "Results Screen").gameObject;
        BuildResults();
        resultsRoot.SetActive(false);
    }

    private void BuildHud()
    {
        progressBadge = CreateImage(hudRoot.transform, "Next Ring Cloud", "cloud_progress",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -22f), new Vector2(450f, 135f));
        CreateImage(progressBadge, "Star", "star", Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(-165f, 0f), new Vector2(62f, 62f));
        CreateText(progressBadge, "Next Ring Label", "NEXT RING", 26, MutedInk,
            new Vector2(-16f, 7f), new Vector2(270f, 36f));
        ringValue = CreateText(progressBadge, "Ring Number", "1 OF 3", 42, Ink,
            new Vector2(-16f, -30f), new Vector2(270f, 58f));

        RectTransform timerBadge = CreateImage(hudRoot.transform, "Timer Cloud", "cloud_progress",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -22f), new Vector2(320f, 135f));
        CreateImage(timerBadge, "Stopwatch", "stopwatch", Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(-105f, 0f), new Vector2(62f, 62f));
        CreateText(timerBadge, "Timer Label", "FLIGHT TIME", 23, MutedInk,
            new Vector2(23f, 7f), new Vector2(205f, 36f));
        timerValue = CreateText(timerBadge, "Timer Value", "0.0s", 42, Ink,
            new Vector2(23f, -30f), new Vector2(205f, 58f));

        RectTransform help = CreateImage(hudRoot.transform, "Controls Cloud", "cloud_progress",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(530f, 90f));
        CreateText(help, "Controls", "A / D STEER     W / S CLIMB     SPACE FLAP\nR RESTART", 20, Ink,
            new Vector2(0f, -5f), new Vector2(490f, 45f));
    }

    private void BuildResults()
    {
        resultsGroup = resultsRoot.AddComponent<CanvasGroup>();
        Image shade = resultsRoot.AddComponent<Image>();
        shade.color = new Color(0.10f, 0.26f, 0.33f, 0.50f);
        shade.raycastTarget = true;

        resultCard = CreateImage(resultsRoot.transform, "Cloud Results Card", "cloud_results",
            Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(560f, 516f));
        CreateImage(resultCard, "Left Star", "star", Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(-252f, 170f), new Vector2(66f, 66f));
        CreateImage(resultCard, "Right Star", "star", Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(252f, 148f), new Vector2(48f, 48f));
        CreateText(resultCard, "Finish Title", "FLIGHT COMPLETE!", 39, Ink,
            new Vector2(0f, 190f), new Vector2(480f, 56f));
        medalImage = CreateImage(resultCard, "Medal", "medal_gold", Vector2.one * 0.5f,
            Vector2.one * 0.5f, new Vector2(0f, 27f), new Vector2(120f, 136f)).GetComponent<Image>();
        medalValue = CreateText(resultCard, "Medal Label", "GOLD MEDAL", 32, Ink,
            new Vector2(0f, -73f), new Vector2(430f, 48f));
        finalTimeValue = CreateText(resultCard, "Final Time", "0.0 SECONDS", 28, Ink,
            new Vector2(0f, -116f), new Vector2(430f, 40f));
        thresholdValue = CreateText(resultCard, "Medal Times", "", 20, MutedInk,
            new Vector2(0f, -151f), new Vector2(480f, 35f));

        RectTransform buttonRect = CreateImage(resultCard, "Try Again Button", "retry_button",
            Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, -213f), new Vector2(290f, 82f));
        Image buttonImage = buttonRect.GetComponent<Image>();
        buttonImage.raycastTarget = true;
        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(() => restartAction?.Invoke());
        CreateText(buttonRect, "Try Again Text", "TRY AGAIN  (R)", 26, Ivory,
            Vector2.zero, new Vector2(260f, 60f));
    }

    private static RectTransform CreateFullScreen(Transform parent, string name)
    {
        GameObject item = new GameObject(name, typeof(RectTransform));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static RectTransform CreateImage(Transform parent, string name, string spriteName,
        Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = item.GetComponent<Image>();
        image.sprite = LoadSprite(spriteName);
        image.raycastTarget = false;
        return rect;
    }

    private static Text CreateText(Transform parent, string name, string content, int fontSize,
        Color color, Vector2 position, Vector2 size)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(Text));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.one * 0.5f;
        rect.anchorMax = Vector2.one * 0.5f;
        rect.pivot = Vector2.one * 0.5f;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Text label = item.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = content;
        label.fontSize = fontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private static Sprite LoadSprite(string name)
    {
        Sprite sprite = Resources.Load<Sprite>($"UI/{name}");
        if (sprite == null)
            Debug.LogError($"Missing Fly UI artwork: {name}");
        return sprite;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        GameObject eventSystem = new GameObject("Fly UI Event System", typeof(EventSystem));
        InputSystemUIInputModule inputModule = eventSystem.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
    }
}
