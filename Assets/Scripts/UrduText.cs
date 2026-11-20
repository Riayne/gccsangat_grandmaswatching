// Urdu text: title, Dadi's and Chotu's lines, praise and ranks.
// (Buttons, instructions and the score bar are in EnglishText.cs.)
public static class UrduText
{
    public const string Title = "شش! دادی دیکھ رہی ہے";
    public const string Tagline = "دادی دیکھ رہی ہے... ہاتھ مت لگانا!";
    public const string BoxLabel = "کرمبل کی کوکیاں";

    public static readonly string[] Praise = { "شاباش!", "واہ!" };
    public const string NearMiss = "بال بال بچے!";
    public const string Golden = "سنہری کوکی!";
    public const string PowerCut = "بجلی گئی!";

    public static readonly string[] DadiWatching = { "کون ہے وہاں؟", "بیٹا، کیا کر رہے ہو؟" };
    public static readonly string[] DadiFakeOut = { "ہمم... کچھ نہیں", "چپل کہاں ہے میری؟" };
    public const string DadiBlocked = "یہ دروازہ کس نے بند کیا؟";
    public const string DadiCaught = "میں نے سب دیکھ لیا!";
    public const string DadiDoubleCheck = "ایک منٹ...";
    public const string DadiTooLoud = "یہ کیا آواز ہے؟";

    public const string ChotuGiggle = "ہی ہی ہی!";
    public const string ChotuSnitch = "دادی! دادی!";

    public const string Caught = "پکڑے گئے!";

    // Rank shown at game over: (minimum cookies, title). Highest first.
    public static readonly (int min, string title)[] Ranks =
    {
        (150, "دادی کا بھی استاد!"),
        (100, "کوکی بادشاہ"),
        (60, "استاد جی"),
        (30, "چھپا رستم"),
        (10, "کوکی چور"),
        (0, "کچا چور"),
    };

    public static string RankFor(int cookies)
    {
        foreach (var r in Ranks)
            if (cookies >= r.min) return r.title;
        return Ranks[Ranks.Length - 1].title;
    }

    public static string Pick(string[] options)
    {
        return options[UnityEngine.Random.Range(0, options.Length)];
    }
}
