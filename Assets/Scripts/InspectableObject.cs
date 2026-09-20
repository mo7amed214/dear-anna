using UnityEngine;
using UnityEngine.InputSystem;

public class InspectableObject : MonoBehaviour
{
    public float pickupDistance = 2.5f;
    public Vector3 holdLocalPosition = new Vector3(0f, 0f, 0.6f);
    public float rotateSpeed = 40f;

    private bool isHeld;
    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    void Update()
    {
        if (Mouse.current == null || Camera.main == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (isHeld)
            {
                PutDown();
            }
            else
            {
                TryPickUp();
            }
        }

        if (isHeld)
        {
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.Self);
        }
    }

    void TryPickUp()
    {
        Camera cam = Camera.main;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance))
        {
            InspectableObject target = hit.collider.GetComponentInParent<InspectableObject>();
            if (target == this)
            {
                PickUp(cam);
            }
        }
    }

    void PickUp(Camera cam)
    {
        isHeld = true;
        originalParent = transform.parent;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;

        transform.SetParent(cam.transform, true);
        transform.localPosition = holdLocalPosition;
    }

    void PutDown()
    {
        isHeld = false;
        transform.SetParent(originalParent, true);
        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;
    }
}
