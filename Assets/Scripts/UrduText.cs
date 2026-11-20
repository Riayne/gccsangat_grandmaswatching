// All on-screen text lives here so it is easy to change (the game name is not final yet).
public static class UrduText
{
    public const string Title = "شش! دادی دیکھ رہی ہے";
    public const string Tagline = "دادی دیکھ رہی ہے... ہاتھ مت لگانا!";
    public static readonly string[] HowTo =
    {
        "اسکرین دبائے رکھیں: کوکیاں کھائیں",
        "قدموں کی آواز آئے تو انگلی ہٹا لیں",
        "دروازہ چھو کر بند کریں",
        "کھڑکی چھو کر باہر کود جائیں",
    };
    public const string Start = "شروع کریں";

    public const string Cookies = "کوکیاں: ";
    public const string Best = "بہترین: ";
    public const string BoxLabel = "کرمبل کی کوکیاں";

    public static readonly string[] Praise = { "شاباش!", "واہ!" };
    public static readonly string[] DadiWatching = { "کون ہے وہاں؟", "بیٹا، کیا کر رہے ہو؟" };
    public static readonly string[] DadiFakeOut = { "ہمم... کچھ نہیں", "چپل کہاں ہے میری؟" };
    public const string DadiBlocked = "یہ دروازہ کس نے بند کیا؟";
    public const string DadiCaught = "میں نے سب دیکھ لیا!";

    public const string Caught = "پکڑے گئے!";
    public const string NewBest = "نیا ریکارڈ!";
    public const string Retry = "دوبارہ کوشش";

    // Converts 123 -> ۱۲۳ (Urdu digits).
    public static string Digits(int number)
    {
        char[] chars = number.ToString().ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] >= '0' && chars[i] <= '9')
                chars[i] = (char)('۰' + (chars[i] - '0'));
        }
        return new string(chars);
    }

    public static string Pick(string[] options)
    {
        return options[UnityEngine.Random.Range(0, options.Length)];
    }
}
