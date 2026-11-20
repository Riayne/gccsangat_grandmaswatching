using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Dadi's behaviour, as a simple coroutine state machine. Each "round" is one of:
//   normal:    Sleeping -> Warning (footsteps) -> Watching (or fake-out, or blocked by a closed door)
//   blackout:  load-shedding, lights out, she can't see (from Nights.Blackout)
//   chotu:     little brother peeks in; if he sees you eating he calls Dadi (from Nights.Chotu)
public class DadiController : MonoBehaviour
{
    public enum State { Idle, Sleeping, Warning, Watching, Leaving, Blackout, ChotuWatching }

    public SpawnPoint[] spawns;
    public GameManager game;
    public AudioManager audioManager;
    public UIController ui;

    [Header("Timings (seconds)")]
    public float firstSleep = 3f;
    public Vector2 sleepEasy = new Vector2(2.2f, 4.5f);   // min/max sleep at difficulty 0
    public Vector2 sleepHard = new Vector2(0.9f, 2.0f);   // min/max sleep at difficulty 1
    public float warningEasy = 1.4f;
    public float warningHard = 0.6f;
    public float warningMinimum = 0.5f;
    public Vector2 watchTime = new Vector2(1.0f, 1.8f);
    public int stepsPerWarning = 4;
    [Header("Chances")]
    public float fakeChanceEasy = 0.10f;
    public float fakeChanceHard = 0.35f;
    public float blackoutChance = 0.2f;
    public float chotuChance = 0.25f;
    public float doubleCheckChance = 0.35f;
    [Tooltip("How much a full noise meter speeds up her waking (1 = twice as fast).")]
    public float noiseWakeBoost = 2f;

    public State Current { get; private set; } = State.Idle;
    public SpawnPoint CurrentSpawn { get; private set; }
    public bool IsWatching => Current == State.Watching;
    public bool IsWarning => Current == State.Warning;
    public bool IsBlackout => Current == State.Blackout;
    public bool IsChotuWatching => Current == State.ChotuWatching && chotuLooking;

    SpawnPoint lastSpawn;
    bool blocked;
    bool chotuLooking;

    public void Begin()
    {
        Stop();
        StartCoroutine(Loop());
    }

    // Stops everything and hides all characters.
    public void Stop()
    {
        StopAllCoroutines();
        foreach (var s in spawns) s.HideAll();
        ui.HideBubble();
        ui.SetWatching(false);
        ui.SetBlackout(false);
        audioManager.SetFanOn(true);
        Current = State.Idle;
        chotuLooking = false;
    }

    // Stops Dadi where she is (used when the bacha is caught).
    public void Freeze()
    {
        StopAllCoroutines();
        if (CurrentSpawn != null)
        {
            CurrentSpawn.warningVisual.SetActive(false);
            if (CurrentSpawn.chotu != null) CurrentSpawn.chotu.SetActive(false);
        }
        ui.SetBlackout(false);
        audioManager.SetFanOn(true);
    }

    // Returns the door/window the player tapped, or null.
    public SpawnPoint TappedSpawn(Vector2 worldPoint)
    {
        foreach (var s in spawns)
        {
            if (s.kind == SpawnPoint.Kind.Sofa) continue;
            if (s.kind == SpawnPoint.Kind.Window && game.Night < Nights.Window) continue;
            if (s.Contains(worldPoint)) return s;
        }
        return null;
    }

    // The player closed a door. If Dadi is coming through it, her route is blocked.
    public void CloseDoor(SpawnPoint door)
    {
        if (door.IsDoorClosed) return;
        door.SetDoorClosed(true);
        audioManager.PlayPanned(audioManager.doorClose, door.pan);
        if (door == CurrentSpawn && Current == State.Warning) blocked = true;
    }

    IEnumerator Loop()
    {
        bool first = true;
        bool lastWasBlackout = false;
        while (true)
        {
            // SLEEPING: ambient only. Noise from greedy eating wakes her sooner.
            Current = State.Sleeping;
            foreach (var s in spawns) s.SetDoorClosed(false);
            float sleep = first ? firstSleep : Random.Range(
                Mathf.Lerp(sleepEasy.x, sleepHard.x, game.Difficulty),
                Mathf.Lerp(sleepEasy.y, sleepHard.y, game.Difficulty));
            first = false;
            bool tooLoud = false;
            float t = 0f;
            while (t < sleep)
            {
                t += Time.deltaTime * (1f + game.Noise * noiseWakeBoost);
                if (game.Noise >= 1f) { tooLoud = true; break; }
                yield return null;
            }

            if (tooLoud)
            {
                game.OnTooLoud();
                lastWasBlackout = false;
                yield return DadiRound(PickSpawn(), false, -1f, UrduText.DadiTooLoud);
                continue;
            }

            int night = game.Night;
            if (night >= Nights.Blackout && !lastWasBlackout && Random.value < blackoutChance)
            {
                lastWasBlackout = true;
                yield return BlackoutRound();
                continue;
            }
            lastWasBlackout = false;

            if (night >= Nights.Chotu && Random.value < chotuChance)
            {
                yield return ChotuRound();
                continue;
            }

            yield return DadiRound(PickSpawn(), true, -1f, null);
        }
    }

    // One visit from Dadi: footsteps, then she watches (unless it's a fake-out or the door is shut).
    IEnumerator DadiRound(SpawnPoint spawn, bool allowFakeOut, float warningOverride, string watchLine)
    {
        CurrentSpawn = spawn;
        bool fakeOut = allowFakeOut && Random.value < Mathf.Lerp(fakeChanceEasy, fakeChanceHard, game.Difficulty);
        blocked = false;

        // WARNING: footsteps get louder from her side of the room.
        Current = State.Warning;
        spawn.warningVisual.SetActive(true);
        float warning = warningOverride > 0f ? warningOverride
            : Mathf.Max(warningMinimum, Mathf.Lerp(warningEasy, warningHard, game.Difficulty));
        float stepGap = warning / stepsPerWarning;
        for (int i = 0; i < stepsPerWarning && !blocked; i++)
        {
            float volume = Mathf.Lerp(0.35f, 1f, i / (stepsPerWarning - 1f));
            audioManager.Footstep(spawn.pan, volume);
            yield return WaitUnlessBlocked(stepGap);
        }
        spawn.warningVisual.SetActive(false);

        if (blocked)
        {
            // Door was shut in her face: she knocks, grumbles and goes away.
            Current = State.Leaving;
            yield return new WaitForSeconds(0.25f);
            audioManager.PlayPanned(audioManager.knock, spawn.pan);
            ui.ShowBubble(spawn.BubblePoint(), UrduText.DadiBlocked);
            yield return new WaitForSeconds(1.4f);
            ui.HideBubble();
            yield break;
        }

        if (fakeOut)
        {
            // She turns away. Footsteps fade out.
            Current = State.Leaving;
            ui.ShowBubble(spawn.BubblePoint(), UrduText.Pick(UrduText.DadiFakeOut));
            for (int i = 0; i < 2; i++)
            {
                audioManager.Footstep(spawn.pan, 0.5f - i * 0.2f);
                yield return new WaitForSeconds(0.4f);
            }
            yield return new WaitForSeconds(0.4f);
            ui.HideBubble();
            yield break;
        }

        // WATCHING: touching now = caught (checked by GameManager).
        yield return Watch(spawn, Random.Range(watchTime.x, watchTime.y), watchLine ?? UrduText.Pick(UrduText.DadiWatching));

        // DOUBLE-CHECK: she leaves... then turns back. Only a cough warns you.
        if (game.Night >= Nights.DoubleCheck && Random.value < doubleCheckChance)
        {
            Current = State.Leaving;
            yield return new WaitForSeconds(Random.Range(0.5f, 0.9f));
            audioManager.PlayPanned(audioManager.cough, spawn.pan);
            yield return new WaitForSeconds(0.45f);
            yield return Watch(spawn, 0.9f, UrduText.DadiDoubleCheck);
        }
        Current = State.Leaving;
    }

    IEnumerator Watch(SpawnPoint spawn, float seconds, string line)
    {
        Current = State.Watching;
        spawn.dadi.SetActive(true);
        StartCoroutine(PopIn(spawn.dadi.transform));
        audioManager.Play(audioManager.sting, 0.8f);
        ui.SetWatching(true);
        ui.ShowBubble(spawn.BubblePoint(), line);
        game.OnDadiWatchStart();
        yield return new WaitForSeconds(seconds);
        spawn.dadi.SetActive(false);
        ui.SetWatching(false);
        ui.HideBubble();
    }

    // LOAD-SHEDDING: lights go out, she can't see. Eat as fast as you can!
    IEnumerator BlackoutRound()
    {
        Current = State.Blackout;
        audioManager.Play(audioManager.powerOff);
        audioManager.SetFanOn(false);
        ui.SetBlackout(true);
        ui.ShowPopup(UrduText.PowerCut, EnglishText.BlackoutSub);
        yield return new WaitForSeconds(3.0f);
        audioManager.Play(audioManager.upsBeep);
        ui.ShowHint(EnglishText.LightsBack);
        yield return new WaitForSeconds(1.0f);
        ui.HideHint();
        ui.SetBlackout(false);
        audioManager.SetFanOn(true);
        Current = State.Sleeping;
    }

    // CHOTU: he giggles and peeks in. If he sees you eating, he calls Dadi straight away.
    IEnumerator ChotuRound()
    {
        var doors = new List<SpawnPoint>();
        foreach (var s in spawns)
            if (s.chotu != null && s.unlockNight <= game.Night) doors.Add(s);
        SpawnPoint spawn = doors[Random.Range(0, doors.Count)];
        CurrentSpawn = spawn;

        Current = State.ChotuWatching;
        chotuLooking = false;
        spawn.chotu.SetActive(true);
        StartCoroutine(PopIn(spawn.chotu.transform));
        audioManager.PlayPanned(audioManager.giggle, spawn.pan);
        ui.ShowBubble(spawn.BubblePoint(spawn.chotu), UrduText.ChotuGiggle);
        yield return new WaitForSeconds(0.6f);   // time to let go

        chotuLooking = true;
        ui.SetWatching(true);
        bool snitched = false;
        float t = 0f;
        while (t < 1.6f && !snitched)
        {
            t += Time.deltaTime;
            if (game.IsEating) snitched = true;
            yield return null;
        }
        chotuLooking = false;
        ui.SetWatching(false);

        if (snitched)
        {
            ui.ShowBubble(spawn.BubblePoint(spawn.chotu), UrduText.ChotuSnitch);
            yield return new WaitForSeconds(0.5f);
            spawn.chotu.SetActive(false);
            ui.HideBubble();
            yield return DadiRound(spawn, false, warningMinimum, null);   // Dadi comes running
        }
        else
        {
            spawn.chotu.SetActive(false);
            ui.HideBubble();
            Current = State.Leaving;
        }
    }

    // Start screen: Dadi keeps peeking out of the kitchen door.
    public void StartMenuPeek()
    {
        Stop();
        StartCoroutine(MenuPeek());
    }

    IEnumerator MenuPeek()
    {
        SpawnPoint kitchen = spawns[0];
        while (true)
        {
            yield return new WaitForSeconds(2.5f);
            kitchen.dadi.SetActive(true);
            StartCoroutine(PopIn(kitchen.dadi.transform));
            yield return new WaitForSeconds(1.2f);
            kitchen.dadi.SetActive(false);
        }
    }

    IEnumerator WaitUnlessBlocked(float seconds)
    {
        float t = 0f;
        while (t < seconds && !blocked)
        {
            t += Time.deltaTime;
            yield return null;
        }
    }

    // Remembers each sprite's real size, so an interrupted pop-in never leaves it shrunk.
    readonly Dictionary<Transform, Vector3> baseScales = new Dictionary<Transform, Vector3>();

    IEnumerator PopIn(Transform t)
    {
        if (!baseScales.TryGetValue(t, out Vector3 full))
        {
            full = t.localScale;
            baseScales[t] = full;
        }
        float time = 0f;
        while (time < 0.15f)
        {
            time += Time.deltaTime;
            float s = Mathf.Lerp(0.8f, 1.05f, Mathf.Clamp01(time / 0.15f));
            t.localScale = full * s;
            yield return null;
        }
        t.localScale = full;
    }

    // Random unlocked spawn, never the same one twice in a row.
    SpawnPoint PickSpawn()
    {
        var options = new List<SpawnPoint>();
        foreach (var s in spawns)
        {
            if (s.unlockNight <= game.Night && s != lastSpawn) options.Add(s);
        }
        if (options.Count == 0) options.AddRange(spawns);
        lastSpawn = options[Random.Range(0, options.Count)];
        return lastSpawn;
    }
}
