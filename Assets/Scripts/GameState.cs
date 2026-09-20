using System.Collections.Generic;

public static class GameState
{
    public class LetterEntry
    {
        public string title;
        public string body;
        public bool incoming;
    }

    // True once the player has read Anna's opening letter.
    public static bool AnnaLetterRead = false;

    // True whenever a full-screen paper UI (a letter, the archive, or the typewriter) is open,
    // so other interactions don't fire underneath it at the same time.
    public static bool IsUIOpen = false;

    // Every letter read or sent so far, for the correspondence archive.
    public static List<LetterEntry> Archive = new List<LetterEntry>();
}
