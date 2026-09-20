using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class TypewriterInteract : MonoBehaviour
{
    public float interactDistance = 2.5f;

    private const string SentTitle = "To Anna";

    // The passage is split around each blank. segments.Length == blankOptions.Length + 1.
    private static readonly string[] segments =
    {
        "Dear Anna,\n\nI have heard about the Palace, though the papers here print only what they are permitted to. What happened to those people was ",
        ".\n\nAs for my mother, I think on this she is ",
        " than either of us has been.\n\nAbout the two of us -- I have not been as honest with you as I ",
        ".\n\nWhatever comes for this country now, I think of you ",
        ", and I wonder too whether this desk is a mercy or the cruelest part of where I have ended up.\n\n-- Stanislav"
    };

    private static readonly string[][] blankOptions =
    {
        new[] { "a crime someone will answer for", "a tragedy, whatever the cause", "more than I know how to judge from this far away" },
        new[] { "more certain", "more frightened", "further from honest" },
        new[] { "should have been", "could have been", "am only now learning how to be" },
        new[] { "more than I write down", "with a fear I cannot name", "every hour the post is late" }
    };

    private bool isWriting;
    private bool lookingAtTypewriter;
    private int currentBlank;
    private string[] chosen;
    private bool posted;
    private Vector2 scroll;
    private GameState.LetterEntry sentEntry;

    private FirstPersonMove fpm;
    private Texture2D paperTexture;

    void Awake()
    {
        paperTexture = PaperUtil.MakePaperTexture(512, 384);
    }

    void Update()
    {
        if (!isWriting)
        {
            lookingAtTypewriter = CheckGaze();
            if (lookingAtTypewriter && GameState.AnnaLetterRead && !GameState.IsUIOpen
                && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                StartWriting();
            }
        }

        if (isWriting && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            StopWriting();
        }
    }

    bool CheckGaze()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            return hit.collider.GetComponentInParent<TypewriterInteract>() == this;
        }
        return false;
    }

    void StartWriting()
    {
        isWriting = true;
        GameState.IsUIOpen = true;
        currentBlank = 0;
        posted = false;
        chosen = new string[blankOptions.Length];
        scroll = Vector2.zero;

        GameObject player = GameObject.Find("Player");
        fpm = player != null ? player.GetComponent<FirstPersonMove>() : null;
        if (fpm != null) fpm.canLook = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PaperUtil.ShowPaperCursor();
    }

    void StopWriting()
    {
        isWriting = false;
        GameState.IsUIOpen = false;

        if (fpm != null) fpm.canLook = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        PaperUtil.ShowDefaultCursor();
    }

    void ChooseOption(string option)
    {
        chosen[currentBlank] = option;
        currentBlank++;
        if (currentBlank >= blankOptions.Length)
        {
            posted = true;
            scroll = Vector2.zero;

            if (sentEntry == null)
            {
                sentEntry = new GameState.LetterEntry { title = SentTitle, incoming = false };
                GameState.Archive.Add(sentEntry);
            }
            sentEntry.body = BuildSentence();
        }
    }

    void RestartLetter()
    {
        currentBlank = 0;
        posted = false;
        chosen = new string[blankOptions.Length];
        scroll = Vector2.zero;
    }

    string BuildSentence()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < segments.Length; i++)
        {
            sb.Append(segments[i]);
            if (i < blankOptions.Length)
            {
                sb.Append(chosen[i] != null ? chosen[i] : "_____");
            }
        }
        return sb.ToString();
    }

    void OnGUI()
    {
        PaperUtil.EnsureNoScrollbarHover();
        if (!isWriting)
        {
            if (lookingAtTypewriter && !GameState.IsUIOpen)
            {
                string msg = GameState.AnnaLetterRead ? "Click the typewriter to answer Anna" : "Read Anna's letter first";
                GUI.Label(new Rect(Screen.width / 2 - 140, Screen.height - 45, 280, 30), msg, PaperUtil.PromptStyle());
            }
            return;
        }

        float panelWidth = 640f;
        float panelHeight = 560f;
        Rect panel = new Rect(Screen.width / 2 - panelWidth / 2, Screen.height / 2 - panelHeight / 2, panelWidth, panelHeight);
        GUI.DrawTexture(panel, paperTexture);

        if (posted)
        {
            // Holding the finished letter, the same full way Anna's letter is held and read.
            Rect viewRect = new Rect(panel.x + 35, panel.y + 25, panel.width - 70, panel.height - 130);
            Rect contentRect = new Rect(0, 0, viewRect.width - 20, 720);
            scroll = GUI.BeginScrollView(viewRect, scroll, contentRect);
            GUI.Label(new Rect(0, 0, contentRect.width, contentRect.height), BuildSentence(), PaperUtil.InkStyle(17));
            GUI.EndScrollView();

            GUI.Label(new Rect(panel.x + 35, panel.y + panel.height - 90, panel.width - 70, 20), "Posted. It cannot be changed now.", PaperUtil.HintStyle());

            if (PaperUtil.FlatButton(new Rect(panel.x + panel.width / 2f - 160, panel.y + panel.height - 55, 150, 36), "Write another"))
            {
                RestartLetter();
            }
            if (PaperUtil.FlatButton(new Rect(panel.x + panel.width / 2f + 10, panel.y + panel.height - 55, 150, 36), "Fold letter away"))
            {
                StopWriting();
            }
        }
        else
        {
            Rect viewRect = new Rect(panel.x + 35, panel.y + 25, panel.width - 70, 300);
            Rect contentRect = new Rect(0, 0, viewRect.width - 20, 340);
            scroll = GUI.BeginScrollView(viewRect, scroll, contentRect);
            GUI.Label(new Rect(0, 0, contentRect.width, contentRect.height), BuildSentence(), PaperUtil.InkStyle(17));
            GUI.EndScrollView();

            float belowY = panel.y + 335;
            GUI.Label(new Rect(panel.x + 35, belowY, panel.width - 70, 20), "Choose a word:", PaperUtil.HintStyle());

            string[] opts = blankOptions[currentBlank];
            float gap = 12f;
            float buttonWidth = (panel.width - 70f - gap * (opts.Length - 1)) / opts.Length;
            for (int i = 0; i < opts.Length; i++)
            {
                Rect btn = new Rect(panel.x + 35 + i * (buttonWidth + gap), belowY + 25, buttonWidth, 56);
                if (PaperUtil.FlatButton(btn, opts[i]))
                {
                    ChooseOption(opts[i]);
                }
            }

            GUI.Label(new Rect(panel.x + 35, belowY + 90, panel.width - 70, 20),
                string.Format("Blank {0} of {1}", currentBlank + 1, blankOptions.Length), PaperUtil.HintStyle());
        }
    }
}
