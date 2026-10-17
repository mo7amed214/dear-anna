using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

// Feeds mouse-look into the first-person CinemachineCamera's vertical (tilt) axis, replacing
// the old hand-coded "pitch -= delta.y; cameraTransform.localRotation = ..." from
// FirstPersonMove. Horizontal look (yaw) stays owned by the Player body's own transform
// rotation (see FirstPersonMove.HandleLook) -- this camera is a child of Player, so it
// inherits that yaw automatically through the normal transform hierarchy; this script only
// ever drives the local tilt on top of that.
[RequireComponent(typeof(CinemachinePanTilt))]
public class CinemachineLookInput : MonoBehaviour
{
    public float mouseSensitivity = 0.12f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    private CinemachinePanTilt panTilt;
    private FirstPersonMove playerMove;

    void Awake()
    {
        panTilt = GetComponent<CinemachinePanTilt>();
        panTilt.TiltAxis.Range = new Vector2(minPitch, maxPitch);
        panTilt.PanAxis.Range = new Vector2(-180f, 180f);
        playerMove = FindAnyObjectByType<FirstPersonMove>();
    }

    void Update()
    {
        if (Mouse.current == null) return;
        if (playerMove != null && !playerMove.canLook) return;

        float deltaY = Mouse.current.delta.ReadValue().y * mouseSensitivity;
        float tilt = panTilt.TiltAxis.Value - deltaY;
        panTilt.TiltAxis.Value = Mathf.Clamp(tilt, minPitch, maxPitch);
    }
}
