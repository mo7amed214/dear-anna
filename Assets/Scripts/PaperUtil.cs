using UnityEngine;

public static class PaperUtil
{
    public static Texture2D MakePaperTexture(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        Color baseCol = new Color(0.93f, 0.87f, 0.74f);
        System.Random rng = new System.Random(7);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.015f, y * 0.015f) - 0.5f;
                float fine = ((float)rng.NextDouble() - 0.5f) * 0.02f;

                float dx = (x - w / 2f) / (w / 2f);
                float dy = (y - h / 2f) / (h / 2f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float vignette = 1f - Mathf.Clamp01(Mathf.Pow(dist, 3f)) * 0.18f;

                float shade = n * 0.05f + fine;
                Color c = baseCol * vignette;
                c.r = Mathf.Clamp01(c.r + shade);
                c.g = Mathf.Clamp01(c.g + shade);
                c.b = Mathf.Clamp01(c.b + shade);
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    static readonly Color Ink = new Color(0.22f, 0.16f, 0.1f);

    // Forces every single interaction state (not just "normal") to the same color,
    // so it is structurally impossible for this text to display any other color
    // under any circumstance -- hover, active, focus, or otherwise.
    static GUIStyle LockedColor(GUIStyle s, Color c)
    {
        s.normal.textColor = c;
        s.hover.textColor = c;
        s.active.textColor = c;
        s.focused.textColor = c;
        s.onNormal.textColor = c;
        s.onHover.textColor = c;
        s.onActive.textColor = c;
        s.onFocused.textColor = c;
        return s;
    }

    public static GUIStyle InkStyle(int fontSize = 18)
    {
        GUIStyle s = new GUIStyle(GUI.skin.label) { fontSize = fontSize, wordWrap = true, alignment = TextAnchor.UpperLeft };
        return LockedColor(s, Ink);
    }

    public static GUIStyle GreetingStyle(int fontSize = 17)
    {
        GUIStyle s = new GUIStyle(GUI.skin.label) { fontSize = fontSize, fontStyle = FontStyle.Italic };
        return LockedColor(s, Ink);
    }

    public static GUIStyle SignatureStyle(int fontSize = 17)
    {
        GUIStyle s = new GUIStyle(GUI.skin.label) { fontSize = fontSize, fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleRight };
        return LockedColor(s, Ink);
    }

    public static GUIStyle HintStyle(int fontSize = 12)
    {
        GUIStyle s = new GUIStyle(GUI.skin.label) { fontSize = fontSize };
        return LockedColor(s, new Color(0.35f, 0.28f, 0.2f));
    }

    public static GUIStyle PromptStyle(int fontSize = 16)
    {
        GUIStyle s = new GUIStyle(GUI.skin.label) { fontSize = fontSize, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        return LockedColor(s, new Color(0.95f, 0.72f, 0.25f)); // warm gold, not white
    }

    private static Texture2D flatFillTex;
    private static Texture2D flatHoverTex;
    private static Texture2D flatBorderTex;
    private static GUIStyle flatTextStyle;
    private static GUIStyle flatTextHoverStyle;
    private static GUIStyle invisibleHitStyle;

    static Texture2D SolidTexture(Color c)
    {
        Texture2D t = new Texture2D(2, 2);
        t.SetPixels(new[] { c, c, c, c });
        t.Apply();
        return t;
    }

    // Unity's IMGUI only honors a style's hover/active state if that state has a
    // background texture assigned; if it's null, it silently falls back to a native
    // default look (a light/white highlight) instead -- regardless of any color you set.
    // GUIStyle.none has no background in ANY state, so it was hitting that native
    // fallback and drawing a white highlight over our button. Fix: give every state
    // the same fully-transparent background so Unity treats them as "provided" and
    // never substitutes its own look.
    static GUIStyle InvisibleHitStyle()
    {
        if (invisibleHitStyle != null) return invisibleHitStyle;

        Texture2D clearTex = SolidTexture(new Color(0f, 0f, 0f, 0f));
        invisibleHitStyle = new GUIStyle();
        Color clearText = new Color(0f, 0f, 0f, 0f);
        foreach (var state in new[]
        {
            invisibleHitStyle.normal, invisibleHitStyle.hover, invisibleHitStyle.active, invisibleHitStyle.focused,
            invisibleHitStyle.onNormal, invisibleHitStyle.onHover, invisibleHitStyle.onActive, invisibleHitStyle.onFocused
        })
        {
            state.background = clearTex;
            state.textColor = clearText;
        }
        invisibleHitStyle.border = new RectOffset(0, 0, 0, 0);
        invisibleHitStyle.margin = new RectOffset(0, 0, 0, 0);
        invisibleHitStyle.padding = new RectOffset(0, 0, 0, 0);
        return invisibleHitStyle;
    }

    // Fully manual button: hover is detected by us (mouse position vs. rect) and drawn
    // with a color we pick, via GUI.Label/DrawTexture which never react to mouse-over
    // on their own. The invisible GUI.Button on top exists purely to detect the click.
    public static bool FlatButton(Rect rect, string text, int fontSize = 14)
    {
        if (flatFillTex == null) flatFillTex = SolidTexture(new Color(0.80f, 0.72f, 0.55f));
        if (flatHoverTex == null) flatHoverTex = SolidTexture(new Color(0.82f, 0.47f, 0.16f)); // warm amber highlight
        if (flatBorderTex == null) flatBorderTex = SolidTexture(new Color(0.32f, 0.24f, 0.15f));
        if (flatTextStyle == null)
        {
            flatTextStyle = new GUIStyle(GUI.skin.label) { fontSize = fontSize, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            flatTextStyle.normal.textColor = Ink;
        }
        if (flatTextHoverStyle == null)
        {
            flatTextHoverStyle = new GUIStyle(flatTextStyle);
            flatTextHoverStyle.normal.textColor = Color.black;
            flatTextHoverStyle.fontStyle = FontStyle.Bold;
        }

        bool hovered = rect.Contains(Event.current.mousePosition);

        GUI.DrawTexture(rect, flatBorderTex);
        Rect inner = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4);
        GUI.DrawTexture(inner, hovered ? flatHoverTex : flatFillTex);
        GUI.Label(inner, text, hovered ? flatTextHoverStyle : flatTextStyle);

        return GUI.Button(rect, GUIContent.none, InvisibleHitStyle());
    }

    // GUI.BeginScrollView auto-generates a scrollbar from the SHARED, GLOBAL GUI.skin --
    // its thumb has its own built-in hover highlight that isn't exposed as a parameter
    // anywhere, so the only way to kill it is to patch the shared skin style directly.
    // Must run inside an OnGUI call (GUI.skin throws outside one); the flag makes it a no-op
    // after the first call.
    private static bool skinPatched;

    public static void EnsureNoScrollbarHover()
    {
        if (skinPatched) return;
        skinPatched = true;

        KillHover(GUI.skin.verticalScrollbarThumb);
        KillHover(GUI.skin.horizontalScrollbarThumb);
        KillHover(GUI.skin.verticalScrollbar);
        KillHover(GUI.skin.horizontalScrollbar);
        KillHover(GUI.skin.verticalScrollbarUpButton);
        KillHover(GUI.skin.verticalScrollbarDownButton);
        KillHover(GUI.skin.horizontalScrollbarLeftButton);
        KillHover(GUI.skin.horizontalScrollbarRightButton);
    }

    static void KillHover(GUIStyle s)
    {
        if (s == null) return;
        s.hover.background = s.normal.background;
        s.active.background = s.normal.background;
        s.focused.background = s.normal.background;
        s.onNormal.background = s.normal.background;
        s.onHover.background = s.normal.background;
        s.onActive.background = s.normal.background;
        s.onFocused.background = s.normal.background;
        s.hover.textColor = s.normal.textColor;
        s.active.textColor = s.normal.textColor;
    }

    // Reverted: keep the plain default OS cursor everywhere. These stay as no-ops so
    // the call sites in GameIntro/TypewriterInteract/LetterArchive don't need editing.
    public static void ShowPaperCursor()
    {
    }

    public static void ShowDefaultCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
