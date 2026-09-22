using UnityEngine;
using UnityEngine.InputSystem;

public class LightSwitch : MonoBehaviour
{
    public float interactDistance = 2.5f;
    public Light controlledLight;
    public Transform toggleLever;

    private bool isOn = true;
    private bool lookingAtSwitch;

    void Update()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        lookingAtSwitch = Physics.Raycast(ray, out RaycastHit hit, interactDistance)
            && hit.collider.GetComponentInParent<LightSwitch>() == this;

        if (lookingAtSwitch && !GameState.IsUIOpen
            && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Toggle();
        }
    }

    void Toggle()
    {
        isOn = !isOn;
        if (controlledLight != null) controlledLight.enabled = isOn;
        if (toggleLever != null)
        {
            toggleLever.localEulerAngles = isOn ? new Vector3(-15f, 0f, 0f) : new Vector3(15f, 0f, 0f);
        }
    }

    void OnGUI()
    {
        if (lookingAtSwitch && !GameState.IsUIOpen)
        {
            string msg = isOn ? "Click to turn off the chandelier" : "Click to turn on the chandelier";
            GUI.Label(new Rect(Screen.width / 2 - 160, Screen.height - 45, 320, 30), msg, PaperUtil.PromptStyle());
        }
    }
}
