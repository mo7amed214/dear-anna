using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// On the bed: look at it and press E to go to sleep. Fades to black, then crosses
// into the dream -- the bedroom chandelier starts flickering and the door swings
// open onto the dream room beyond -- and fades back in.
public class SleepInteract : MonoBehaviour
{
    public float interactDistance = 2.5f;
    public float fadeDuration = 1.2f;
    public float holdBlackDuration = 0.6f;

    private bool lookingAtBed;
    private bool isSleeping;
    private float fadeAlpha;
    private FirstPersonMove fpm;
    private AudioSource musicSource;
    private float musicStartVolume;
    private MusicPlayer musicPlayer;

    void Update()
    {
        if (GameState.IsDreaming || isSleeping) return;

        // No dream to have until Anna's letter has actually been read -- the bed simply
        // doesn't offer anything before that.
        if (!GameState.AnnaLetterRead) { lookingAtBed = false; return; }

        lookingAtBed = CheckGaze();
        if (lookingAtBed && !GameState.IsUIOpen && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            StartCoroutine(SleepRoutine());
        }
    }

    bool CheckGaze()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            return hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform);
        }
        return false;
    }

    IEnumerator SleepRoutine()
    {
        isSleeping = true;
        GameState.IsUIOpen = true;

        GameObject player = GameObject.Find("Player");
        fpm = player != null ? player.GetComponent<FirstPersonMove>() : null;
        if (fpm != null) { fpm.canLook = false; fpm.canMove = false; }

        var gramophone = GameObject.Find("Gramophone");
        musicSource = gramophone != null ? gramophone.GetComponent<AudioSource>() : null;
        musicPlayer = gramophone != null ? gramophone.GetComponent<MusicPlayer>() : null;
        musicStartVolume = musicSource != null ? musicSource.volume : 0f;

        // Screen fades to black and the gramophone fades out together -- it would break
        // the dream if the music just kept playing underneath it.
        yield return FadeOut(fadeDuration);
        yield return new WaitForSeconds(holdBlackDuration);

        // The room resets for the dream: the gramophone's spinning record stops (not just its
        // volume), and Anna's letter -- read or not -- has no voice in a nightmare.
        if (musicPlayer != null) musicPlayer.StopForSleep();
        var gameIntro = Object.FindAnyObjectByType<GameIntro>();
        if (gameIntro != null) gameIntro.StopLetterAudio();

        GameState.IsDreaming = true;

        var chandelier = GameObject.Find("Chandelier");
        if (chandelier != null)
        {
            var flicker = chandelier.GetComponent<ChandelierFlicker>();
            if (flicker == null) flicker = chandelier.AddComponent<ChandelierFlicker>();
            flicker.enabled = true;
        }

        var dreamDoor = Object.FindAnyObjectByType<DreamDoor>();
        if (dreamDoor != null) dreamDoor.Open();

        // The camera stays put, in Stanislav's own first-person view, the whole time --
        // door.Open()'s swing is slow enough (see DreamDoor.swingDuration) that it's still
        // clearly mid-motion once the fade lifts, so the player wakes up and watches it
        // finish opening rather than finding it already open.
        yield return Fade(1f, 0f, fadeDuration);

        if (fpm != null) { fpm.canLook = true; fpm.canMove = true; }
        GameState.IsUIOpen = false;
        isSleeping = false;
    }

    IEnumerator FadeOut(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            fadeAlpha = p;
            if (musicSource != null) musicSource.volume = Mathf.Lerp(musicStartVolume, 0f, p);
            yield return null;
        }
        fadeAlpha = 1f;
        if (musicSource != null)
        {
            musicSource.volume = 0f;
            musicSource.Stop();
        }
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            fadeAlpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        fadeAlpha = to;
    }

    void OnGUI()
    {
        if (!isSleeping && lookingAtBed && !GameState.IsUIOpen && !GameState.IsDreaming)
        {
            GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height - 45, 200, 30), "Press E to sleep", PaperUtil.PromptStyle());
        }

        if (fadeAlpha > 0f)
        {
            Color prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;
        }
    }
}
