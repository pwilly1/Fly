using UnityEngine;
using UnityEngine.InputSystem;

public class BirdFlightController : MonoBehaviour
{
    [SerializeField] private float forwardSpeed = 8f;
    [SerializeField] private float turnSpeed = 95f;
    [SerializeField] private Transform visual;
    [SerializeField] private float bankAngle = 20f;

    private void Update()
    {
        float steer = 0f;
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed) steer -= 1f;
            if (keyboard.dKey.isPressed) steer += 1f;
        }

        transform.Rotate(0f, steer * turnSpeed * Time.deltaTime, 0f, Space.Self);
        transform.position += transform.forward * (forwardSpeed * Time.deltaTime);

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
