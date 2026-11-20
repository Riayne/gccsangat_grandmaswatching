// The night-by-night progression. Each night has a cookie target and adds ONE new twist.
// Change the numbers here to make the game easier or harder.
public static class Nights
{
    // The night each feature starts.
    public const int Combo = 3;          // hold longer = eat faster, but noise wakes Dadi
    public const int GoldenCookie = 4;
    public const int Window = 5;         // Dadi can peek through the window (spawn unlock is on SpawnPoint)
    public const int Blackout = 6;       // load-shedding frenzy
    public const int DoubleCheck = 7;    // Dadi turns back after leaving (sofa spawn unlocks too)
    public const int Chotu = 8;          // little brother snitches

    // Total cookies needed to finish each night. After the last one, every night needs 50 more.
    static readonly int[] targets = { 10, 25, 45, 70, 100, 135, 175, 220 };

    public static int Target(int night)
    {
        if (night <= targets.Length) return targets[night - 1];
        return targets[targets.Length - 1] + (night - targets.Length) * 50;
    }

    public static int PreviousTarget(int night)
    {
        return night <= 1 ? 0 : Target(night - 1);
    }

    // Text on the card shown at the start of each night (English: these are instructions).
    public static string Card(int night)
    {
        switch (night)
        {
            case 1: return "HOLD to eat. LET GO when you hear footsteps!";
            case 2: return "TIP: TAP the door she is coming from to SHUT it!";
            case 3: return "Hold longer to eat FASTER (x2, x3)... but crunching is LOUD!";
            case 4: return "GOLDEN COOKIES! Be eating when one shines: +5";
            case 5: return "Dadi can peek through the WINDOW now. Tap it to jump out!";
            case 6: return "LOAD-SHEDDING! When the lights go out, EAT FAST!";
            case 7: return "Dadi got suspicious. Listen for her COUGH... she may come back!";
            case 8: return "Uh oh, CHOTU is awake. Don't let him see you eating!";
            default: return "Dadi had her chai. She is FASTER than ever!";
        }
    }
}
