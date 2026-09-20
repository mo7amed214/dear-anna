using UnityEngine;
using UnityEngine.InputSystem;

public class GameIntro : MonoBehaviour
{
    public float delayBeforeLetter = 1.5f;

    private const string AnnaTitle = "From Anna";

    private static readonly string AnnaLetter =
        "Dear Stanislav,\n\n" +
        "I do not know if this will reach you before the post is stopped again -- twice this month the trains did not run, and twice I stood at the window watching for a rider who never came. But I have to write, because the quiet between your letters has grown longer than the distance, and I have started filling it with thoughts I am not sure are fair to you.\n\n" +
        "You will have heard, even shut away in your rooms, what happened before the Winter Palace in January. I did not see it, but Pyotr Fedorovich's nephew did, and he has not been himself since. The workers walked to ask the Tsar for bread and were answered with rifles. Your mother says they brought it on themselves. I will admit to you, and to no one else, that I am no longer sure she is right. I think of the women I know who go hungry so that their husbands can keep striking, and I cannot make myself believe, as she does, that they are simply fools being led astray.\n\n" +
        "I tell you this because I do not want to become a stranger to you in the things that matter, only in the ordinary business of days, which cannot be helped. The house is cold, and the Petrovna girl has gone to the factory towns, where they say the wages are better even now, even with soldiers in the streets. Your brother writes that the shop has been closed eleven days this month. I do not know if that is the strikes or something else he is not telling either of us.\n\n" +
        "The children still ask after you every night, though I suspect by spring they will ask less. I have not decided whether that frightens me more than anything happening in the streets.\n\n" +
        "Write to me honestly, Stanislav. Not what you think I want to hear, and not what your mother would have you say. Tell me plainly what you believe is happening to us -- to the country, and to the two of us inside it. I would rather hear your uncertainty stated plainly than your comfort delivered dishonestly.\n\n" +
        "I think of you at that desk of yours, further from the shouting than any of us, and I wonder sometimes whether that is a mercy or the cruelest part of where you have ended up.\n\n" +
        "Yours, whatever the post still allows,\nAnna";

    private bool letterArrived;
    private bool isReading;
    private float timer;
    private Vector2 scroll;

    private FirstPersonMove fpm;
    private Texture2D paperTexture;

    void Awake()
    {
        paperTexture = PaperUtil.MakePaperTexture(512, 384);
    }

    void Update()
    {
        if (!letterArrived)
        {
            timer += Time.deltaTime;
            if (timer >= delayBeforeLetter)
            {
                letterArrived = true;
            }
            return;
        }

        // This is the game's opening beat, so it triggers on any click, or on E,
        // rather than requiring the player to find and look exactly at the door.
        if (!isReading && !GameState.AnnaLetterRead && !GameState.IsUIOpen)
        {
            bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool pressedE = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            if (clicked || pressedE)
            {
                OpenLetter();
            }
        }

        if (isReading && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseLetter();
        }
    }

    void OpenLetter()
    {
        isReading = true;
        GameState.IsUIOpen = true;

        GameObject player = GameObject.Find("Player");
        fpm = player != null ? player.GetComponent<FirstPersonMove>() : null;
        if (fpm != null) fpm.canLook = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PaperUtil.ShowPaperCursor();
    }

    void CloseLetter()
    {
        isReading = false;

        if (!GameState.AnnaLetterRead)
        {
            GameState.AnnaLetterRead = true;
            GameState.Archive.Add(new GameState.LetterEntry { title = AnnaTitle, body = AnnaLetter, incoming = true });
        }

        GameState.IsUIOpen = false;
        if (fpm != null) fpm.canLook = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        PaperUtil.ShowDefaultCursor();
    }

    void OnGUI()
    {
        PaperUtil.EnsureNoScrollbarHover();
        if (!letterArrived) return;

        if (!isReading)
        {
            if (!GameState.AnnaLetterRead)
            {
                GUI.Label(new Rect(Screen.width / 2 - 220, Screen.height - 45, 440, 30),
                    "A letter has been slipped under the door. (Click or press E to read)", PaperUtil.PromptStyle());
            }
            return;
        }

        float panelWidth = 640f;
        float panelHeight = 560f;
        Rect panel = new Rect(Screen.width / 2 - panelWidth / 2, Screen.height / 2 - panelHeight / 2, panelWidth, panelHeight);
        GUI.DrawTexture(panel, paperTexture);

        Rect viewRect = new Rect(panel.x + 35, panel.y + 25, panel.width - 70, panel.height - 100);
        Rect contentRect = new Rect(0, 0, viewRect.width - 20, 720);

        scroll = GUI.BeginScrollView(viewRect, scroll, contentRect);
        GUI.Label(new Rect(0, 0, contentRect.width, contentRect.height), AnnaLetter, PaperUtil.InkStyle(17));
        GUI.EndScrollView();

        if (PaperUtil.FlatButton(new Rect(panel.x + panel.width / 2f - 75, panel.y + panel.height - 55, 150, 36), "Fold letter away"))
        {
            CloseLetter();
        }
    }
}
