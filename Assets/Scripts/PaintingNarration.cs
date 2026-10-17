using UnityEngine;
using UnityEngine.InputSystem;

// Attach to a painting/photo/exhibit in the dream's mind-palace: look at it and press E,
// and Stanislav reacts with a line about it. Purely narrated -- nothing to pick up.
// Detection uses a sphere-cast (not a pixel-thin ray) so flush wall-mounted frames are easy
// to land the crosshair on without needing to be pixel-perfect.
public class PaintingNarration : MonoBehaviour
{
    public float interactDistance = 5.0f;
    public float gazeRadius = 0.35f;
    // Optional short label folded into the prompt below ("Press E to look closer -- <name>")
    // so objects crammed close together (desk clutter) are each identifiable. Blank for every
    // object where the plain prompt is already enough -- leaves those prompts unchanged.
    public string displayName = "";
    [TextArea] public string narration = "";
    public float displayDuration = 7f;
    public AudioClip voiceLine;
    // How far in front of this object CameraRig's memory-focus camera is placed when this
    // object is narrated -- a per-object knob for the same reason interactDistance/gazeRadius
    // already are (frame sizes and wall depths vary object to object).
    public float focusDistance = 1.1f;
    // True for flat, wall-mounted art (paintings, photographs, the portrait, the clock's
    // own face) -- the shot looks straight at the object along its own forward axis,
    // perfectly centred and level, the way a gallery label shot would. False for anything
    // freestanding on a surface (the inkwell and paper, the chess set, the safe, the
    // flowers) where the object's own forward axis isn't reliably "outward" at all; those
    // get an elevated three-quarter shot instead, looking down and across at the object
    // from whichever side the player approached from.
    public bool flatFraming = true;
    // Extra height the camera is lifted above the object for a non-flat (angled) shot.
    public float focusElevation = 0.35f;

    private bool lookingAt;
    private float narrationTimer;
    private AudioSource voiceSource;
    private FirstPersonMove fpm;

    // Only one PaintingNarration owns the shared memory-focus camera at a time; this is
    // how that instance knows it's the one that should hand the camera back when its own
    // narration ends, rather than every instance fighting over CameraRig on every frame.
    private static PaintingNarration activeFocus;

    void Awake()
    {
        voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        voiceSource.spatialBlend = 0f;
        voiceSource.loop = false;
        fpm = FindAnyObjectByType<FirstPersonMove>();
    }

    // Movement and look are frozen for the length of a focus shot -- CM_MemoryFocus has no
    // pan/tilt of its own, so letting the player's hidden body keep turning or walking
    // underneath it would only desync where they land once the shot blends back out.
    void EndFocus()
    {
        activeFocus = null;
        narrationTimer = 0f;
        voiceSource.Stop();
        GameState.IsUIOpen = false;
        if (fpm != null) { fpm.canMove = true; fpm.canLook = true; }
        if (CameraRig.Instance != null) CameraRig.Instance.ShowFirstPerson();
    }

    void Update()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.SphereCastAll(ray, gazeRadius, interactDistance);
        lookingAt = false;
        float thisDistance = float.MaxValue;
        foreach (var h in hits)
        {
            if (h.collider.GetComponentInParent<PaintingNarration>() == this)
            {
                lookingAt = true;
                thisDistance = h.distance;
                break;
            }
        }
        if (lookingAt)
        {
            foreach (var h in hits)
            {
                if (h.collider.GetComponentInParent<PaintingNarration>() == this) continue;
                if (h.collider.isTrigger) continue;
                // A hit at ~0 distance means the sphere-cast's own starting sphere already
                // touches that collider -- i.e. the camera itself is right up against some
                // nearby wall/furniture, not that it's standing between the camera and the
                // target. That's irrelevant to whether THIS object is actually obstructed,
                // so it must never count as "something closer in the way".
                if (h.distance < 0.05f) continue;
                if (h.distance < thisDistance - 0.05f) { lookingAt = false; break; }
            }
        }

        if (activeFocus == this && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            EndFocus();
            return;
        }

        bool wasCountingDown = narrationTimer > 0f;
        if (wasCountingDown) narrationTimer -= Time.deltaTime;
        if (wasCountingDown && narrationTimer <= 0f && activeFocus == this)
        {
            EndFocus();
            return;
        }

        if (lookingAt && !GameState.IsUIOpen && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            narrationTimer = displayDuration;
            if (voiceLine != null)
            {
                voiceSource.clip = voiceLine;
                voiceSource.Play();
                narrationTimer = Mathf.Max(narrationTimer, voiceLine.length);
            }

            if (CameraRig.Instance != null && CameraRig.Instance.memoryFocusCamera != null)
            {
                Transform focusCam = CameraRig.Instance.memoryFocusCamera.transform;
                Vector3 shotPos;
                Vector3 lookDir;

                if (flatFraming)
                {
                    // Straight-on and centred: the object's own forward axis is the
                    // outward wall normal for flat-mounted art, so this is reliable here.
                    lookDir = -transform.forward;
                    shotPos = transform.position + transform.forward * focusDistance;
                }
                else
                {
                    // Elevated three-quarter shot: pull back horizontally on whichever
                    // side the player is standing (not the object's own forward axis,
                    // which isn't meaningful for a freestanding desk object), then lift
                    // the camera above the object so it looks down and across at it.
                    Vector3 horiz = transform.position - cam.transform.position;
                    horiz.y = 0f;
                    if (horiz.sqrMagnitude < 0.0001f) horiz = cam.transform.forward;
                    horiz.y = 0f;
                    if (horiz.sqrMagnitude < 0.0001f) horiz = Vector3.forward;
                    horiz.Normalize();
                    shotPos = transform.position - horiz * focusDistance + Vector3.up * focusElevation;
                    lookDir = (transform.position - shotPos).normalized;
                }

                focusCam.SetPositionAndRotation(shotPos, Quaternion.LookRotation(lookDir, Vector3.up));

                activeFocus = this;
                GameState.IsUIOpen = true;
                if (fpm != null) { fpm.canMove = false; fpm.canLook = false; }
                CameraRig.Instance.ShowMemoryFocus();
            }
        }
    }

    void OnGUI()
    {
        if (lookingAt && !GameState.IsUIOpen && narrationTimer <= 0f)
        {
            string prompt = string.IsNullOrEmpty(displayName)
                ? "Press E to look closer"
                : "Press E to look closer -- " + displayName;
            Rect promptRect = string.IsNullOrEmpty(displayName)
                ? new Rect(Screen.width / 2 - 110, Screen.height - 45, 220, 30)
                : new Rect(Screen.width / 2 - 210, Screen.height - 45, 420, 30);
            GUI.Label(promptRect, prompt, PaperUtil.PromptStyle());
        }

        if (narrationTimer > 0f && !string.IsNullOrEmpty(narration))
        {
            GUI.Label(new Rect(Screen.width / 2 - 260, 40, 520, 90), narration, PaperUtil.GreetingStyle(17));
        }

        if (activeFocus == this && narrationTimer > 0f)
        {
            GUI.Label(new Rect(Screen.width / 2 - 110, Screen.height - 45, 220, 30), "Press Esc to look away", PaperUtil.PromptStyle());
        }
    }
}
