using UnityEngine;
using UnityEngine.InputSystem;

public class RingGoal : MonoBehaviour
{
    [SerializeField] private Transform bird;
    [SerializeField] private Transform[] rings;
    [SerializeField] private float clearRadius = 3f;
    [SerializeField] private float goldTimeSeconds = 13f;
    [SerializeField] private float silverTimeSeconds = 18f;

    private Vector3 previousBirdPosition;
    private BirdFlightController flightController;
    private int nextRingIndex;
    private float runStartedAt;
    private float finishTimeSeconds;

    private void Start()
    {
        runStartedAt = Time.time;
        if (bird != null)
        {
            previousBirdPosition = bird.position;
            flightController = bird.GetComponent<BirdFlightController>();
        }
    }

    private void LateUpdate()
    {
        if (bird == null || rings == null || rings.Length == 0) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            if (flightController != null) flightController.ResetFlight();
            nextRingIndex = 0;
            runStartedAt = Time.time;
            finishTimeSeconds = 0f;
            previousBirdPosition = bird.position;
            return;
        }

        Vector3 currentPosition = bird.position;
        if (nextRingIndex < rings.Length && CrossedRing(rings[nextRingIndex], currentPosition))
        {
            nextRingIndex++;
            if (nextRingIndex == rings.Length)
            {
                finishTimeSeconds = Time.time - runStartedAt;
                if (flightController != null) flightController.StopFlight();
            }
        }

        previousBirdPosition = currentPosition;
    }

    private bool CrossedRing(Transform ring, Vector3 currentPosition)
    {
        if (ring == null) return false;

        float previousDistance = Vector3.Dot(previousBirdPosition - ring.position, ring.forward);
        float currentDistance = Vector3.Dot(currentPosition - ring.position, ring.forward);

        if (previousDistance < 0f && currentDistance >= 0f)
        {
            float crossingFraction = -previousDistance / (currentDistance - previousDistance);
            Vector3 crossingPoint = Vector3.Lerp(previousBirdPosition, currentPosition, crossingFraction);
            Vector3 localPoint = ring.InverseTransformPoint(crossingPoint);

            if (new Vector2(localPoint.x, localPoint.y).sqrMagnitude <= clearRadius * clearRadius)
                return true;
        }

        return false;
    }

    private void OnGUI()
    {
        if (rings == null || rings.Length == 0) return;

        if (nextRingIndex == rings.Length)
        {
            DrawResults();
            return;
        }

        GUIStyle messageStyle = new GUIStyle(GUI.skin.box);
        messageStyle.alignment = TextAnchor.MiddleCenter;
        messageStyle.fontSize = Mathf.Clamp(Screen.height / 35, 16, 22);
        messageStyle.normal.textColor = Color.white;

        float elapsedSeconds = Time.time - runStartedAt;
        string message = $"Next: Ring {nextRingIndex + 1} of {rings.Length}\nTime: {elapsedSeconds:F1}s  |  R to restart";
        float width = Mathf.Min(Screen.width - 24f, 540f);
        GUI.Box(new Rect((Screen.width - width) / 2f, 18f, width, 70f), message, messageStyle);
    }

    private void DrawResults()
    {
        float width = Mathf.Min(Screen.width - 32f, 460f);
        Rect panel = new Rect((Screen.width - width) / 2f, (Screen.height - 210f) / 2f, width, 210f);
        GUI.Box(panel, "");

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.fontSize = 26;
        titleStyle.normal.textColor = Color.white;

        GUIStyle medalStyle = new GUIStyle(titleStyle);
        medalStyle.fontSize = 32;
        string medal;
        if (finishTimeSeconds <= goldTimeSeconds)
        {
            medal = "GOLD MEDAL";
            medalStyle.normal.textColor = new Color(1f, 0.82f, 0.2f);
        }
        else if (finishTimeSeconds <= silverTimeSeconds)
        {
            medal = "SILVER MEDAL";
            medalStyle.normal.textColor = new Color(0.83f, 0.88f, 0.95f);
        }
        else
        {
            medal = "BRONZE MEDAL";
            medalStyle.normal.textColor = new Color(0.8f, 0.5f, 0.25f);
        }

        GUIStyle detailStyle = new GUIStyle(titleStyle);
        detailStyle.fontSize = 18;

        GUI.Label(new Rect(panel.x, panel.y + 12f, width, 36f), "Course complete!", titleStyle);
        GUI.Label(new Rect(panel.x, panel.y + 52f, width, 44f), medal, medalStyle);
        GUI.Label(new Rect(panel.x, panel.y + 100f, width, 28f), $"Final time: {finishTimeSeconds:F1}s", detailStyle);
        GUI.Label(new Rect(panel.x, panel.y + 132f, width, 28f), $"Gold <= {goldTimeSeconds:F0}s   Silver <= {silverTimeSeconds:F0}s", detailStyle);
        GUI.Label(new Rect(panel.x, panel.y + 168f, width, 28f), "Press R to try again", detailStyle);
    }
}
