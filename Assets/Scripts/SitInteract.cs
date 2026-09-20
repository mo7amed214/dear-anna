using UnityEngine;
using UnityEngine.InputSystem;

public class SitInteract : MonoBehaviour
{
    public float interactDistance = 2.5f;
    public Vector3 seatWorldOffset = new Vector3(0f, 0.43f, 0f);
    public Vector3 seatFacingEuler = new Vector3(0f, 0f, 0f);

    private bool isSitting;
    private bool lookingAtChair;
    private GameObject player;
    private FirstPersonMove fpm;
    private CharacterController cc;
    private Vector3 standPosition;
    private Quaternion standRotation;
    private GUIStyle standStyle;

    void Update()
    {
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

        if (fpm != null) fpm.canMove = false;
        if (cc != null) cc.enabled = false;

        player.transform.position = transform.position + seatWorldOffset;
        player.transform.rotation = Quaternion.Euler(seatFacingEuler);

        isSitting = true;
    }

    void StandUp()
    {
        if (player == null) return;

        player.transform.position = standPosition;
        player.transform.rotation = standRotation;

        if (cc != null) cc.enabled = true;
        if (fpm != null) fpm.canMove = true;

        isSitting = false;
    }

    void EnsureStyles()
    {
        if (standStyle != null) return;
        standStyle = new GUIStyle(GUI.skin.box) { fontSize = 20, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        standStyle.normal.textColor = Color.white;
    }

    void OnGUI()
    {
        EnsureStyles();

        if (!isSitting)
        {
            if (lookingAtChair && !GameState.IsUIOpen)
            {
                GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height - 65, 200, 30), "Click to sit down", PaperUtil.PromptStyle());
            }
        }
        else if (!GameState.IsUIOpen)
        {
            Rect box = new Rect(Screen.width / 2 - 130, 20, 260, 44);
            GUI.Box(box, "Press  E  to stand up", standStyle);
        }
    }
}
