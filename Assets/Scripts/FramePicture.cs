using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Attach to any frame/painting/photo GameObject. Drag an image into the Image slot in the
// Inspector and it's placed inside that exact frame instantly, scaled to fit and facing the
// right way -- no menu, no dialog. All placement logic lives here (in a normal, non-editor-only
// script) so both this component and the Tools > Frame Picture menu item (FramePictureTool, in
// Assets/Editor) can call the same code; an editor-only script can't be added via AddComponent
// from code, only by hand in the Inspector, so the component itself has to live outside Editor/.
[ExecuteAlways]
public class FramePicture : MonoBehaviour
{
    public Texture2D image;
    [Range(0f, 1f)] public float brightness = 0.8f;
    [Range(0.5f, 1f)] public float marginFactor = 0.85f;
    [Tooltip("Where to center the crop inside the source image, in UV space: (0,0)=bottom-left, (1,1)=top-right, (0.5,0.5)=centered (default). Shift this if the subject isn't centered in the source photo.")]
    public Vector2 focusPoint = new Vector2(0.5f, 0.5f);
    [Tooltip("The source photo's own aspect ratio is too different from this frame's opening for any crop window to show everyone/everything -- a portrait photo in a landscape frame, or vice versa. Shows the whole image shrunk to fit instead of cropping it, leaving a sliver of the opening showing on the narrow sides.")]
    public bool showFullImage = false;
    [TextArea] public string narration = "";

#if UNITY_EDITOR
    private Texture2D lastAppliedImage;
    private float lastAppliedBrightness = -1f;
    private float lastAppliedMargin = -1f;
    private Vector2 lastAppliedFocus = new Vector2(-1f, -1f);
    private bool lastAppliedShowFull;

    void OnValidate()
    {
        // OnValidate also fires on every domain reload -- including the one Unity performs
        // on every single "Enter Play Mode" by default -- not just real Inspector edits. Its
        // non-serialized lastApplied* fields reset to defaults on that same reload, so without
        // this guard ApplyIfChanged() always sees a "change" and rebakes the quad fresh with
        // the static image the instant Play mode starts, silently undoing any runtime texture
        // swap (e.g. FrameVideo's video-feed) a moment after it happens. Re-baking frame
        // geometry/crop is purely an authoring-time concern; it has nothing to do once playing.
        if (EditorApplication.isPlaying) return;
        EditorApplication.delayCall += ApplyIfChanged;
    }

    void ApplyIfChanged()
    {
        if (this == null || image == null) return;
        if (image == lastAppliedImage && brightness == lastAppliedBrightness && marginFactor == lastAppliedMargin && focusPoint == lastAppliedFocus && showFullImage == lastAppliedShowFull) return;
        Apply();
    }

    public void Apply()
    {
        if (image == null) return;
        string path = AssetDatabase.GetAssetPath(image);
        FramePictureCore.PlaceImage(gameObject, image, path, null,
            string.IsNullOrEmpty(narration) ? null : narration, marginFactor, brightness, focusPoint, showFullImage);
        lastAppliedImage = image;
        lastAppliedBrightness = brightness;
        lastAppliedMargin = marginFactor;
        lastAppliedFocus = focusPoint;
        lastAppliedShowFull = showFullImage;
    }
#endif
}
