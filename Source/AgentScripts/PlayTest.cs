// Automated playtest (run in Play mode):
//   unity command script run --file AgentScripts/PlayTest.cs --entry PlayTest.Run --timeout 150
// Simulates a finger with a virtual mouse and checks the core rules.
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class PlayTest
{
    static Mouse finger;
    static StringBuilder log;

    public static async Task<string> Run()
    {
        log = new StringBuilder();
        var gm = Object.FindAnyObjectByType<GameManager>();
        var dadi = gm.dadi;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        finger = InputSystem.AddDevice<Mouse>("TestFinger");
        try
        {
            Vector2 middle = gm.cam.WorldToScreenPoint(new Vector3(0, -4, 0));   // floor area, not a door
            Touch(false, middle);
            await Frames(3);
            PlayerPrefs.SetInt("TutorialDone", 1);
            if (gm.Current != GameManager.State.Playing)
            {
                gm.StartGame();
                float end = Time.realtimeSinceStartup + 2.8f;   // night card
                while (Time.realtimeSinceStartup < end) await Task.Yield();
            }

            // 1. Hold to eat.
            Touch(true, middle);
            await Seconds(1.2f);
            Log($"hold 1.2s -> score {gm.Score}, bacha {gm.bacha.Current}");

            // 2. Release when footsteps start, survive the watch.
            await Until(() => dadi.IsWarning, 15);
            Log($"warning from {dadi.CurrentSpawn.name}, releasing");
            Touch(false, middle);
            await Until(() => !dadi.IsWarning, 5);
            Log($"after warning: dadi {dadi.Current}, bacha {gm.bacha.Current}");
            await Until(() => dadi.Current == DadiController.State.Sleeping, 6);
            Log($"survived round: game {gm.Current}, score {gm.Score}");

            // 3. Close the door on her (retry until she comes from a door).
            bool doorTested = false;
            for (int round = 0; round < 6 && !doorTested; round++)
            {
                await Until(() => dadi.IsWarning, 15);
                var sp = dadi.CurrentSpawn;
                if (sp.kind == SpawnPoint.Kind.Door)
                {
                    Vector2 doorScreen = gm.cam.WorldToScreenPoint(sp.tapArea.center);
                    Touch(true, doorScreen); await Frames(2); Touch(false, doorScreen);
                    await Frames(2);
                    Log($"tapped {sp.name}: closed={sp.IsDoorClosed}, dadi {dadi.Current}");
                    await Until(() => dadi.Current == DadiController.State.Sleeping, 6);
                    Log($"after knock: game {gm.Current}, dadi seen={sp.dadi.activeSelf}");
                    doorTested = true;
                }
                else
                {
                    await Until(() => dadi.Current == DadiController.State.Sleeping, 6);
                }
            }

            // 4. Touch while she watches = caught.
            for (int round = 0; round < 8 && gm.Current == GameManager.State.Playing; round++)
            {
                await Until(() => dadi.IsWatching || dadi.Current == DadiController.State.Leaving, 15);
                if (dadi.IsWatching)
                {
                    Touch(true, middle);
                    await Frames(3);
                    Log($"touched while watching -> game {gm.Current}");
                    Touch(false, middle);
                }
                else await Until(() => dadi.Current == DadiController.State.Sleeping, 6);
            }
            await Seconds(2.5f);
            Log($"end: game {gm.Current}, score {gm.Score}, best {gm.Best}");
        }
        finally
        {
            InputSystem.RemoveDevice(finger);
        }
        return log.ToString();
    }

    static void Touch(bool down, Vector2 pos)
    {
        var state = new MouseState { position = pos }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left, down);
        InputSystem.QueueStateEvent(finger, state);
    }

    static void Log(string s) => log.AppendLine($"[{Time.time:F1}] {s}");

    static async Task Frames(int n)
    {
        int target = Time.frameCount + n;
        while (Time.frameCount < target) await Task.Yield();
    }

    static async Task Seconds(float s)
    {
        float end = Time.time + s;
        while (Time.time < end) await Task.Yield();
    }

    static async Task Until(System.Func<bool> cond, float timeout)
    {
        float end = Time.time + timeout;
        while (!cond() && Time.time < end) await Task.Yield();
    }
}
