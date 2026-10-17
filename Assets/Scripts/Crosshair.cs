using UnityEngine;

// A fixed dot at the center of the screen -- with this many closely-packed interactable
// props around the house, the player needs a precise, unambiguous mark for what their
// gaze raycast is actually centered on. Hidden whenever a full-screen paper UI is open,
// matching every other on-screen prompt's convention (see GameState.IsUIOpen).
public class Crosshair : MonoBehaviour
{
    [Range(2f, 12f)] public float dotSize = 6f;
    public Color dotColor = new Color(0.95f, 0.95f, 0.92f, 0.85f);

    private Texture2D dotTexture;

    void Awake()
    {
        const int size = 16;
        dotTexture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = Mathf.Clamp01(1f - dist / radius);
                alpha *= alpha; // soften the edge into a small clean circle rather than a sharp disc
                dotTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        dotTexture.Apply();
    }

    void OnGUI()
    {
        if (GameState.IsUIOpen) return;

        Color prev = GUI.color;
        GUI.color = dotColor;
        Rect rect = new Rect(Screen.width / 2f - dotSize / 2f, Screen.height / 2f - dotSize / 2f, dotSize, dotSize);
        GUI.DrawTexture(rect, dotTexture);
        GUI.color = prev;
    }
}
