using UnityEngine;
using UnityEngine.InputSystem;

public class ChoreWindow : MonoBehaviour
{
    public float interactDistance = 3.0f;
    public float brushRadiusPixels = 16f;
    public float wipeStrength = 0.10f;
    [Range(0f, 1f)] public float completeAtRatio = 0.85f;

    private const int TexSize = 256;

    private Texture2D grimeTex;
    private bool started;
    private bool completed;
    private float startMessageTimer;
    private float completeMessageTimer;
    private float lastCompletionCheck;
    private bool lookingAtWindow;

    void Awake()
    {
        grimeTex = MakeGrimeTexture(TexSize);
        var mr = GetComponent<MeshRenderer>();
        if (mr != null && mr.sharedMaterial != null)
        {
            mr.sharedMaterial.SetTexture("_BaseMap", grimeTex);
        }
    }

    Texture2D MakeGrimeTexture(int size)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color grime = new Color(0.50f, 0.47f, 0.40f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.04f, y * 0.04f);
                float streak = Mathf.PerlinNoise(x * 0.01f, y * 0.2f) * 0.15f;
                Color c = grime;
                float shade = (n - 0.5f) * 0.12f - streak;
                c.r = Mathf.Clamp01(c.r + shade);
                c.g = Mathf.Clamp01(c.g + shade);
                c.b = Mathf.Clamp01(c.b + shade);
                c.a = Mathf.Clamp01(0.92f + (n - 0.5f) * 0.15f);
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        return t;
    }

    void Update()
    {
        if (completed)
        {
            if (completeMessageTimer > 0f) completeMessageTimer -= Time.deltaTime;
            return;
        }

        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        bool hitThis = Physics.Raycast(ray, out RaycastHit hit, interactDistance) && hit.collider.gameObject == gameObject;
        lookingAtWindow = hitThis;

        if (startMessageTimer > 0f) startMessageTimer -= Time.deltaTime;

        if (hitThis && Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            if (!started)
            {
                started = true;
                startMessageTimer = 3.5f;
            }
            PaintAt(hit.textureCoord);
        }
    }

    void PaintAt(Vector2 uv)
    {
        int cx = Mathf.RoundToInt(uv.x * TexSize);
        int cy = Mathf.RoundToInt(uv.y * TexSize);
        int r = Mathf.RoundToInt(brushRadiusPixels);
        int minX = Mathf.Max(0, cx - r);
        int maxX = Mathf.Min(TexSize - 1, cx + r);
        int minY = Mathf.Max(0, cy - r);
        int maxY = Mathf.Min(TexSize - 1, cy + r);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                if (d <= r)
                {
                    Color c = grimeTex.GetPixel(x, y);
                    c.a = Mathf.Max(0f, c.a - wipeStrength);
                    grimeTex.SetPixel(x, y, c);
                }
            }
        }
        grimeTex.Apply();

        CheckCompletion();
    }

    void CheckCompletion()
    {
        if (Time.time - lastCompletionCheck < 0.4f) return;
        lastCompletionCheck = Time.time;

        Color[] pixels = grimeTex.GetPixels();
        int sampled = 0;
        int clear = 0;
        for (int i = 0; i < pixels.Length; i += 4)
        {
            sampled++;
            if (pixels[i].a < 0.12f) clear++;
        }

        float ratio = sampled > 0 ? clear / (float)sampled : 0f;
        if (ratio >= completeAtRatio)
        {
            completed = true;
            completeMessageTimer = 4f;
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                c.a = 0f;
                pixels[i] = c;
            }
            grimeTex.SetPixels(pixels);
            grimeTex.Apply();
        }
    }

    void OnGUI()
    {
        if (completed)
        {
            if (completeMessageTimer > 0f)
            {
                GUI.Label(new Rect(Screen.width / 2 - 220, Screen.height - 45, 440, 30),
                    "There. Petersburg, such as it is, through clean glass.", PaperUtil.PromptStyle());
            }
            return;
        }

        if (started && startMessageTimer > 0f)
        {
            GUI.Label(new Rect(Screen.width / 2 - 220, Screen.height - 45, 440, 30),
                "The glass has gone grey with soot and cold.", PaperUtil.PromptStyle());
        }
        else if (lookingAtWindow && !started)
        {
            GUI.Label(new Rect(Screen.width / 2 - 220, Screen.height - 45, 440, 30),
                "Hold click and look around to wipe the glass clean", PaperUtil.PromptStyle());
        }
    }
}
