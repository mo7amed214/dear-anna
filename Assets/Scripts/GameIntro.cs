using UnityEngine;
using UnityEngine.InputSystem;

public class GameIntro : MonoBehaviour
{
    public float delayBeforeLetter = 1.5f;

    // Assigned in the inspector: the 6 voice-acted parts of Anna's letter, played in order once opened.
    public AudioClip[] letterAudioParts;
    public Texture2D bloodySundayImage;

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

    private AudioSource audioSource;
    private bool audioSequenceStarted;
    private int currentAudioPart = -1;
    private const int BloodySundayPartIndex = 1; // "second part of the letter", zero-based
    private const float BloodySundayCueTime = 14f;
    private const float BloodySundayDisplayDuration = 7f;
    private const float BloodySundayFadeDuration = 0.5f;
    private bool bloodySundayShownThisPart;
    private float bloodySundayImageTimer;

    void Awake()
    {
        // Game state must not depend on Unity's domain-reload behavior to reset --
        // this is the one guaranteed "start of a fresh session" hook, so reset here explicitly.
        GameState.AnnaLetterRead = false;
        GameState.IsUIOpen = false;
        GameState.Archive.Clear();

        paperTexture = PaperUtil.MakePaperTexture(512, 384);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.loop = false;
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

        // Deliberately E-only, not click: the player may want to click other things
        // (like starting music on the gramophone) before opening the letter.
        if (!isReading && !GameState.AnnaLetterRead && !GameState.IsUIOpen)
        {
            bool pressedE = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            if (pressedE)
            {
                OpenLetter();
            }
        }

        if (isReading && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseLetter();
        }

        UpdateAudioSequence();

        if (bloodySundayImageTimer > 0f)
        {
            bloodySundayImageTimer -= Time.deltaTime;
        }
    }

    void UpdateAudioSequence()
    {
        if (!audioSequenceStarted || currentAudioPart < 0) return;

        // Bloody Sunday cue: 14 seconds into the second audio part specifically.
        if (currentAudioPart == BloodySundayPartIndex && !bloodySundayShownThisPart
            && audioSource.isPlaying && audioSource.time >= BloodySundayCueTime)
        {
            bloodySundayShownThisPart = true;
            bloodySundayImageTimer = BloodySundayDisplayDuration;
        }

        if (!audioSource.isPlaying)
        {
            PlayNextAudioPart();
        }
    }

    void StartAudioSequence()
    {
        if (letterAudioParts == null || letterAudioParts.Length == 0) return;
        audioSequenceStarted = true;
        currentAudioPart = -1;
        bloodySundayShownThisPart = false;
        PlayNextAudioPart();
    }

    void PlayNextAudioPart()
    {
        currentAudioPart++;
        if (currentAudioPart >= letterAudioParts.Length)
        {
            audioSequenceStarted = false;
            currentAudioPart = -1;
            return;
        }

        if (currentAudioPart == BloodySundayPartIndex)
        {
            bloodySundayShownThisPart = false;
        }

        AudioClip clip = letterAudioParts[currentAudioPart];
        if (clip == null)
        {
            PlayNextAudioPart();
            return;
        }

        audioSource.clip = clip;
        audioSource.Play();
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

        if (!audioSequenceStarted && currentAudioPart < 0)
        {
            StartAudioSequence();
        }
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
                    "A letter has been slipped under the door. (Press E to read)", PaperUtil.PromptStyle());
            }
        }
        else
        {
            float panelWidth = 640f;
            float panelHeight = 560f;
            Rect panel = new Rect(Screen.width / 2 - panelWidth / 2, Screen.height / 2 - panelHeight / 2, panelWidth, panelHeight);
            GUI.DrawTexture(panel, paperTexture);

            Rect viewRect = new Rect(panel.x + 35, panel.y + 25, panel.width - 70, panel.height - 100);
            GUIStyle bodyStyle = PaperUtil.InkStyle(17);
            float textHeight = bodyStyle.CalcHeight(new GUIContent(AnnaLetter), viewRect.width - 20);
            Rect contentRect = new Rect(0, 0, viewRect.width - 20, Mathf.Max(viewRect.height, textHeight + 20));

            scroll = GUI.BeginScrollView(viewRect, scroll, contentRect);
            GUI.Label(new Rect(0, 0, contentRect.width, contentRect.height), AnnaLetter, bodyStyle);
            GUI.EndScrollView();

            if (PaperUtil.FlatButton(new Rect(panel.x + panel.width / 2f - 75, panel.y + panel.height - 55, 150, 36), "Fold letter away"))
            {
                CloseLetter();
            }
        }

        // Drawn last so it sits fully on top of the letter, fullscreen, with a fade transition in and out.
        DrawBloodySundayOverlay();
    }

    void DrawBloodySundayOverlay()
    {
        if (bloodySundayImageTimer <= 0f || bloodySundayImage == null) return;

        float elapsed = BloodySundayDisplayDuration - bloodySundayImageTimer;
        float alpha = 1f;
        if (elapsed < BloodySundayFadeDuration)
        {
            alpha = elapsed / BloodySundayFadeDuration;
        }
        else if (bloodySundayImageTimer < BloodySundayFadeDuration)
        {
            alpha = bloodySundayImageTimer / BloodySundayFadeDuration;
        }
        alpha = Mathf.Clamp01(alpha);

        Rect full = new Rect(0, 0, Screen.width, Screen.height);
        Color prevColor = GUI.color;

        GUI.color = new Color(0f, 0f, 0f, alpha);
        GUI.DrawTexture(full, Texture2D.whiteTexture);

        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.DrawTexture(full, bloodySundayImage, ScaleMode.ScaleAndCrop);

        GUI.color = prevColor;
    }
}
