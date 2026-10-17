using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

// Drives CM_Seated's own pan AND tilt directly from mouse delta. Unlike
// CinemachineLookInput (which only owns tilt and relies on being a child of
// the player body for yaw), CM_Seated is intentionally unparented so
// SitInteract can position it explicitly each time -- the same pattern
// PaintingNarration already uses for CM_MemoryFocus's blend-in, which is
// the one camera swap confirmed to actually blend rather than cut. With no
// parent to inherit yaw from, this adapter owns both axes itself.
[RequireComponent(typeof(CinemachinePanTilt))]
public class SeatedLookInput : MonoBehaviour
{
    public float mouseSensitivity = 0.12f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    private CinemachinePanTilt panTilt;
    private FirstPersonMove playerMove;
    private CinemachineCamera cam;

    void Awake()
    {
        panTilt = GetComponent<CinemachinePanTilt>();
        panTilt.TiltAxis.Range = new Vector2(minPitch, maxPitch);
        panTilt.PanAxis.Range = new Vector2(-180f, 180f);
        playerMove = FindAnyObjectByType<FirstPersonMove>();
        cam = GetComponent<CinemachineCamera>();
    }

    void Update()
    {
        // The GameObject itself is never deactivated (only cam.enabled toggles when
        // CameraRig swaps cameras), so without this check this would keep consuming
        // mouse delta -- and silently drifting PanAxis/TiltAxis -- the entire time the
        // player is simply walking around in first person, not just while seated.
        if (cam != null && !cam.enabled) return;
        if (Mouse.current == null) return;
        if (playerMove != null && !playerMove.canLook) return;

        Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;
        panTilt.PanAxis.Value += delta.x;
        float tilt = panTilt.TiltAxis.Value - delta.y;
        panTilt.TiltAxis.Value = Mathf.Clamp(tilt, minPitch, maxPitch);
    }

    // Called by SitInteract right before each sit. CinemachinePanTilt recomputes
    // this camera's rotation from PanAxis/TiltAxis every frame -- it is the ONLY
    // thing that is allowed to touch this camera's rotation, ever, the same way
    // CM_FirstPerson's own rotation is owned entirely by its PanTilt. Setting
    // Transform.rotation directly from script (as SitInteract used to) fights
    // PanTilt's own recomputation on the very next frame, which is what was
    // reading as "cuts in, then a correcting animation" instead of a clean blend.
    public void FaceDirection(float yawDegrees)
    {
        panTilt.PanAxis.Value = yawDegrees;
        panTilt.TiltAxis.Value = 0f;
    }
}
