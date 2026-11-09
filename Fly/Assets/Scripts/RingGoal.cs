using UnityEngine;
using UnityEngine.InputSystem;

public class RingGoal : MonoBehaviour
{
    [SerializeField] private Transform bird;
    [SerializeField] private Transform[] rings;
    [SerializeField] private FlightUI flightUI;
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
        if (flightUI == null) flightUI = GetComponent<FlightUI>();
        if (flightUI != null)
        {
            flightUI.SetRestartAction(RestartCourse);
            flightUI.ShowProgress(1, rings.Length, 0f);
        }
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
            RestartCourse();
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
                string medal = finishTimeSeconds <= goldTimeSeconds ? "Gold"
                    : finishTimeSeconds <= silverTimeSeconds ? "Silver" : "Bronze";
                if (flightUI != null)
                    flightUI.ShowResults(finishTimeSeconds, medal, goldTimeSeconds, silverTimeSeconds);
            }
        }

        previousBirdPosition = currentPosition;
        if (nextRingIndex < rings.Length && flightUI != null)
            flightUI.ShowProgress(nextRingIndex + 1, rings.Length, Time.time - runStartedAt);
    }

    public void RestartCourse()
    {
        if (bird == null) return;

        if (flightController != null) flightController.ResetFlight();
        nextRingIndex = 0;
        runStartedAt = Time.time;
        finishTimeSeconds = 0f;
        previousBirdPosition = bird.position;
        if (flightUI != null) flightUI.ShowProgress(1, rings.Length, 0f);
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

}
