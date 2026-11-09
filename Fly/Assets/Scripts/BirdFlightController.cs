using UnityEngine;
using UnityEngine.InputSystem;

public class BirdFlightController : MonoBehaviour
{
    [SerializeField] private float forwardSpeed = 8f;
    [SerializeField] private float turnSpeed = 95f;
    [SerializeField] private float pitchSpeed = 65f;
    [SerializeField] private float maxPitchAngle = 35f;
    [SerializeField] private float flapImpulse = 5f;
    [SerializeField] private float flapDecay = 9f;
    [SerializeField] private float flapCooldown = 0.7f;
    [SerializeField] private Transform visual;
    [SerializeField] private float bankAngle = 20f;

    private float pitchAngle;
    private float flapVelocity;
    private float nextFlapTime;

    private void Update()
    {
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
                flapVelocity = flapImpulse;
                nextFlapTime = Time.time + flapCooldown;
            }
        }

        transform.Rotate(0f, steer * turnSpeed * Time.deltaTime, 0f, Space.World);

        float desiredPitch = Mathf.Clamp(
            pitchAngle - pitch * pitchSpeed * Time.deltaTime,
            -maxPitchAngle,
            maxPitchAngle);
        transform.Rotate(desiredPitch - pitchAngle, 0f, 0f, Space.Self);
        pitchAngle = desiredPitch;

        transform.position += (transform.forward * forwardSpeed + Vector3.up * flapVelocity) * Time.deltaTime;
        flapVelocity = Mathf.MoveTowards(flapVelocity, 0f, flapDecay * Time.deltaTime);

        if (visual != null)
        {
            Quaternion bank = Quaternion.Euler(90f, 0f, -steer * bankAngle);
            visual.localRotation = Quaternion.Slerp(
                visual.localRotation,
                bank,
                1f - Mathf.Exp(-8f * Time.deltaTime));
        }
    }

    public void SetVisual(Transform birdVisual)
    {
        visual = birdVisual;
    }
}
