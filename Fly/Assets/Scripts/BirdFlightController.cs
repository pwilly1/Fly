using UnityEngine;
using UnityEngine.InputSystem;

public class BirdFlightController : MonoBehaviour
{
    [Header("Steering")]
    [SerializeField] private float forwardSpeed = 8f;
    [SerializeField] private float turnSpeed = 95f;
    [SerializeField] private float turnAcceleration = 280f;
    [SerializeField] private float headingResponse = 7f;
    [SerializeField] private float pitchSpeed = 65f;
    [SerializeField] private float pitchInputResponse = 5f;
    [SerializeField] private float maxPitchAngle = 35f;

    [Header("Glide")]
    [SerializeField] private float speedAcceleration = 2.5f;
    [SerializeField] private float climbSpeedPenalty = 3f;
    [SerializeField] private float diveSpeedBonus = 3.5f;
    [SerializeField] private float glideSinkRate = 0.8f;
    [SerializeField] private float lowSpeedSinkMultiplier = 0.75f;
    [SerializeField] private float glideGravity = 5f;

    [Header("Flap")]
    [SerializeField] private float flapImpulse = 5f;
    [SerializeField] private float maxVerticalSpeed = 6.5f;
    [SerializeField] private float flapCooldown = 0.7f;

    [Header("Visual")]
    [SerializeField] private Transform visual;
    [SerializeField] private float bankAngle = 20f;

    private float pitchAngle;
    private float pitchInput;
    private float turnRate;
    private float currentForwardSpeed;
    private float verticalVelocity;
    private Vector3 travelDirection;
    private float nextFlapTime;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Quaternion startVisualRotation;
    private BirdWingMotion wingMotion;
    private Animator birdAnimator;
    private bool isFlying = true;

    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        currentForwardSpeed = forwardSpeed;
        travelDirection = transform.forward;
        if (visual != null) startVisualRotation = visual.localRotation;
        if (visual != null) wingMotion = visual.GetComponent<BirdWingMotion>();
        if (visual != null) birdAnimator = visual.GetComponent<Animator>();
        if (visual != null && visual.GetComponentInChildren<SkinnedMeshRenderer>() == null
            && visual.GetComponent<BirdMeshBinder>() == null)
            visual.gameObject.AddComponent<BirdMeshBinder>();
    }

    private void Update()
    {
        if (!isFlying) return;

        float steer = 0f;
        float pitch = 0f;
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed) steer -= 1f;
            if (keyboard.dKey.isPressed) steer += 1f;
            if (keyboard.wKey.isPressed) pitch += 1f;
            if (keyboard.sKey.isPressed) pitch -= 1f;

            if (keyboard.spaceKey.wasPressedThisFrame && Time.time >= nextFlapTime)
            {
                verticalVelocity = Mathf.Min(verticalVelocity + flapImpulse, maxVerticalSpeed);
                nextFlapTime = Time.time + flapCooldown;
                if (wingMotion != null) wingMotion.Flap();
                if (birdAnimator != null) birdAnimator.SetTrigger("Flap");
            }
        }

        float deltaTime = Time.deltaTime;
        turnRate = Mathf.MoveTowards(turnRate, steer * turnSpeed, turnAcceleration * deltaTime);
        transform.Rotate(0f, turnRate * deltaTime, 0f, Space.World);

        pitchInput = Mathf.MoveTowards(pitchInput, pitch, pitchInputResponse * deltaTime);
        float desiredPitch = Mathf.Clamp(
            pitchAngle - pitchInput * pitchSpeed * deltaTime,
            -maxPitchAngle,
            maxPitchAngle);
        transform.Rotate(desiredPitch - pitchAngle, 0f, 0f, Space.Self);
        pitchAngle = desiredPitch;

        float climb = Mathf.Max(0f, -pitchAngle / maxPitchAngle);
        float dive = Mathf.Max(0f, pitchAngle / maxPitchAngle);
        float targetSpeed = forwardSpeed - climb * climbSpeedPenalty + dive * diveSpeedBonus;
        currentForwardSpeed = Mathf.MoveTowards(
            currentForwardSpeed, targetSpeed, speedAcceleration * deltaTime);

        float sinkRate = Mathf.Clamp(
            glideSinkRate + (forwardSpeed - currentForwardSpeed) * lowSpeedSinkMultiplier,
            0.25f, 3.2f);
        verticalVelocity = Mathf.MoveTowards(
            verticalVelocity, -sinkRate, glideGravity * deltaTime);
        travelDirection = Vector3.Slerp(
            travelDirection, transform.forward,
            1f - Mathf.Exp(-headingResponse * deltaTime)).normalized;
        transform.position += (travelDirection * currentForwardSpeed
            + Vector3.up * verticalVelocity) * deltaTime;

        if (visual != null)
        {
            float bankAmount = turnRate / Mathf.Max(turnSpeed, 0.01f);
            Quaternion bank = Quaternion.Euler(0f, 0f, -bankAmount * bankAngle);
            visual.localRotation = Quaternion.Slerp(
                visual.localRotation,
                bank,
                1f - Mathf.Exp(-8f * Time.deltaTime));
        }
    }

    public void SetVisual(Transform birdVisual)
    {
        visual = birdVisual;
        wingMotion = visual != null ? visual.GetComponent<BirdWingMotion>() : null;
        birdAnimator = visual != null ? visual.GetComponent<Animator>() : null;
    }

    public void ResetFlight()
    {
        RespawnAt(startPosition, startRotation);
    }

    public void RespawnAt(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        pitchAngle = 0f;
        pitchInput = 0f;
        turnRate = 0f;
        currentForwardSpeed = forwardSpeed;
        verticalVelocity = 0f;
        travelDirection = transform.forward;
        nextFlapTime = 0f;
        if (wingMotion != null) wingMotion.ResetMotion();
        if (birdAnimator != null)
        {
            birdAnimator.ResetTrigger("Flap");
            birdAnimator.Play("Glide", 0, 0f);
        }
        if (visual != null) visual.localRotation = startVisualRotation;
        isFlying = true;
    }

    public void StopFlight()
    {
        isFlying = false;
        verticalVelocity = 0f;
    }
}
