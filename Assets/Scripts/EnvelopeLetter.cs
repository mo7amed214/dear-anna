using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// A physical envelope prop that slides out from under the door and can be picked up
// with E once the player is close and looking at it. This only handles the physical
// pickup -- the actual letter-reading panel is owned by whatever delivered the letter
// (see GameIntro for Anna's first one) via the OnPickedUp event, so each letter's
// reading UI is drawn from that caller's own OnGUI and layers correctly against its
// own extra effects (e.g. GameIntro's Bloody Sunday image).
public class EnvelopeLetter : MonoBehaviour
{
    public float interactDistance = 2.2f;
    public float slideDuration = 1.3f;

    public event Action OnPickedUp;

    private bool pickedUp;
    private bool lookingAtEnvelope;

    public void BeginDelivery(Vector3 from, Vector3 to)
    {
        transform.position = from;
        StartCoroutine(SlideRoutine(from, to));
    }

    IEnumerator SlideRoutine(Vector3 from, Vector3 to)
    {
        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / slideDuration);
            transform.position = Vector3.Lerp(from, to, p);
            yield return null;
        }
        transform.position = to;
    }

    void Update()
    {
        if (pickedUp) return;

        // A letter from waking life has no business in a dream.
        if (GameState.IsDreaming) { lookingAtEnvelope = false; return; }

        lookingAtEnvelope = CheckGaze();
        if (lookingAtEnvelope && !GameState.IsUIOpen && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            pickedUp = true;
            OnPickedUp?.Invoke();
            gameObject.SetActive(false);
        }
    }

    bool CheckGaze()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            return hit.collider.GetComponentInParent<EnvelopeLetter>() == this;
        }
        return false;
    }

    void OnGUI()
    {
        if (pickedUp || GameState.IsUIOpen) return;
        if (lookingAtEnvelope)
        {
            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height - 45, 300, 30),
                "Press E to pick up the letter", PaperUtil.PromptStyle());
        }
    }
}
