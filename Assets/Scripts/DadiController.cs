using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Dadi's behaviour, as a simple coroutine state machine:
// Sleeping -> Warning (footsteps) -> Watching (or a fake-out, or blocked by a closed door) -> Sleeping ...
public class DadiController : MonoBehaviour
{
    public enum State { Idle, Sleeping, Warning, Watching, Leaving }

    public SpawnPoint[] spawns;
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
    [Header("Fake-outs")]
    public float fakeChanceEasy = 0.10f;
    public float fakeChanceHard = 0.35f;
    public int stepsPerWarning = 4;

    public State Current { get; private set; } = State.Idle;
    public SpawnPoint CurrentSpawn { get; private set; }
    public bool IsWatching => Current == State.Watching;
    public bool IsWarning => Current == State.Warning;

    // Set by GameManager every frame (0 = easy, 1 = hardest).
    public float Difficulty { get; set; }

    SpawnPoint lastSpawn;
    bool blocked;
    Coroutine loop;

    public void Begin()
    {
        foreach (var s in spawns)
        {
            s.dadi.SetActive(false);
            s.warningVisual.SetActive(false);
            s.SetDoorClosed(false);
        }
        loop = StartCoroutine(Loop());
    }

    // Stops Dadi where she is (used when the bacha is caught).
    public void Freeze()
    {
        if (loop != null) StopCoroutine(loop);
        loop = null;
        if (CurrentSpawn != null) CurrentSpawn.warningVisual.SetActive(false);
    }

    // Returns the door/window the player tapped, or null.
    public SpawnPoint TappedSpawn(Vector2 worldPoint)
    {
        foreach (var s in spawns)
        {
            if (s.kind != SpawnPoint.Kind.Sofa && s.Contains(worldPoint)) return s;
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
        while (true)
        {
            // 1. SLEEPING: nothing but ambient sound.
            Current = State.Sleeping;
            foreach (var s in spawns) s.SetDoorClosed(false);
            float sleep = first ? firstSleep : Random.Range(
                Mathf.Lerp(sleepEasy.x, sleepHard.x, Difficulty),
                Mathf.Lerp(sleepEasy.y, sleepHard.y, Difficulty));
            first = false;
            yield return new WaitForSeconds(sleep);

            // 2. WARNING: footsteps get louder from her side of the room.
            CurrentSpawn = PickSpawn();
            bool fakeOut = Random.value < Mathf.Lerp(fakeChanceEasy, fakeChanceHard, Difficulty);
            blocked = false;
            Current = State.Warning;
            CurrentSpawn.warningVisual.SetActive(true);
            float warning = Mathf.Max(warningMinimum, Mathf.Lerp(warningEasy, warningHard, Difficulty));
            float stepGap = warning / stepsPerWarning;
            for (int i = 0; i < stepsPerWarning && !blocked; i++)
            {
                float volume = Mathf.Lerp(0.35f, 1f, i / (stepsPerWarning - 1f));
                audioManager.Footstep(CurrentSpawn.pan, volume);
                yield return WaitUnlessBlocked(stepGap);
            }
            CurrentSpawn.warningVisual.SetActive(false);

            if (blocked)
            {
                // Door was shut in her face: she knocks, grumbles and goes away.
                Current = State.Leaving;
                yield return new WaitForSeconds(0.25f);
                audioManager.PlayPanned(audioManager.knock, CurrentSpawn.pan);
                ui.ShowBubble(CurrentSpawn.BubblePoint(), UrduText.DadiBlocked);
                yield return new WaitForSeconds(1.4f);
                ui.HideBubble();
                continue;
            }

            if (fakeOut)
            {
                // She turns away. Footsteps fade out.
                Current = State.Leaving;
                ui.ShowBubble(CurrentSpawn.BubblePoint(), UrduText.Pick(UrduText.DadiFakeOut));
                for (int i = 0; i < 2; i++)
                {
                    audioManager.Footstep(CurrentSpawn.pan, 0.5f - i * 0.2f);
                    yield return new WaitForSeconds(0.4f);
                }
                yield return new WaitForSeconds(0.4f);
                ui.HideBubble();
                continue;
            }

            // 3. WATCHING: she steps out. Touching now = caught (checked by GameManager).
            Current = State.Watching;
            CurrentSpawn.dadi.SetActive(true);
            StartCoroutine(PopIn(CurrentSpawn.dadi.transform));
            audioManager.Play(audioManager.sting, 0.8f);
            ui.ShowBubble(CurrentSpawn.BubblePoint(), UrduText.Pick(UrduText.DadiWatching));
            yield return new WaitForSeconds(Random.Range(watchTime.x, watchTime.y));

            Current = State.Leaving;
            CurrentSpawn.dadi.SetActive(false);
            ui.HideBubble();
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

    IEnumerator PopIn(Transform t)
    {
        Vector3 full = t.localScale;
        float time = 0f;
        while (time < 0.15f)
        {
            time += Time.deltaTime;
            float k = Mathf.Clamp01(time / 0.15f);
            float s = Mathf.Lerp(0.8f, 1.05f, k);
            t.localScale = new Vector3(full.x * s, full.y * s, 1f);
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
            if (Difficulty >= s.unlockAt && s != lastSpawn) options.Add(s);
        }
        if (options.Count == 0) options.AddRange(spawns);
        lastSpawn = options[Random.Range(0, options.Count)];
        return lastSpawn;
    }
}
