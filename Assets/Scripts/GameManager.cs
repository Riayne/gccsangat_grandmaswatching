using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Runs the game: reads touch input, counts cookies, and decides when the bacha is caught.
public class GameManager : MonoBehaviour
{
    public enum State { Title, Playing, GameOver }

    public Camera cam;
    public Bacha bacha;
    public CookieBox cookieBox;
    public DadiController dadi;
    public AudioManager audioManager;
    public UIController ui;
    public GameOverSequence gameOver;

    [Header("Tuning")]
    public float cookiesPerSecond = 2.5f;
    public float cookiesForMaxDifficulty = 100f;
    public float windowSafeTime = 3.5f;
    public float windowCooldown = 8f;

    public State Current { get; private set; } = State.Title;
    public int Score { get; private set; }
    public int Best { get; private set; }

    const string BestKey = "BestScore";
    static bool skipTitle;   // true after "Dobara Koshish" so the title is not shown again

    // Static values can survive between Play sessions in the Editor, so reset them on launch.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => skipTitle = false;

    float cookieProgress;
    bool pressConsumed;      // the current touch was used for a door/window/button, not for eating
    float windowReadyTime;

    void Start()
    {
        Best = PlayerPrefs.GetInt(BestKey, 0);
        ui.SetScore(0, Best);
        if (skipTitle) StartGame();
        else ui.ShowTitle();
    }

    public void StartGame()
    {
        Current = State.Playing;
        Score = 0;
        cookieProgress = 0f;
        pressConsumed = true;   // ignore the touch that pressed the button
        ui.HideTitle();
        ui.SetScore(Score, Best);
        dadi.Begin();
    }

    public void Retry()
    {
        skipTitle = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void Update()
    {
        if (Current != State.Playing) return;

        dadi.Difficulty = Mathf.Clamp01(Score / cookiesForMaxDifficulty);

        var pointer = Pointer.current;
        bool pressed = pointer != null && pointer.press.isPressed;
        bool pressedThisFrame = pointer != null && pointer.press.wasPressedThisFrame;

        // Out of the window: safe from Dadi, but no eating.
        if (bacha.Current == Bacha.State.Away)
        {
            cookieBox.IsEating = false;
            if (!pressed) pressConsumed = false;
            return;
        }

        // Touching anything while Dadi is watching = caught.
        if (pressed && dadi.IsWatching)
        {
            Caught();
            return;
        }

        if (pressedThisFrame)
        {
            pressConsumed = false;
            if (dadi.IsWarning)
            {
                Vector2 world = cam.ScreenToWorldPoint(pointer.position.ReadValue());
                SpawnPoint tapped = dadi.TappedSpawn(world);
                if (tapped != null)
                {
                    pressConsumed = true;
                    if (tapped.kind == SpawnPoint.Kind.Door) dadi.CloseDoor(tapped);
                    else if (tapped.kind == SpawnPoint.Kind.Window && Time.time >= windowReadyTime) StartCoroutine(JumpOutOfWindow());
                }
            }
        }
        if (!pressed) pressConsumed = false;

        bool eating = pressed && !pressConsumed;
        bacha.SetState(eating ? Bacha.State.Eating : Bacha.State.Hiding);
        cookieBox.IsEating = eating;

        if (eating)
        {
            cookieProgress += cookiesPerSecond * Time.deltaTime;
            while (cookieProgress >= 1f)
            {
                cookieProgress -= 1f;
                AddCookie();
            }
        }
    }

    void AddCookie()
    {
        Score++;
        audioManager.Crunch();
        cookieBox.Bounce();
        ui.SetScore(Score, Best);
        ui.PopScore();
        if (Score % 10 == 0)
        {
            ui.ShowPraise(UrduText.Pick(UrduText.Praise));
            audioManager.Play(audioManager.praise, 0.7f);
        }
    }

    IEnumerator JumpOutOfWindow()
    {
        windowReadyTime = Time.time + windowCooldown;
        audioManager.Play(audioManager.jump);
        bacha.SetState(Bacha.State.Away);
        cookieBox.IsEating = false;
        yield return new WaitForSeconds(windowSafeTime);
        if (Current == State.Playing) bacha.SetState(Bacha.State.Hiding);
    }

    public void Caught()
    {
        if (Current != State.Playing) return;
        Current = State.GameOver;
        StopAllCoroutines();
        dadi.Freeze();
        cookieBox.IsEating = false;

        bool newBest = Score > Best;
        if (newBest)
        {
            Best = Score;
            PlayerPrefs.SetInt(BestKey, Best);
            PlayerPrefs.Save();
        }
        ui.SetScore(Score, Best);

        // Make sure Dadi is visible when she catches him.
        if (dadi.CurrentSpawn != null) dadi.CurrentSpawn.dadi.SetActive(true);
        ui.ShowBubble(dadi.CurrentSpawn.BubblePoint(), UrduText.DadiCaught);
        StartCoroutine(GameOverRoutine(newBest));
    }

    IEnumerator GameOverRoutine(bool newBest)
    {
        yield return gameOver.Play(dadi.CurrentSpawn.dadi.transform.position);
        ui.HideBubble();
        ui.ShowGameOver(Score, Best, newBest);
    }
}
