using UnityEngine;

public class RingGoal : MonoBehaviour
{
    [SerializeField] private Transform bird;
    [SerializeField] private float clearRadius = 3f;

    private Vector3 previousBirdPosition;
    private bool cleared;

    private void Start()
    {
        if (bird != null)
            previousBirdPosition = bird.position;
    }

    private void LateUpdate()
    {
        if (bird == null || cleared) return;

        Vector3 currentPosition = bird.position;
        float previousDistance = Vector3.Dot(previousBirdPosition - transform.position, transform.forward);
        float currentDistance = Vector3.Dot(currentPosition - transform.position, transform.forward);

        if (previousDistance < 0f && currentDistance >= 0f)
        {
            float crossingFraction = -previousDistance / (currentDistance - previousDistance);
            Vector3 crossingPoint = Vector3.Lerp(previousBirdPosition, currentPosition, crossingFraction);
            Vector3 localPoint = transform.InverseTransformPoint(crossingPoint);

            if (new Vector2(localPoint.x, localPoint.y).sqrMagnitude <= clearRadius * clearRadius)
                cleared = true;
        }

        previousBirdPosition = currentPosition;
    }

    private void OnGUI()
    {
        GUIStyle messageStyle = new GUIStyle(GUI.skin.box);
        messageStyle.alignment = TextAnchor.MiddleCenter;
        messageStyle.fontSize = Mathf.Clamp(Screen.height / 28, 18, 30);
        messageStyle.normal.textColor = Color.white;

        string message = cleared ? "Ring cleared!" : "Fly through the gold ring";
        GUI.Box(new Rect(Screen.width / 2f - 190f, 18f, 380f, 54f), message, messageStyle);
    }
}
