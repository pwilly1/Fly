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
    [SerializeField] private float resetPenaltySeconds = 3f;
    [SerializeField] private float collisionRadius = 0.4f;
    [SerializeField] private Vector3 courseMin = new Vector3(-50f, 1f, -15f);
    [SerializeField] private Vector3 courseMax = new Vector3(50f, 35f, 100f);

    private readonly RaycastHit[] collisionHits = new RaycastHit[16];
    private Vector3 previousBirdPosition;
    private Vector3 checkpointPosition;
    private Quaternion checkpointRotation;
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
            checkpointPosition = bird.position;
            checkpointRotation = bird.rotation;
            flightController = bird.GetComponent<BirdFlightController>();
        }
    }

    private void LateUpdate()
    {
        if (bird == null || rings == null || rings.Length == 0) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            if (nextRingIndex == rings.Length) RestartCourse();
            else RespawnAtCheckpoint();
            return;
        }

        Vector3 currentPosition = bird.position;
        if (nextRingIndex < rings.Length && (OutsideCourse(currentPosition) || HitObstacle(currentPosition)))
        {
            RespawnAtCheckpoint();
            return;
        }

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
            else
            {
                Transform clearedRing = rings[nextRingIndex - 1];
                checkpointPosition = clearedRing.position + clearedRing.forward * 2.5f;
                Vector3 towardNextRing = rings[nextRingIndex].position - checkpointPosition;
                checkpointRotation = Quaternion.LookRotation(towardNextRing.normalized, Vector3.up);
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
        checkpointPosition = bird.position;
        checkpointRotation = bird.rotation;
        runStartedAt = Time.time;
        finishTimeSeconds = 0f;
        previousBirdPosition = bird.position;
        if (flightUI != null)
        {
            flightUI.ClearRecoveryMessage();
            flightUI.ShowProgress(1, rings.Length, 0f);
        }
    }

    private void RespawnAtCheckpoint()
    {
        if (flightController != null) flightController.RespawnAt(checkpointPosition, checkpointRotation);
        else bird.SetPositionAndRotation(checkpointPosition, checkpointRotation);

        runStartedAt -= resetPenaltySeconds;
        previousBirdPosition = bird.position;
        if (flightUI != null)
        {
            flightUI.ShowProgress(nextRingIndex + 1, rings.Length, Time.time - runStartedAt);
            flightUI.ShowRecovery(resetPenaltySeconds);
        }
    }

    private bool OutsideCourse(Vector3 position)
    {
        return position.x < courseMin.x || position.x > courseMax.x
            || position.y < courseMin.y || position.y > courseMax.y
            || position.z < courseMin.z || position.z > courseMax.z;
    }

    private bool HitObstacle(Vector3 currentPosition)
    {
        Vector3 travel = currentPosition - previousBirdPosition;
        float distance = travel.magnitude;
        if (distance <= 0f) return false;

        int hitCount = Physics.SphereCastNonAlloc(previousBirdPosition, collisionRadius,
            travel / distance, collisionHits, distance, Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = collisionHits[i].collider;
            if (hit != null && !hit.transform.IsChildOf(bird)) return true;
        }
        return false;
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
