using Unity.Cinemachine;
using UnityEngine;

// Owns the three CinemachineCamera behaviors: the default first-person view, a
// one-shot "memory focus" shot used when a painting/photo is narrated, and a
// seated vantage used while sitting at the writing desk. CinemachineBrain (on
// Main Camera) smoothly blends between whichever one is enabled via its own
// priority/blend system -- switching which virtual camera is live is a blend,
// not a cut, the same mechanism all three behaviors share.
public class CameraRig : MonoBehaviour
{
    public static CameraRig Instance { get; private set; }

    public CinemachineCamera firstPersonCamera;
    public CinemachineCamera memoryFocusCamera;
    public CinemachineCamera seatedCamera;

    void Awake()
    {
        Instance = this;
        SetActive(firstPersonCamera);
    }

    public void ShowFirstPerson() => SetActive(firstPersonCamera);
    public void ShowMemoryFocus() => SetActive(memoryFocusCamera);
    public void ShowSeated() => SetActive(seatedCamera);

    void SetActive(CinemachineCamera target)
    {
        if (firstPersonCamera != null) firstPersonCamera.enabled = (target == firstPersonCamera);
        if (memoryFocusCamera != null) memoryFocusCamera.enabled = (target == memoryFocusCamera);
        if (seatedCamera != null) seatedCamera.enabled = (target == seatedCamera);
    }
}
