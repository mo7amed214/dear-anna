using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class InspectableObject : MonoBehaviour
{
    public float pickupDistance = 5f;
    public float gazeRadius = 0.3f;
    public Vector3 holdLocalPosition = new Vector3(0f, 0f, 0.6f);
    // Some meshes' "front" doesn't line up with the object's own local forward axis -- override
    // per-object if it looks wrong (flipped/upside down) when held.
    public Vector3 holdLocalEuler = Vector3.zero;
    public float rotateSpeed = 0.08f;
    public float minimumHoldDistance = 1.25f;
    public string pickupAnimationState = "Pickup";
    public float backgroundBlurDistance = 1.4f;
    [Range(0f, 1f)] public float backgroundBlurStrength = 1f;

    // Multiplies the object's own scale while held, so small props (a ring, a watch) read
    // clearly in front of the camera instead of staying table-sized once picked up.
    public float heldScale = 4f;

    // Cap on how much of the screen height the held object may fill, so an already-large prop
    // multiplied by heldScale doesn't end up filling (and overflowing) the whole view.
    [Range(0.1f, 1f)] public float maxScreenFill = 0.6f;

    // Optional short label folded into the "Click to inspect -- <name>" prompt below, same
    // convention as PaintingNarration.displayName -- blank by default, no effect on existing prompts.
    public string displayName = "";

    // Optional line Stanislav says when this is picked up -- used for the dream/mind-palace
    // exhibits (pocket watch, ring), left blank for ordinary real-world props (teacup, photo).
    [TextArea] public string narration = "";
    public float narrationDuration = 6f;

    // Optional recorded voice line played alongside the narration text. Left unassigned until
    // the real voice-acted clip is recorded -- narrationDuration still governs the text alone.
    public AudioClip voiceLine;

    private bool isHeld;
    private bool lookingAt;
    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Vector3 originalLocalScale;
    private float narrationTimer;
    private AudioSource voiceSource;
    private FirstPersonMove playerMovement;
    private Animator animator;
    private Animation legacyAnimation;
    private static InspectableObject heldObject;
    private float inspectionYaw;
    private float inspectionPitch;
    private Volume inspectionVolume;
    private DepthOfField depthOfField;
    private UniversalAdditionalCameraData cameraData;
    private bool originalCanMove;
    private bool originalCanLook;
    private bool originalPostProcessingState;
    private Camera holdCamera;
    private float originalNearClip;
    private Collider[] disabledColliders;
    private Vector3 heldCenter;
    private Vector3 boundsCenterLocal;

    // While held, the object is shrunk and pulled toward the lens by the same factor -- it looks
    // identical on screen, but sits closer than any desk or wall can get, so nothing clips through it.
    const float HeldNearClip = 0.01f;
    const float HeldClosestDistance = 0.04f;

    void Awake()
    {
        voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        voiceSource.spatialBlend = 0f;
        voiceSource.loop = false;
        playerMovement = FindAnyObjectByType<FirstPersonMove>();
        animator = GetComponentInChildren<Animator>();
        legacyAnimation = GetComponentInChildren<Animation>();
        foreach (Camera childCamera in GetComponentsInChildren<Camera>(true))
        {
            childCamera.enabled = false;
        }
        if (animator != null) animator.enabled = false;
        if (legacyAnimation != null) legacyAnimation.enabled = false;
    }

    void Update()
    {
        if (Mouse.current == null || Camera.main == null) return;

        if (!isHeld)
        {
            lookingAt = CheckGaze();
        }

        if ((isHeld || (lookingAt && heldObject == null))
            && !GameState.IsUIOpen && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (isHeld) PutDown();
            else PickUp(Camera.main);
        }

        if (isHeld)
        {
            if (playerMovement != null)
            {
                playerMovement.canMove = false;
                playerMovement.canLook = false;
            }

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            inspectionYaw += mouseDelta.x * rotateSpeed;
            inspectionPitch -= mouseDelta.y * rotateSpeed;
            transform.localRotation = Quaternion.Euler(holdLocalEuler)
                * Quaternion.Euler(inspectionPitch, inspectionYaw, 0f);
            KeepCenterInPlace();
        }

        if (narrationTimer > 0f) narrationTimer -= Time.deltaTime;
    }

    bool CheckGaze()
    {
        Camera cam = Camera.main;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        // A sphere-cast, not a pixel-thin ray -- small props (a ring, a watch) shouldn't
        // need a pixel-perfect crosshair to be picked up. Checked against every overlap
        // (not just the first) so a slightly-clipped table edge doesn't block the hit, while
        // still respecting a genuine wall in the way.
        RaycastHit[] hits = Physics.SphereCastAll(ray, gazeRadius, pickupDistance);
        float nearestDistance = float.MaxValue;
        foreach (var h in hits)
        {
            if (h.collider.GetComponentInParent<InspectableObject>() == this && h.distance < nearestDistance)
            {
                nearestDistance = h.distance;
            }
        }
        return nearestDistance < float.MaxValue;
    }

    void PickUp(Camera cam)
    {
        if (heldObject != null && heldObject != this) return;

        isHeld = true;
        heldObject = this;
        originalParent = transform.parent;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        originalLocalScale = transform.localScale;

        transform.SetParent(cam.transform, true);
        transform.localPosition = new Vector3(
            holdLocalPosition.x,
            holdLocalPosition.y,
            Mathf.Max(holdLocalPosition.z, minimumHoldDistance));
        transform.localRotation = Quaternion.Euler(holdLocalEuler); // face the camera squarely, regardless of how it sat in the world
        inspectionYaw = 0f;
        inspectionPitch = 0f;
        transform.localScale = originalLocalScale * heldScale;
        FrameHeldObject(cam);

        // Colliders off while held so the prop can't shove the player or snag gaze raycasts.
        disabledColliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in disabledColliders) c.enabled = false;

        if (playerMovement != null)
        {
            originalCanMove = playerMovement.canMove;
            originalCanLook = playerMovement.canLook;
            playerMovement.canMove = false;
            playerMovement.canLook = false;
        }
        EnableBackgroundBlur(cam);

        if (animator != null && !string.IsNullOrEmpty(pickupAnimationState))
        {
            animator.enabled = true;
            animator.Play(pickupAnimationState, 0, 0f);
        }
        else if (legacyAnimation != null)
        {
            legacyAnimation.enabled = true;
            legacyAnimation.Play();
        }

        if (!string.IsNullOrEmpty(narration)) narrationTimer = narrationDuration;

        if (voiceLine != null)
        {
            voiceSource.clip = voiceLine;
            voiceSource.Play();
            // The line itself sets the pace once it's recorded -- keep the text up as long as it plays.
            narrationTimer = Mathf.Max(narrationTimer, voiceLine.length);
        }
    }

    void PutDown()
    {
        isHeld = false;
        if (heldObject == this) heldObject = null;
        transform.SetParent(originalParent, true);
        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;
        transform.localScale = originalLocalScale;
        if (holdCamera != null) holdCamera.nearClipPlane = originalNearClip;
        holdCamera = null;
        if (disabledColliders != null)
        {
            foreach (Collider c in disabledColliders) if (c != null) c.enabled = true;
            disabledColliders = null;
        }
        if (playerMovement != null)
        {
            playerMovement.canMove = originalCanMove;
            playerMovement.canLook = originalCanLook;
        }
        DisableBackgroundBlur();
        if (animator != null) animator.enabled = false;
        if (legacyAnimation != null) legacyAnimation.enabled = false;
        narrationTimer = 0f;
        voiceSource.Stop();
    }

    // Centres the held prop on its visual middle (not its pivot, which can sit well off to one
    // side), caps its on-screen size, then shrinks it toward the lens. Scaling position and size
    // about the camera by the same factor leaves the projected image unchanged, so it keeps its
    // framing but sits closer than any desk or wall can get.
    void FrameHeldObject(Camera cam)
    {
        heldCenter = transform.localPosition;
        boundsCenterLocal = Vector3.zero;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        boundsCenterLocal = transform.InverseTransformPoint(bounds.center);

        float distance = heldCenter.magnitude;
        float visibleHalfHeight = distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float largestExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        float fitLimit = visibleHalfHeight * maxScreenFill;
        float radius = bounds.extents.magnitude;
        if (largestExtent > fitLimit)
        {
            float fit = fitLimit / largestExtent;
            transform.localScale *= fit;
            radius *= fit;
        }

        float closestDistance = distance - radius;
        if (closestDistance > HeldClosestDistance)
        {
            holdCamera = cam;
            originalNearClip = cam.nearClipPlane;
            cam.nearClipPlane = Mathf.Min(originalNearClip, HeldNearClip);

            float k = HeldClosestDistance / closestDistance;
            heldCenter *= k;
            transform.localScale *= k;
        }

        KeepCenterInPlace();
    }

    // Offsets the pivot so the mesh spins around its visual centre rather than swinging around.
    void KeepCenterInPlace()
    {
        transform.localPosition = heldCenter
            - transform.localRotation * Vector3.Scale(transform.localScale, boundsCenterLocal);
    }

    void EnableBackgroundBlur(Camera cam)
    {
        if (inspectionVolume == null)
        {
            GameObject volumeObject = new GameObject("Inspection Background Blur");
            volumeObject.transform.SetParent(cam.transform, false);
            inspectionVolume = volumeObject.AddComponent<Volume>();
            inspectionVolume.isGlobal = true;
            inspectionVolume.priority = 100f;
            inspectionVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            depthOfField = inspectionVolume.profile.Add<DepthOfField>(true);
        }

        cameraData = cam.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData != null)
        {
            originalPostProcessingState = cameraData.renderPostProcessing;
            cameraData.renderPostProcessing = true;
        }

        depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
        depthOfField.gaussianStart.Override(Mathf.Max(0.1f, minimumHoldDistance - 0.15f));
        depthOfField.gaussianEnd.Override(Mathf.Max(backgroundBlurDistance, minimumHoldDistance + 0.05f));
        depthOfField.gaussianMaxRadius.Override(backgroundBlurStrength);
        depthOfField.active = true;
        inspectionVolume.enabled = true;
    }

    void DisableBackgroundBlur()
    {
        if (inspectionVolume != null) inspectionVolume.enabled = false;
        if (cameraData != null) cameraData.renderPostProcessing = originalPostProcessingState;
    }

    void OnGUI()
    {
        if (!isHeld && lookingAt && !GameState.IsUIOpen)
        {
            string prompt = string.IsNullOrEmpty(displayName) ? "Click to inspect" : "Click to inspect -- " + displayName;
            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height - 45, 300, 30), prompt, PaperUtil.PromptStyle());
        }

        if (isHeld)
        {
            GUI.Label(new Rect(Screen.width / 2 - 190, Screen.height - 70, 380, 24), "Move mouse to rotate | Click to put down", PaperUtil.PromptStyle(13));

            if (narrationTimer > 0f && !string.IsNullOrEmpty(narration))
            {
                GUI.Label(new Rect(Screen.width / 2 - 260, 40, 520, 90), narration, PaperUtil.GreetingStyle(17));
            }
        }
    }
}
