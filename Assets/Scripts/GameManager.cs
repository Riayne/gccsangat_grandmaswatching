using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Runs the game: start screen, nights, touch input, cookies, combo/noise, golden cookies,
// near misses, and deciding when the bacha is caught.
public class GameManager : MonoBehaviour
{
    public enum State { Menu, NightCard, Playing, GameOver }

    public Camera cam;
    public Bacha bacha;
    public CookieBox cookieBox;
    public DadiController dadi;
    public AudioManager audioManager;
    public UIController ui;
    public GameOverSequence gameOver;

    [Header("Eating")]
    public float cookiesPerSecond = 2.5f;
    public float comboX2After = 1.5f;      // seconds of holding for x2
    public float comboX3After = 3.5f;      // seconds of holding for x3
    [Header("Noise (from Nights.Combo)")]
    public float[] noisePerSecond = { 0f, 0.10f, 0.22f, 0.38f };   // by multiplier x1, x2, x3
    public float noiseFadePerSecond = 0.35f;
    [Header("Window")]
    public float windowSafeTime = 3.5f;
    public float windowCooldown = 8f;
    [Header("Bonuses")]
    public float nearMissWindow = 0.3f;    // let go this close to her look = near miss
    public int nearMissBonus = 3;
    public int goldenBonus = 5;
    public Vector2 goldenEvery = new Vector2(6f, 11f);
    public float goldenShowTime = 1.6f;

    public State Current { get; private set; } = State.Menu;
    public int Score { get; private set; }
    public int Best { get; private set; }
    public int BestNight { get; private set; }
    public int Night { get; private set; } = 1;
    public float Noise { get; private set; }
    public bool IsEating { get; private set; }
    public int Multiplier { get; private set; } = 1;
    public float Difficulty { get; private set; }   // 0 = easy, 1 = hardest

    const string BestKey = "BestScore";
    const string BestNightKey = "BestNight";
    const string TutorialKey = "TutorialDone";
    static bool skipTitle;   // true after "PLAY AGAIN" so the start screen is not shown again

    // Static values can survive between Play sessions in the Editor, so reset them on launch.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => skipTitle = false;

    float cookieProgress;
    bool pressConsumed;      // the current touch was used for a door/window/button, not for eating
    float windowReadyTime;
    float holdTime;
    float lastEatEnd = -10f;
    float nextGoldenTime;
    bool tutorialDone;
    bool tutorialSlowMo;
    bool holdHintShown;

    void Start()
    {
        Time.timeScale = 1f;
        Best = PlayerPrefs.GetInt(BestKey, 0);
        BestNight = PlayerPrefs.GetInt(BestNightKey, 0);
        tutorialDone = PlayerPrefs.GetInt(TutorialKey, 0) == 1;
        ui.SetHud(0, Best, 1);
        if (skipTitle) StartGame();
        else ShowMenu();
    }

    void ShowMenu()
    {
        Current = State.Menu;
        bacha.SetState(Bacha.State.Hiding);
        ui.ShowMenu(Best, BestNight);
        dadi.StartMenuPeek();
    }

    public void StartGame()
    {
        ui.HideMenu();
        Score = 0;
        Night = 1;
        Noise = 0f;
        cookieProgress = 0f;
        ui.SetHud(Score, Best, Night);
        StartCoroutine(NightIntro());
    }

    public void Retry()
    {
        skipTitle = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BackToMenu()
    {
        skipTitle = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Card between nights: "NIGHT 3 - <new twist>". Dadi goes back to sleep meanwhile.
    IEnumerator NightIntro()
    {
        Current = State.NightCard;
        Time.timeScale = 1f;
        tutorialSlowMo = false;
        dadi.Stop();
        cookieBox.HideGolden();
        cookieBox.IsEating = false;
        IsEating = false;
        bacha.SetState(Bacha.State.Hiding);
        Noise = 0f;
        ui.SetCombo(1, false);
        ui.SetNoise(0f, Night >= Nights.Combo);
        ui.SetHud(Score, Best, Night);
        ui.ShowNightCard(Night, Nights.Card(Night));
        audioManager.Play(audioManager.nightStart, 0.7f);
        yield return new WaitForSecondsRealtime(2.6f);
        ui.HideNightCard();

        Current = State.Playing;
        pressConsumed = true;   // ignore a finger that is already down
        holdTime = 0f;
        nextGoldenTime = Time.time + Random.Range(goldenEvery.x, goldenEvery.y) * 0.5f;
        dadi.Begin();
        if (Night == 1 && !tutorialDone)
        {
            ui.ShowHint(EnglishText.HintHold);
            holdHintShown = true;
        }
    }

    void Update()
    {
        if (Current != State.Playing) return;

        float prev = Nights.PreviousTarget(Night);
        float progress = Mathf.Clamp01((Score - prev) / (Nights.Target(Night) - prev));
        Difficulty = Mathf.Clamp01((Night - 1 + progress) / 10f);

        var pointer = Pointer.current;
        bool pressed = pointer != null && pointer.press.isPressed;
        bool pressedThisFrame = pointer != null && pointer.press.wasPressedThisFrame;

        // Out of the window: safe from Dadi, but no eating.
        if (bacha.Current == Bacha.State.Away)
        {
            SetEating(false);
            if (!pressed) pressConsumed = false;
            FadeNoise();
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

        SetEating(pressed && !pressConsumed);
        UpdateComboAndNoise();
        UpdateTutorial();

        if (IsEating)
        {
            cookieProgress += cookiesPerSecond * Multiplier * Time.deltaTime;
            while (cookieProgress >= 1f)
            {
                cookieProgress -= 1f;
                AddCookies(1);
            }
        }

        UpdateGoldenCookie();

        if (Score >= Nights.Target(Night) && Current == State.Playing)
        {
            Night++;
            if (Night > BestNight)
            {
                BestNight = Night;
                PlayerPrefs.SetInt(BestNightKey, BestNight);
            }
            StartCoroutine(NightIntro());
        }
    }

    void SetEating(bool eating)
    {
        if (IsEating && !eating) lastEatEnd = Time.time;
        IsEating = eating;
        bacha.SetState(eating ? Bacha.State.Eating : (bacha.Current == Bacha.State.Away ? Bacha.State.Away : Bacha.State.Hiding));
        cookieBox.IsEating = eating;
        if (eating && holdHintShown)
        {
            ui.HideHint();
            holdHintShown = false;
        }
    }

    // Holding longer eats faster (x2, x3), but makes noise that wakes Dadi sooner.
    void UpdateComboAndNoise()
    {
        holdTime = IsEating ? holdTime + Time.deltaTime : 0f;
        bool comboOn = Night >= Nights.Combo;

        if (dadi.IsBlackout) Multiplier = 3;   // load-shedding frenzy
        else if (!comboOn) Multiplier = 1;
        else Multiplier = holdTime > comboX3After ? 3 : holdTime > comboX2After ? 2 : 1;

        if (comboOn && IsEating && !dadi.IsBlackout)
            Noise = Mathf.Clamp01(Noise + noisePerSecond[Multiplier] * Time.deltaTime);
        else
            FadeNoise();

        ui.SetCombo(Multiplier, IsEating && Multiplier > 1);
        ui.SetNoise(Noise, comboOn);
    }

    void FadeNoise()
    {
        Noise = Mathf.Max(0f, Noise - noiseFadePerSecond * Time.deltaTime);
    }

    // Night 1 tutorial: the first time footsteps start while eating, slow time and say "LET GO NOW!".
    void UpdateTutorial()
    {
        if (tutorialDone || Night != 1) return;
        if (!tutorialSlowMo && dadi.IsWarning && IsEating)
        {
            tutorialSlowMo = true;
            Time.timeScale = 0.35f;
            ui.ShowHint(EnglishText.HintLetGo);
        }
        else if (tutorialSlowMo && !IsEating)
        {
            tutorialSlowMo = false;
            tutorialDone = true;
            PlayerPrefs.SetInt(TutorialKey, 1);
            Time.timeScale = 1f;
            ui.ShowHint(EnglishText.HintNice, 1.8f);
        }
    }

    void UpdateGoldenCookie()
    {
        if (Night < Nights.GoldenCookie) return;
        if (!cookieBox.GoldenActive && Time.time >= nextGoldenTime)
        {
            cookieBox.ShowGolden(goldenShowTime);
            nextGoldenTime = Time.time + Random.Range(goldenEvery.x, goldenEvery.y);
        }
        if (IsEating && cookieBox.GoldenActive)
        {
            cookieBox.HideGolden();
            audioManager.Play(audioManager.golden);
            Haptics.Pulse(40);
            ui.ShowPopup(UrduText.Golden, EnglishText.GoldenSub);
            AddCookies(goldenBonus);
        }
    }

    void AddCookies(int amount)
    {
        int before = Score;
        Score += amount;
        audioManager.Crunch(1f + (Multiplier - 1) * 0.12f);
        cookieBox.Bounce();
        ui.SetHud(Score, Best, Night);
        ui.PopScore();
        if (Score / 10 > before / 10 && amount == 1)
        {
            ui.ShowPopup(UrduText.Pick(UrduText.Praise), null);
            audioManager.Play(audioManager.praise, 0.7f);
        }
    }

    // Called by Dadi the moment she looks. Letting go at the last moment is a NEAR MISS.
    public void OnDadiWatchStart()
    {
        if (!IsEating && Time.time - lastEatEnd < nearMissWindow)
        {
            lastEatEnd = -10f;
            AddCookies(nearMissBonus);
            ui.ShowPopup(UrduText.NearMiss, EnglishText.NearMissSub);
            audioManager.Play(audioManager.whoosh, 0.7f);
            Haptics.Pulse(60);
            StartCoroutine(SlowMotion(0.4f, 0.6f));
        }
    }

    // Called by Dadi when the noise meter fills up and wakes her.
    public void OnTooLoud()
    {
        ui.ShowHint(EnglishText.TooLoud, 1.2f);
        Noise = 0.3f;
    }

    IEnumerator SlowMotion(float scale, float realSeconds)
    {
        Time.timeScale = scale;
        yield return new WaitForSecondsRealtime(realSeconds);
        if (Current == State.Playing && !tutorialSlowMo) Time.timeScale = 1f;
    }

    IEnumerator JumpOutOfWindow()
    {
        windowReadyTime = Time.time + windowCooldown;
        audioManager.Play(audioManager.jump);
        IsEating = false;
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
        Time.timeScale = 1f;
        dadi.Freeze();
        cookieBox.IsEating = false;
        cookieBox.HideGolden();
        ui.HideHint();
        ui.SetCombo(1, false);

        bool newBest = Score > Best;
        if (newBest)
        {
            Best = Score;
            PlayerPrefs.SetInt(BestKey, Best);
        }
        PlayerPrefs.Save();
        ui.SetHud(Score, Best, Night);

        // Make sure Dadi is visible when she catches him.
        SpawnPoint spawn = dadi.CurrentSpawn;
        spawn.dadi.SetActive(true);
        ui.SetWatching(true);
        ui.ShowBubble(spawn.BubblePoint(), UrduText.DadiCaught);
        StartCoroutine(GameOverRoutine(spawn, newBest));
    }

    IEnumerator GameOverRoutine(SpawnPoint spawn, bool newBest)
    {
        yield return gameOver.Play(spawn.dadi.transform.position);
        ui.HideBubble();
        ui.SetWatching(false);
        ui.ShowGameOver(Score, Best, Night, newBest, UrduText.RankFor(Score));
    }
}
