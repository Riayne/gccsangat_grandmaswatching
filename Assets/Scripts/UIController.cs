using UnityEngine;
using UnityEngine.UIElements;

// Connects the UI Toolkit screen (Assets/UI/Game.uxml) to the game.
// Urdu text needs the "advanced" text generator, which Game.uss turns on.
// Runs before GameManager so the UI is ready when the game starts.
[DefaultExecutionOrder(-100)]
public class UIController : MonoBehaviour
{
    public UIDocument document;
    public GameManager game;
    public Camera cam;
    public Transform boxLabelAnchor;   // world point where "Crumble ki cookiyan" is written

    VisualElement root, bubble, titlePanel, gameOverPanel;
    Label scoreLabel, bestLabel, boxLabel, bubbleText, praise, newBestLabel, finalScoreLabel, finalBestLabel;
    Vector3 bubbleWorldPoint;

    void Start()
    {
        root = document.rootVisualElement;
        root.languageDirection = LanguageDirection.RTL;   // Urdu reads right to left
        scoreLabel = root.Q<Label>("scoreLabel");
        bestLabel = root.Q<Label>("bestLabel");
        boxLabel = root.Q<Label>("boxLabel");
        bubble = root.Q("bubble");
        bubbleText = root.Q<Label>("bubbleText");
        praise = root.Q<Label>("praise");
        titlePanel = root.Q("titlePanel");
        gameOverPanel = root.Q("gameOverPanel");
        finalScoreLabel = root.Q<Label>("finalScoreLabel");
        finalBestLabel = root.Q<Label>("finalBestLabel");
        newBestLabel = root.Q<Label>("newBestLabel");

        // Fill in all the Urdu text.
        boxLabel.text = UrduText.BoxLabel;
        root.Q<Label>("titleLabel").text = UrduText.Title;
        root.Q<Label>("taglineLabel").text = UrduText.Tagline;
        root.Q<Label>("caughtLabel").text = UrduText.Caught;
        newBestLabel.text = UrduText.NewBest;
        var howTo = root.Q("howTo");
        foreach (string line in UrduText.HowTo)
        {
            var label = new Label(line);
            label.AddToClassList("how-to-line");
            howTo.Add(label);
        }

        var startButton = root.Q<Button>("startButton");
        startButton.text = UrduText.Start;
        startButton.clicked += game.StartGame;
        var retryButton = root.Q<Button>("retryButton");
        retryButton.text = UrduText.Retry;
        retryButton.clicked += game.Retry;

        root.RegisterCallback<GeometryChangedEvent>(_ => PlaceWorldLabels());
    }

    void LateUpdate()
    {
        PlaceWorldLabels();
    }

    // Keeps labels that belong to the room (box label, Dadi's bubble) on top of the right spot.
    void PlaceWorldLabels()
    {
        if (root.panel == null) return;
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

    public void SetScore(int score, int best)
    {
        scoreLabel.text = UrduText.Cookies + UrduText.Digits(score);
        bestLabel.text = UrduText.Best + UrduText.Digits(best);
    }

    public void PopScore()
    {
        scoreLabel.AddToClassList("score--pop");
        scoreLabel.schedule.Execute(() => scoreLabel.RemoveFromClassList("score--pop")).StartingIn(90);
    }

    public void ShowPraise(string text)
    {
        praise.text = text;
        praise.AddToClassList("praise--show");
        praise.schedule.Execute(() => praise.RemoveFromClassList("praise--show")).StartingIn(900);
    }

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

    public void ShowTitle() => titlePanel.RemoveFromClassList("hidden");
    public void HideTitle() => titlePanel.AddToClassList("hidden");

    public void ShowGameOver(int score, int best, bool newBest)
    {
        finalScoreLabel.text = UrduText.Cookies + UrduText.Digits(score);
        finalBestLabel.text = UrduText.Best + UrduText.Digits(best);
        newBestLabel.EnableInClassList("hidden", !newBest);
        gameOverPanel.RemoveFromClassList("hidden");
    }
}
