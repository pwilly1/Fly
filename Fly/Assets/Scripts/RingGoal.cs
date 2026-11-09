using UnityEngine;
using UnityEngine.InputSystem;

public class RingGoal : MonoBehaviour
{
    [SerializeField] private Transform bird;
    [SerializeField] private Transform[] rings;
    [SerializeField] private float clearRadius = 3f;

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
                finishTimeSeconds = Time.time - runStartedAt;
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

        GUIStyle messageStyle = new GUIStyle(GUI.skin.box);
        messageStyle.alignment = TextAnchor.MiddleCenter;
        messageStyle.fontSize = Mathf.Clamp(Screen.height / 35, 16, 22);
        messageStyle.normal.textColor = Color.white;

        bool finished = nextRingIndex == rings.Length;
        float elapsedSeconds = finished ? finishTimeSeconds : Time.time - runStartedAt;
        string status = finished
            ? "Course complete!"
            : $"Ring {nextRingIndex + 1}/{rings.Length} - fly through it";
        string message = $"{status}\nTime: {elapsedSeconds:F1}s  |  R to restart";
        float width = Mathf.Min(Screen.width - 24f, 540f);
        GUI.Box(new Rect((Screen.width - width) / 2f, 18f, width, 70f), message, messageStyle);
    }
}
