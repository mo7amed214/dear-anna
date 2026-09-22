using UnityEngine;
using UnityEngine.InputSystem;

public class MusicPlayer : MonoBehaviour
{
    public float interactDistance = 2.5f;
    public AudioClip[] tracks;
    public string[] trackTitles;

    private AudioSource audioSource;
    private Animation anim;
    private int currentTrack = -1;
    private bool lookingAtPlayer;
    private bool wasPlaying;
    private float messageTimer;
    private string message = "";

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 1.5f;
        audioSource.maxDistance = 8f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.loop = false;
        audioSource.playOnAwake = false;

        anim = GetComponent<Animation>();
    }

    void Update()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        lookingAtPlayer = Physics.Raycast(ray, out RaycastHit hit, interactDistance)
            && hit.collider.GetComponentInParent<MusicPlayer>() == this;

        if (messageTimer > 0f) messageTimer -= Time.deltaTime;

        if (lookingAtPlayer && !GameState.IsUIOpen && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            NextTrack();
        }

        // Auto-advance when a record finishes on its own, so the room doesn't fall silent.
        if (wasPlaying && !audioSource.isPlaying)
        {
            NextTrack();
        }
        wasPlaying = audioSource.isPlaying;
    }

    void NextTrack()
    {
        if (tracks == null || tracks.Length == 0) return;

        currentTrack = (currentTrack + 1) % tracks.Length;
        audioSource.clip = tracks[currentTrack];
        audioSource.Play();

        if (anim != null && anim.GetClip("Play") != null)
        {
            anim.Stop();
            anim.Play("Play");
        }

        string title = (trackTitles != null && currentTrack < trackTitles.Length) ? trackTitles[currentTrack] : tracks[currentTrack].name;
        message = $"Now playing: {title}";
        messageTimer = 3.5f;
    }

    void OnGUI()
    {
        if (lookingAtPlayer && !GameState.IsUIOpen)
        {
            string hint = currentTrack < 0 ? "Click to wind the gramophone" : "Click to change the record";
            GUI.Label(new Rect(Screen.width / 2 - 160, Screen.height - 45, 320, 30), hint, PaperUtil.PromptStyle());
        }
        else if (messageTimer > 0f)
        {
            GUI.Label(new Rect(Screen.width / 2 - 220, Screen.height - 45, 440, 30), message, PaperUtil.PromptStyle());
        }
    }
}
