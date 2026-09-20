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
    private float pitch;
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

    void HandleLook()
    {
        if (Mouse.current == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;

        transform.Rotate(Vector3.up * delta.x);

        pitch -= delta.y;
        pitch = Mathf.Clamp(pitch, -80f, 80f);
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
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
