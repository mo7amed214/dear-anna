using UnityEngine;
using UnityEngine.InputSystem;

public class LetterArchive : MonoBehaviour
{
    private bool isOpen;
    private int openIndex = -1;
    private Vector2 listScroll;
    private Vector2 readScroll;

    private FirstPersonMove fpm;
    private Texture2D paperTexture;

    void Awake()
    {
        paperTexture = PaperUtil.MakePaperTexture(512, 384);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
        {
            if (!isOpen && !GameState.IsUIOpen)
            {
                OpenArchive();
            }
            else if (isOpen)
            {
                CloseArchive();
            }
        }

        if (isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (openIndex >= 0)
            {
                openIndex = -1;
                readScroll = Vector2.zero;
            }
            else
            {
                CloseArchive();
            }
        }
    }

    void OpenArchive()
    {
        isOpen = true;
        openIndex = -1;
        GameState.IsUIOpen = true;

        GameObject player = GameObject.Find("Player");
        fpm = player != null ? player.GetComponent<FirstPersonMove>() : null;
        if (fpm != null) fpm.canLook = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PaperUtil.ShowPaperCursor();
    }

    void CloseArchive()
    {
        isOpen = false;
        openIndex = -1;
        GameState.IsUIOpen = false;

        if (fpm != null) fpm.canLook = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        PaperUtil.ShowDefaultCursor();
    }

    void OnGUI()
    {
        PaperUtil.EnsureNoScrollbarHover();
        if (!isOpen)
        {
            GUI.Label(new Rect(Screen.width - 230, 16, 210, 22), "Press L to open your letters", PaperUtil.HintStyle(13));
            return;
        }

        float panelWidth = 640f;
        float panelHeight = 560f;
        Rect panel = new Rect(Screen.width / 2f - panelWidth / 2f, Screen.height / 2f - panelHeight / 2f, panelWidth, panelHeight);
        GUI.DrawTexture(panel, paperTexture);

        if (openIndex < 0)
        {
            GUI.Label(new Rect(panel.x + 35, panel.y + 20, panel.width - 70, 30), "Correspondence", PaperUtil.GreetingStyle(20));

            Rect viewRect = new Rect(panel.x + 35, panel.y + 60, panel.width - 70, panel.height - 130);
            float rowHeight = 46f;
            Rect contentRect = new Rect(0, 0, viewRect.width - 20, Mathf.Max(viewRect.height, GameState.Archive.Count * rowHeight));

            listScroll = GUI.BeginScrollView(viewRect, listScroll, contentRect);
            if (GameState.Archive.Count == 0)
            {
                GUI.Label(new Rect(0, 0, contentRect.width, 30), "Nothing written or received yet.", PaperUtil.HintStyle());
            }
            for (int i = 0; i < GameState.Archive.Count; i++)
            {
                var e = GameState.Archive[i];
                string label = (e.incoming ? "Received -- " : "Sent -- ") + e.title;
                if (PaperUtil.FlatButton(new Rect(0, i * rowHeight, contentRect.width, 38), label))
                {
                    openIndex = i;
                    readScroll = Vector2.zero;
                }
            }
            GUI.EndScrollView();

            if (PaperUtil.FlatButton(new Rect(panel.x + panel.width / 2f - 75, panel.y + panel.height - 50, 150, 36), "Close"))
            {
                CloseArchive();
            }
        }
        else
        {
            var e = GameState.Archive[openIndex];
            GUI.Label(new Rect(panel.x + 35, panel.y + 20, panel.width - 70, 25), e.title, PaperUtil.GreetingStyle());

            Rect viewRect = new Rect(panel.x + 35, panel.y + 55, panel.width - 70, panel.height - 135);
            Rect contentRect = new Rect(0, 0, viewRect.width - 20, 720);
            readScroll = GUI.BeginScrollView(viewRect, readScroll, contentRect);
            GUI.Label(new Rect(0, 0, contentRect.width, contentRect.height), e.body, PaperUtil.InkStyle(16));
            GUI.EndScrollView();

            if (PaperUtil.FlatButton(new Rect(panel.x + panel.width / 2f - 75, panel.y + panel.height - 50, 150, 36), "Back to list"))
            {
                openIndex = -1;
            }
        }
    }
}
