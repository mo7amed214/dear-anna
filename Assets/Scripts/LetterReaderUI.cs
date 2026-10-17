using UnityEngine;

// Shared renderer for "open letter" panels: the illegible handwritten scan on one side,
// a parchment-backed translation on the other, with the translation revealing letter by
// letter. Kept as a static draw call (state passed in) rather than its own MonoBehaviour
// so each letter's owner (e.g. GameIntro) can call it from inside its own OnGUI and
// keep control of draw order against its own extra effects.
public static class LetterReaderUI
{
    public class ReaderState
    {
        public float revealTimer;
        public int revealedChars;
        public Vector2 scroll;
        public string cachedBody;
    }

    // Characters per second -- the letter writes itself out one character at a time.
    private const float CharsPerSecond = 15f;

    public static void ResetReveal(ReaderState state)
    {
        state.cachedBody = null;
        state.revealTimer = 0f;
        state.revealedChars = 0;
        state.scroll = Vector2.zero;
    }

    static void EnsureBody(ReaderState state, string body)
    {
        if (state.cachedBody == body) return;

        state.cachedBody = body;
        state.revealedChars = 0;
        state.revealTimer = 0f;
        state.scroll = Vector2.zero;
    }

    public static bool IsFullyRevealed(ReaderState state)
    {
        return state.cachedBody != null && state.revealedChars >= state.cachedBody.Length;
    }

    public static void SkipToEnd(ReaderState state)
    {
        if (state.cachedBody != null) state.revealedChars = state.cachedBody.Length;
    }

    // Draws the paper + translation panel. Call every frame while the letter is open,
    // from the owner's own OnGUI so draw order against any extra overlay stays in its hands.
    public static void Draw(ReaderState state, string title, string body, Texture2D paperImage, Texture2D parchmentBackground)
    {
        EnsureBody(state, body);

        if (state.revealedChars < body.Length)
        {
            state.revealTimer += Time.deltaTime;
            int shouldHave = Mathf.FloorToInt(state.revealTimer * CharsPerSecond);
            state.revealedChars = Mathf.Min(shouldHave, body.Length);
        }

        // Paper on the left, translation on the right, with a real gap between them
        // so the two pieces of UI never overlap no matter the screen size.
        float margin = Screen.width * 0.06f;
        float gap = Screen.width * 0.03f;
        float halfWidth = (Screen.width - margin * 2f - gap) * 0.5f;
        float panelHeight = Screen.height * 0.8f;
        float top = (Screen.height - panelHeight) * 0.5f;

        Rect paperRect = new Rect(margin, top, halfWidth, panelHeight);
        Rect transRect = new Rect(margin + halfWidth + gap, top, halfWidth, panelHeight);

        if (paperImage != null)
        {
            float imgAspect = (float)paperImage.width / paperImage.height;
            float rectAspect = paperRect.width / paperRect.height;
            Rect fitted;
            if (imgAspect > rectAspect)
            {
                float h = paperRect.width / imgAspect;
                fitted = new Rect(paperRect.x, paperRect.y + (paperRect.height - h) / 2f, paperRect.width, h);
            }
            else
            {
                float w = paperRect.height * imgAspect;
                fitted = new Rect(paperRect.x + (paperRect.width - w) / 2f, paperRect.y, w, paperRect.height);
            }
            GUI.DrawTexture(fitted, paperImage, ScaleMode.StretchToFill);
        }

        if (parchmentBackground != null)
        {
            GUI.DrawTexture(transRect, parchmentBackground, ScaleMode.StretchToFill);
        }

        GUIStyle titleStyle = PaperUtil.InLetterFont(PaperUtil.GreetingStyle(22));
        GUI.Label(new Rect(transRect.x + 28, transRect.y + 20, transRect.width - 56, 32), title, titleStyle);

        // The not-yet-written remainder is still laid out, just fully transparent, so word
        // wrapping is fixed from the start and a half-typed word never jumps to the next line.
        string shown = body.Substring(0, state.revealedChars)
            + "<color=#00000000>" + body.Substring(state.revealedChars) + "</color>";
        GUIStyle bodyStyle = PaperUtil.InLetterFont(PaperUtil.InkStyle(18));
        bodyStyle.richText = true;
        Rect viewRect = new Rect(transRect.x + 28, transRect.y + 60, transRect.width - 56, transRect.height - 130);
        float textHeight = bodyStyle.CalcHeight(new GUIContent(body), viewRect.width - 20);
        Rect contentRect = new Rect(0, 0, viewRect.width - 20, Mathf.Max(viewRect.height, textHeight + 20));

        state.scroll = GUI.BeginScrollView(viewRect, state.scroll, contentRect);
        GUI.Label(new Rect(0, 0, contentRect.width, contentRect.height), shown, bodyStyle);
        GUI.EndScrollView();

        string hint = IsFullyRevealed(state) ? "Press Esc to fold the letter away" : "Press Esc to close";
        GUI.Label(new Rect(transRect.x + 28, transRect.y + transRect.height - 34, transRect.width - 56, 24), hint, PaperUtil.InLetterFont(PaperUtil.HintStyle(14)));
    }
}
