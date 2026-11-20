// English text: buttons, instructions and the score bar (HUD).
public static class EnglishText
{
    public const string Play = "PLAY";
    public const string HowToPlay = "HOW TO PLAY";
    public const string GotIt = "GOT IT!";
    public const string PlayAgain = "PLAY AGAIN";
    public const string Menu = "MENU";
    public const string SoundOn = "SOUND: ON";
    public const string SoundOff = "SOUND: OFF";

    public const string Cookies = "COOKIES";
    public const string Best = "BEST";
    public const string Night = "NIGHT";
    public const string Noise = "NOISE";
    public const string NewBest = "NEW BEST!";
    public const string TooLoud = "TOO LOUD!";

    public const string HintHold = "HOLD ANYWHERE TO EAT";
    public const string HintLetGo = "FOOTSTEPS! LET GO NOW!";
    public const string HintNice = "PHEW! KEEP EATING WHEN IT'S QUIET";
    public const string BlackoutSub = "EAT FAST! SHE CAN'T SEE!";
    public const string LightsBack = "LIGHTS COMING BACK!";
    public const string NearMissSub = "CLOSE CALL! +3";
    public const string GoldenSub = "+5 COOKIES";

    // How to Play cards: icon sprite name in Assets/Art, then the text.
    public static readonly string[,] HowTo =
    {
        { "bacha_eat", "HOLD anywhere to eat cookies. Hold longer to eat faster!" },
        { "icon_steps", "Hear FOOTSTEPS? LET GO to hide behind the charpai." },
        { "dadi", "Touching the screen while Dadi is looking = CAUGHT!" },
        { "door_panel", "TAP the door she is coming from to SHUT it." },
        { "window_jali", "TAP the window to jump out and stay safe for a moment." },
        { "golden_cookie", "Every NIGHT brings a new surprise. How far can you go?" },
    };
}
