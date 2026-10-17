using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonMove : MonoBehaviour
{
    public float moveSpeed = 3.2f;
    public float mouseSensitivity = 0.12f;
    public float gravity = -9.81f;
    public Transform cameraTransform;
    [HideInInspector] public bool canMove = true;
    [HideInInspector] public bool canLook = true;

    private CharacterController controller;
    private float verticalVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && GetComponentInChildren<Camera>() != null)
        {
            cameraTransform = GetComponentInChildren<Camera>().transform;
        }
    }

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (canLook)
        {
            HandleLook();
        }
        if (canMove)
        {
            HandleMove();
        }
    }

    // Yaw (horizontal look) still rotates the player body directly, since HandleMove()
    // derives its WASD direction from this same transform -- turning the body this way is
    // what keeps "forward" meaning "the way I'm facing" for movement. Pitch (vertical look)
    // is no longer set here: it's owned by CinemachineLookInput/CinemachinePanTilt on the
    // first-person CinemachineCamera, which inherits this yaw automatically by being a child
    // of this transform, the same way cameraTransform always was.
    void HandleLook()
    {
        if (Mouse.current == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;
        transform.Rotate(Vector3.up * delta.x);
    }

    void HandleMove()
    {
        if (Keyboard.current == null || !controller.enabled) return;

        float x = 0f;
        float z = 0f;
        if (Keyboard.current.aKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.sKey.isPressed) z -= 1f;
        if (Keyboard.current.wKey.isPressed) z += 1f;

        Vector3 move = (transform.right * x + transform.forward * z);
        if (move.sqrMagnitude > 1f) move.Normalize();

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -0.5f;
        }
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * moveSpeed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }
}
