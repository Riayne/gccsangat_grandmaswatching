// Automated checks for each mechanic (run in Play mode):
//   unity command script run --file AgentScripts/MechanicsTest.cs --entry MechanicsTest.Run --args '["combo"]' --timeout 120
// Scenarios: nearmiss, combo, golden, blackout, chotu, doublecheck, nightup, howto
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public static class MechanicsTest
{
    static Mouse finger;
    static StringBuilder log;
    static GameManager gm;
    static DadiController dadi;

    public static async Task<string> Run(string scenario)
    {
        log = new StringBuilder();
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        PlayerPrefs.SetInt("TutorialDone", scenario == "tutorial" ? 0 : 1);
        finger = InputSystem.AddDevice<Mouse>("TestFinger");
        try
        {
            SceneManager.LoadScene(0);
            await Frames(5);
            gm = Object.FindAnyObjectByType<GameManager>();
            dadi = gm.dadi;
            switch (scenario)
            {
                case "howto": await HowTo(); break;
                case "nearmiss": await NearMiss(); break;
                case "combo": await Combo(); break;
                case "golden": await Golden(); break;
                case "blackout": await Blackout(); break;
                case "chotu": await Chotu(); break;
                case "doublecheck": await DoubleCheck(); break;
                case "nightup": await NightUp(); break;
                case "tutorial": await Tutorial(); break;
                case "nightcard": await NightCardShot(); break;
                case "caught": await CaughtShot(); break;
            }
        }
        finally
        {
            Touch(false);
            InputSystem.RemoveDevice(finger);
        }
        return log.ToString();
    }

    static async Task StartAt(int night, float firstSleep)
    {
        dadi.firstSleep = firstSleep;
        gm.StartGame();
        await Seconds(2.8f, realtime: true);   // night card
        typeof(GameManager).GetProperty("Night").SetValue(gm, night);
        Log($"playing, night set to {gm.Night}, state {gm.Current}");
    }

    static async Task HowTo()
    {
        var root = Object.FindAnyObjectByType<UIDocument>().rootVisualElement;
        root.Q("howToPanel").RemoveFromClassList("hidden");
        Log("how-to rows: " + root.Q("howToRows").childCount);
        Time.timeScale = 0f;   // freeze for the screenshot; restored by the next test's scene load
    }

    static async Task NearMiss()
    {
        await StartAt(1, 1.5f);
        Touch(true);
        await Until(() => dadi.IsWarning, 10);
        float warning = Mathf.Max(dadi.warningMinimum, Mathf.Lerp(dadi.warningEasy, dadi.warningHard, gm.Difficulty));
        await Seconds(warning - 0.12f);
        int before = gm.Score;
        Touch(false);
        await Until(() => dadi.IsWatching, 2);
        await Frames(2);
        Log($"released 0.12s before look: watching={dadi.IsWatching}, game={gm.Current}, score {before} -> {gm.Score} (expect +3)");
    }

    static async Task Combo()
    {
        await StartAt(3, 30f);
        Touch(true);
        for (int i = 1; i <= 5; i++)
        {
            await Seconds(1f);
            Log($"held {i}s: x{gm.Multiplier}, noise {gm.Noise:F2}, score {gm.Score}");
        }
        Touch(false);
        await Seconds(1.5f);
        Log($"released 1.5s: x{gm.Multiplier}, noise {gm.Noise:F2}");
        Touch(true);
        await Until(() => dadi.IsWarning || gm.Noise >= 1f, 20);
        Log($"kept holding: noise {gm.Noise:F2}, dadi {dadi.Current} (expect woken early by noise)");
        Touch(false);
    }

    static async Task Golden()
    {
        gm.goldenEvery = new Vector2(1f, 1.2f);
        await StartAt(4, 30f);
        await Until(() => gm.cookieBox.GoldenActive, 5);
        Log($"golden visible: {gm.cookieBox.GoldenActive}");
        int before = gm.Score;
        Touch(true);
        await Frames(3);
        Touch(false);
        await Frames(2);
        Log($"tapped during golden: score {before} -> {gm.Score} (expect +5 or +6), golden now {gm.cookieBox.GoldenActive}");
    }

    static async Task Blackout()
    {
        dadi.blackoutChance = 1f;
        await StartAt(6, 0.5f);
        await Until(() => dadi.IsBlackout, 10);
        Log($"blackout started: {dadi.IsBlackout}");
        Touch(true);
        await Seconds(1.5f);
        Log($"eating in the dark: x{gm.Multiplier}, score {gm.Score}, game {gm.Current}");
        Time.timeScale = 0f;   // freeze for screenshot
    }

    static async Task Chotu()
    {
        dadi.chotuChance = 1f;
        dadi.blackoutChance = 0f;
        await StartAt(8, 0.5f);
        await Until(() => dadi.Current == DadiController.State.ChotuWatching, 10);
        Log($"chotu peeking from {dadi.CurrentSpawn.name}");
        await Until(() => dadi.IsChotuWatching, 2);
        Touch(true);   // he sees us eating
        await Until(() => dadi.IsWarning, 3);
        Log($"after eating in front of chotu: dadi {dadi.Current} (expect Warning)");
        Touch(false);
        await Until(() => dadi.IsWatching, 3);
        Log($"dadi came: watching={dadi.IsWatching}, game {gm.Current} (we let go, expect Playing)");
    }

    static async Task DoubleCheck()
    {
        dadi.doubleCheckChance = 1f;
        dadi.fakeChanceEasy = dadi.fakeChanceHard = 0f;
        dadi.blackoutChance = 0f;
        await StartAt(7, 0.5f);
        await Until(() => dadi.IsWatching, 10);
        Log("first look");
        await Until(() => !dadi.IsWatching, 3);
        Log($"she left: {dadi.Current}");
        await Until(() => dadi.IsWatching, 2);
        Log($"double-check look: watching={dadi.IsWatching}");
        Time.timeScale = 0f;
    }

    static async Task NightUp()
    {
        await StartAt(1, 30f);
        Touch(true);
        await Until(() => gm.Current == GameManager.State.NightCard, 10);
        Log($"score {gm.Score}: state {gm.Current}, night {gm.Night} (expect NightCard, 2)");
        Touch(false);
        await Seconds(2.8f, realtime: true);
        Log($"after card: state {gm.Current}");
    }

    static async Task Tutorial()
    {
        await StartAt(1, 1.5f);
        Touch(true);
        await Until(() => dadi.IsWarning, 10);
        await Frames(2);
        Log($"footsteps while eating: timeScale {Time.timeScale:F2} (expect 0.35)");
        Touch(false);
        await Frames(3);
        Log($"let go: timeScale {Time.timeScale:F2}, tutorial saved {PlayerPrefs.GetInt("TutorialDone")}");
    }

    static async Task NightCardShot()
    {
        gm.StartGame();
        await Frames(2);
        gm.StopAllCoroutines();   // keep the card on screen for the screenshot
        gm.ui.SetHud(45, 61, 3);
        gm.ui.ShowNightCard(3, Nights.Card(3));
        Log("night card showing");
    }

    static async Task CaughtShot()
    {
        dadi.fakeChanceEasy = dadi.fakeChanceHard = 0f;
        await StartAt(4, 0.5f);
        Touch(true);
        await Seconds(1.0f);
        await Until(() => dadi.IsWatching, 10);
        await Seconds(3.5f, realtime: true);
        Log($"game {gm.Current}, score {gm.Score}, rank {UrduText.RankFor(gm.Score)}");
    }

    static void Touch(bool down)
    {
        Vector2 pos = new Vector2(Screen.width * 0.5f, Screen.height * 0.35f);
        InputSystem.QueueStateEvent(finger, new MouseState { position = pos }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left, down));
    }

    static void Log(string s) => log.AppendLine($"[{Time.time:F1}] {s}");

    static async Task Frames(int n)
    {
        int target = Time.frameCount + n;
        while (Time.frameCount < target) await Task.Yield();
    }

    static async Task Seconds(float s, bool realtime = false)
    {
        float end = (realtime ? Time.realtimeSinceStartup : Time.time) + s;
        while ((realtime ? Time.realtimeSinceStartup : Time.time) < end) await Task.Yield();
    }

    static async Task Until(System.Func<bool> cond, float timeout)
    {
        float end = Time.realtimeSinceStartup + timeout;
        while (!cond() && Time.realtimeSinceStartup < end) await Task.Yield();
    }
}
