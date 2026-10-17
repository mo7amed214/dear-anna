using System.Collections;
using UnityEngine;

// The real double door (AnimatedDoor asset) between the bedroom and the dream beyond.
// Always closed in normal play; the nightmare sequence (and later, whatever ends it)
// opens and closes it with a real swing. Each leaf's "closed" pose already carries a
// baked base rotation from the source asset -- opening rotates further around each
// leaf's own local Z on top of that base, never replacing it.
public class DreamDoor : MonoBehaviour
{
    public Transform leftDoor;
    public Transform rightDoor;
    public float openAngle = 100f;
    public float swingDuration = 1.3f;

    private Quaternion leftClosedRotation;
    private Quaternion rightClosedRotation;
    private bool isOpen;
    private Coroutine swingRoutine;

    void Awake()
    {
        // Capture whatever rotation each leaf has right now as "closed" and force it there --
        // the door must never start open just because of leftover editor/test state.
        if (leftDoor != null) leftClosedRotation = leftDoor.localRotation;
        if (rightDoor != null) rightClosedRotation = rightDoor.localRotation;
        isOpen = false;
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        if (swingRoutine != null) StopCoroutine(swingRoutine);
        swingRoutine = StartCoroutine(Swing(openAngle));
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        if (swingRoutine != null) StopCoroutine(swingRoutine);
        swingRoutine = StartCoroutine(Swing(0f));
    }

    IEnumerator Swing(float targetAngle)
    {
        Quaternion leftFrom = leftDoor != null ? leftDoor.localRotation : Quaternion.identity;
        Quaternion rightFrom = rightDoor != null ? rightDoor.localRotation : Quaternion.identity;
        Quaternion leftTo = leftClosedRotation * Quaternion.Euler(0f, 0f, targetAngle);
        Quaternion rightTo = rightClosedRotation * Quaternion.Euler(0f, 0f, -targetAngle);

        float t = 0f;
        while (t < swingDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / swingDuration);
            if (leftDoor != null) leftDoor.localRotation = Quaternion.Slerp(leftFrom, leftTo, p);
            if (rightDoor != null) rightDoor.localRotation = Quaternion.Slerp(rightFrom, rightTo, p);
            yield return null;
        }
        if (leftDoor != null) leftDoor.localRotation = leftTo;
        if (rightDoor != null) rightDoor.localRotation = rightTo;
    }
}
