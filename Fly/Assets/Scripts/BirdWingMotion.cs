using UnityEngine;

public class BirdWingMotion : MonoBehaviour
{
    [SerializeField] private Transform leftWing;
    [SerializeField] private Transform rightWing;
    [SerializeField] private float glideSwayDegrees = 4f;
    [SerializeField] private float flapDegrees = 48f;
    [SerializeField] private float flapDuration = 0.48f;

    private float flapStartedAt = float.NegativeInfinity;

    private void LateUpdate()
    {
        if (leftWing == null || rightWing == null) return;

        float glide = Mathf.Sin(Time.time * 2.6f) * glideSwayDegrees;
        float progress = (Time.time - flapStartedAt) / flapDuration;
        float flap = progress >= 0f && progress <= 1f
            ? Mathf.Sin(progress * Mathf.PI) * flapDegrees : 0f;
        float angle = 9f + glide + flap;

        leftWing.localRotation = Quaternion.Euler(0f, 0f, angle);
        rightWing.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }

    public void Flap()
    {
        flapStartedAt = Time.time;
    }

    public void ResetMotion()
    {
        flapStartedAt = float.NegativeInfinity;
    }
}
