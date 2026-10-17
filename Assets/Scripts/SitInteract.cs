using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class SitInteract : MonoBehaviour
{
    public float interactDistance = 2.5f;
    public Vector3 seatWorldOffset = new Vector3(0f, 0.43f, 0f);
    public Vector3 seatFacingEuler = new Vector3(0f, 0f, 0f);
    // Deliberately lower than CM_FirstPerson's own ~0.7 standing eye offset, so
    // sitting down actually lowers the camera (confirmed via diagnostic logging
    // that the old 0.7 value put the seated eye height ~7cm *above* standing,
    // leaving nothing visible for the blend to show).
    public float cameraEyeHeight = 0.45f;

    private bool isSitting;
    // True for the whole length of a camera blend (sit or stand) -- found via
    // diagnostic logging that pressing E/clicking again before a blend finished
    // interrupted it mid-flight, which Cinemachine handles by recalculating a
    // shortened, inconsistent blend duration (0.34s, 0.64s, 1.27s, all different
    // each time) rather than the real 2s -- that inconsistency was the actual
    // "sometimes works, sometimes doesn't." Locking input out until the current
    // transition's blend has genuinely finished makes every transition clean.
    private bool isTransitioning;
    private bool lookingAtChair;
    private GameObject player;
    private FirstPersonMove fpm;
    private CharacterController cc;
    private Vector3 standPosition;
    private Quaternion standRotation;
    private GUIStyle standStyle;

    void Update()
    {
        if (isTransitioning) return;

        if (!isSitting)
        {
            lookingAtChair = CheckGaze();
            if (lookingAtChair && !GameState.IsUIOpen && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                SitDown();
            }
        }
        else
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                StandUp();
            }
        }
    }

    bool CheckGaze()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            return hit.collider.GetComponentInParent<SitInteract>() == this;
        }
        return false;
    }

    void SitDown()
    {
        player = GameObject.Find("Player");
        if (player == null) return;
        fpm = player.GetComponent<FirstPersonMove>();
        cc = player.GetComponent<CharacterController>();

        standPosition = player.transform.position;
        standRotation = player.transform.rotation;

        if (fpm != null) { fpm.canMove = false; fpm.canLook = false; }
        if (cc != null) cc.enabled = false;

        Vector3 seatPos = transform.position + seatWorldOffset;
        Quaternion seatRot = Quaternion.Euler(seatFacingEuler);

        // CM_Seated is positioned straight from the chair's own transform -- it never
        // needed the player's position at all, so the camera swap and the player's
        // own teleport are fully decoupled (see MovePlayerAfterBlend below for why
        // that decoupling has to last the whole blend, not just one frame).
        if (CameraRig.Instance != null && CameraRig.Instance.seatedCamera != null)
        {
            Transform seatedCam = CameraRig.Instance.seatedCamera.transform;
            seatedCam.position = seatPos + Vector3.up * cameraEyeHeight;
            CameraRig.Instance.seatedCamera.GetComponent<SeatedLookInput>()?.FaceDirection(seatFacingEuler.y);
            CameraRig.Instance.ShowSeated();
        }

        isTransitioning = true;
        isSitting = true;
        StartCoroutine(MovePlayerAfterBlend(seatPos, seatRot));
    }

    // Cinemachine doesn't take a one-time snapshot of the outgoing camera when a
    // blend starts -- it keeps live-sampling CM_FirstPerson's current transform
    // for the entire blend. Since CM_FirstPerson is a child of Player, moving the
    // player into the blend immediately corrupts the reference the blend is
    // actively reading from, collapsing it to almost nothing. The player must not
    // move until the blend has genuinely finished. Polling CinemachineBrain.IsBlending
    // for this turned out unreliable (confirmed via logging: it read false
    // immediately after the swap and never flipped true before the wait resolved),
    // so this waits the actual configured blend duration directly instead of
    // trusting that flag.
    IEnumerator MovePlayerAfterBlend(Vector3 pos, Quaternion rot)
    {
        yield return new WaitForSeconds(BlendDuration() + 0.05f);
        player.transform.SetPositionAndRotation(pos, rot);
        if (fpm != null) fpm.canLook = true;
        isTransitioning = false;
    }

    float BlendDuration()
    {
        CinemachineBrain brain = FindAnyObjectByType<CinemachineBrain>();
        return brain != null ? brain.DefaultBlend.Time : 0f;
    }

    void StandUp()
    {
        if (player == null) return;

        player.transform.SetPositionAndRotation(standPosition, standRotation);

        if (cc != null) cc.enabled = true;
        if (fpm != null) fpm.canMove = true;

        if (CameraRig.Instance != null) CameraRig.Instance.ShowFirstPerson();

        isSitting = false;
        isTransitioning = true;
        StartCoroutine(ClearTransitionWhenBlendDone());
    }

    IEnumerator ClearTransitionWhenBlendDone()
    {
        yield return new WaitForSeconds(BlendDuration() + 0.05f);
        isTransitioning = false;
    }

    void EnsureStyles()
    {
        if (standStyle != null) return;
        standStyle = new GUIStyle(GUI.skin.box) { font = PaperUtil.GameFont, fontSize = 20, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        standStyle.normal.textColor = Color.white;
    }

    void OnGUI()
    {
        EnsureStyles();

        if (!isSitting)
        {
            if (lookingAtChair && !GameState.IsUIOpen && !isTransitioning)
            {
                GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height - 65, 200, 30), "Click to sit down", PaperUtil.PromptStyle());
            }
        }
        else if (!GameState.IsUIOpen && !isTransitioning)
        {
            Rect box = new Rect(Screen.width / 2 - 130, 20, 260, 44);
            GUI.Box(box, "Press  E  to stand up", standStyle);
        }
    }
}
