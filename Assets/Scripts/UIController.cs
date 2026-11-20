using UnityEngine;
using UnityEngine.UIElements;

// Connects the UI Toolkit screen (Assets/UI/Game.uxml) to the game.
// Urdu text needs the "advanced" text generator and right-to-left direction (set in Game.uss and below);
// English text uses the "en" class (Lilita One font, left-to-right).
// Runs before GameManager so the UI is ready when the game starts.
[DefaultExecutionOrder(-100)]
public class UIController : MonoBehaviour
{
    public UIDocument document;
    public GameManager game;
    public AudioManager audioManager;
    public Camera cam;
    public Transform boxLabelAnchor;   // world point where "Crumble ki cookiyan" is written
    public Sprite[] howToIcons;        // one per row of EnglishText.HowTo, same order

    VisualElement root, bubble, popup, menuPanel, howToPanel, nightCard, gameOverPanel;
    VisualElement blackout, vignette, noise, noiseFill;
    Label scoreLabel, bestLabel, nightLabel, boxLabel, bubbleText, popupText, popupSub, hint, comboLabel;
    Label nightTitle, nightText, bestLine, rankLabel, newBestLabel, finalScoreLabel, finalBestLabel, finalNightLabel;
    Button soundButton;
    Vector3 bubbleWorldPoint;
    IVisualElementScheduledItem popupHide, hintHide;

    void Start()
    {
        root = document.rootVisualElement;
        root.languageDirection = LanguageDirection.RTL;   // Urdu reads right to left

        scoreLabel = root.Q<Label>("scoreLabel");
        bestLabel = root.Q<Label>("bestLabel");
        nightLabel = root.Q<Label>("nightLabel");
        boxLabel = root.Q<Label>("boxLabel");
        bubble = root.Q("bubble");
        bubbleText = root.Q<Label>("bubbleText");
        popup = root.Q("popup");
        popupText = root.Q<Label>("popupText");
        popupSub = root.Q<Label>("popupSub");
        hint = root.Q<Label>("hint");
        comboLabel = root.Q<Label>("comboLabel");
        noise = root.Q("noise");
        noiseFill = root.Q("noiseFill");
        blackout = root.Q("blackout");
        vignette = root.Q("vignette");
        menuPanel = root.Q("menuPanel");
        howToPanel = root.Q("howToPanel");
        nightCard = root.Q("nightCard");
        gameOverPanel = root.Q("gameOverPanel");
        nightTitle = root.Q<Label>("nightTitle");
        nightText = root.Q<Label>("nightText");
        bestLine = root.Q<Label>("bestLine");
        rankLabel = root.Q<Label>("rankLabel");
        finalScoreLabel = root.Q<Label>("finalScoreLabel");
        finalBestLabel = root.Q<Label>("finalBestLabel");
        finalNightLabel = root.Q<Label>("finalNightLabel");
        newBestLabel = root.Q<Label>("newBestLabel");

        // Fixed text.
        boxLabel.text = UrduText.BoxLabel;
        root.Q<Label>("titleLabel").text = UrduText.Title;
        root.Q<Label>("taglineLabel").text = UrduText.Tagline;
        root.Q<Label>("caughtLabel").text = UrduText.Caught;
        root.Q<Label>("howToHeader").text = EnglishText.HowToPlay;
        root.Q<Label>("noiseLabel").text = EnglishText.Noise;
        newBestLabel.text = EnglishText.NewBest;
        BuildHowToRows();

        // Buttons.
        SetupButton("playButton", EnglishText.Play, game.StartGame);
        SetupButton("howToButton", EnglishText.HowToPlay, () => howToPanel.RemoveFromClassList("hidden"));
        SetupButton("gotItButton", EnglishText.GotIt, () => howToPanel.AddToClassList("hidden"));
        SetupButton("playAgainButton", EnglishText.PlayAgain, game.Retry);
        SetupButton("menuButton", EnglishText.Menu, game.BackToMenu);
        soundButton = SetupButton("soundButton", "", () => { audioManager.ToggleSound(); UpdateSoundButton(); });
        UpdateSoundButton();

        // English text reads left to right.
        root.Query(className: "en").ForEach(e => e.languageDirection = LanguageDirection.LTR);

        root.RegisterCallback<GeometryChangedEvent>(_ => PlaceWorldLabels());
    }

    Button SetupButton(string name, string text, System.Action onClick)
    {
        var button = root.Q<Button>(name);
        button.text = text;
        button.clicked += onClick;
        return button;
    }

    void UpdateSoundButton()
    {
        soundButton.text = audioManager.SoundOn ? EnglishText.SoundOn : EnglishText.SoundOff;
    }

    void BuildHowToRows()
    {
        var rows = root.Q("howToRows");
        for (int i = 0; i < EnglishText.HowTo.GetLength(0); i++)
        {
            var row = new VisualElement();
            row.AddToClassList("howto-row");
            var icon = new VisualElement();
            icon.AddToClassList("howto-icon");
            if (i < howToIcons.Length && howToIcons[i] != null)
                icon.style.backgroundImage = new StyleBackground(howToIcons[i]);
            var text = new Label(EnglishText.HowTo[i, 1]);
            text.AddToClassList("howto-text");
            text.AddToClassList("en");
            row.Add(icon);
            row.Add(text);
            rows.Add(row);
        }
    }

    void LateUpdate()
    {
        PlaceWorldLabels();
    }

    // Keeps labels that belong to the room (box label, speech bubble) on top of the right spot.
    void PlaceWorldLabels()
    {
        if (root == null || root.panel == null) return;
        PlaceAt(boxLabel, boxLabelAnchor.position, 0f);
        if (!bubble.ClassListContains("hidden"))
            PlaceAt(bubble, bubbleWorldPoint, 260f);
    }

    void PlaceAt(VisualElement element, Vector3 worldPoint, float edgeMargin)
    {
        Vector2 p = RuntimePanelUtils.CameraTransformWorldToPanel(root.panel, worldPoint, cam);
        float width = root.layout.width;
        if (edgeMargin > 0f && width > edgeMargin * 2f)
            p.x = Mathf.Clamp(p.x, edgeMargin, width - edgeMargin);
        element.style.left = p.x;
        element.style.top = p.y;
    }

    // ---------- HUD ----------
    public void SetHud(int score, int best, int night)
    {
        scoreLabel.text = EnglishText.Cookies + " " + score;
        bestLabel.text = EnglishText.Best + " " + best;
        nightLabel.text = EnglishText.Night + " " + night;
    }

    public void PopScore()
    {
        scoreLabel.AddToClassList("score--pop");
        scoreLabel.schedule.Execute(() => scoreLabel.RemoveFromClassList("score--pop")).StartingIn(90);
    }

    public void SetCombo(int multiplier, bool visible)
    {
        comboLabel.text = "x" + multiplier;
        comboLabel.EnableInClassList("combo--on", visible);
    }

    public void SetNoise(float value, bool visible)
    {
        noise.EnableInClassList("hidden", !visible);
        noiseFill.style.width = Length.Percent(value * 100f);
        noiseFill.EnableInClassList("noise-fill--loud", value > 0.7f);
    }

    // ---------- Popups, hints, effects ----------
    public void ShowPopup(string urdu, string englishSub)
    {
        popupText.text = urdu;
        popupSub.text = englishSub ?? "";
        popupSub.EnableInClassList("hidden", string.IsNullOrEmpty(englishSub));
        popup.AddToClassList("popup--show");
        popupHide?.Pause();
        popupHide = popup.schedule.Execute(() => popup.RemoveFromClassList("popup--show")).StartingIn(1000);
    }

    public void ShowHint(string text, float seconds = 0f)
    {
        hint.text = text;
        hint.RemoveFromClassList("hidden");
        hintHide?.Pause();
        if (seconds > 0f)
            hintHide = hint.schedule.Execute(HideHint).StartingIn((long)(seconds * 1000));
    }

    public void HideHint()
    {
        hintHide?.Pause();
        hint.AddToClassList("hidden");
    }

    public void SetWatching(bool on) => vignette.EnableInClassList("vignette--on", on);
    public void SetBlackout(bool on) => blackout.EnableInClassList("blackout--on", on);

    public void ShowBubble(Vector3 worldPoint, string text)
    {
        bubbleWorldPoint = worldPoint;
        bubbleText.text = text;
        bubble.RemoveFromClassList("hidden");
        PlaceWorldLabels();
    }

    public void HideBubble()
    {
        bubble.AddToClassList("hidden");
    }

    // ---------- Screens ----------
    public void ShowMenu(int best, int bestNight)
    {
        bestLine.text = best > 0 ? EnglishText.Best + " " + best + "   |   " + EnglishText.Night + " " + Mathf.Max(1, bestNight) : "";
        menuPanel.RemoveFromClassList("hidden");
        SetHudVisible(false);
    }

    public void SetHudVisible(bool visible)
    {
        root.Q("hud").EnableInClassList("hidden", !visible);
        root.Q("meters").EnableInClassList("hidden", !visible);
    }

    public void HideMenu()
    {
        menuPanel.AddToClassList("hidden");
        howToPanel.AddToClassList("hidden");
        SetHudVisible(true);
    }

    public void ShowNightCard(int night, string text)
    {
        nightTitle.text = EnglishText.Night + " " + night;
        nightText.text = text;
        nightCard.RemoveFromClassList("hidden");
    }

    public void HideNightCard() => nightCard.AddToClassList("hidden");

    public void ShowGameOver(int score, int best, int night, bool newBest, string rank)
    {
        rankLabel.text = rank;
        finalScoreLabel.text = EnglishText.Cookies + " " + score;
        finalNightLabel.text = EnglishText.Night + " " + night;
        finalBestLabel.text = EnglishText.Best + " " + best;
        newBestLabel.EnableInClassList("hidden", !newBest);
        gameOverPanel.RemoveFromClassList("hidden");
    }
}
