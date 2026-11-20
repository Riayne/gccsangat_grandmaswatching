// One-time scene builder, run from the CLI:
//   unity command script run --file AgentScripts/BuildScene.cs --entry BuildScene.All
// It imports the generated art/audio, then creates Assets/Scenes/Game.unity from AgentScripts/art/layout_unity.txt.
// WARNING: re-running it overwrites Game.unity, so hand edits to the scene would be lost.
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

public static class BuildScene
{
    [Serializable] class SpriteInfo { public string name; public float x, y, w, h; public int order; }
    [Serializable] class SpawnInfo { public string id, kind, sprite; public float unlockAt, pan, scale, feetX, feetY, tapX, tapY, tapW, tapH; public int order; }
    [Serializable] class Layout { public SpriteInfo[] sprites; public SpawnInfo[] spawns; }

    const string ScenePath = "Assets/Scenes/Game.unity";
    static Layout layout;
    static Material unlit;

    public static string All()
    {
        string log = ImportAssets();
        log += Scene();
        log += Settings();
        return log;
    }

    public static string ImportAssets()
    {
        AssetDatabase.Refresh();
        foreach (string path in Directory.GetFiles("Assets/Art", "*.png"))
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
            string name = Path.GetFileNameWithoutExtension(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.maxTextureSize = name == "bg_lounge" ? 4096 : 2048;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteAlignment = name.StartsWith("dadi") || name == "chotu" ? (int)SpriteAlignment.BottomCenter : (int)SpriteAlignment.Center;
            s.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);
            ti.SaveAndReimport();
        }

        // Font: keep only Nastaliq, with a path without spaces (Game.uss points at it).
        string oldFa = "Assets/Fonts/NotoNastaliqUrdu SDF.asset";
        string fa = "Assets/Fonts/NotoNastaliqUrdu_SDF.asset";
        if (AssetDatabase.LoadAssetAtPath<FontAsset>(oldFa) != null) AssetDatabase.MoveAsset(oldFa, fa);
        AssetDatabase.DeleteAsset("Assets/Fonts/NotoNaskhArabic SDF.asset");
        AssetDatabase.DeleteAsset("Assets/Fonts/NotoNaskhArabic.ttf");
        // English font asset (Lilita One) for buttons, instructions and the HUD.
        string enFa = "Assets/Fonts/LilitaOne_SDF.asset";
        if (AssetDatabase.LoadAssetAtPath<FontAsset>(enFa) == null)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/LilitaOne-Regular.ttf");
            var asset = FontAsset.CreateFontAsset(font);
            AssetDatabase.CreateAsset(asset, enFa);
            if (asset.material) AssetDatabase.AddObjectToAsset(asset.material, asset);
            foreach (var t in asset.atlasTextures) if (t) AssetDatabase.AddObjectToAsset(t, asset);
        }

        // Panel settings: 1080x1920 reference, match width.
        var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/GamePanelSettings.asset");
        ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        ps.referenceResolution = new Vector2Int(1080, 1920);
        ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        ps.match = 0f;
        EditorUtility.SetDirty(ps);
        AssetDatabase.SaveAssets();
        return "assets imported; ";
    }

    public static string Scene()
    {
        layout = LoadLayout("AgentScripts/art/layout_unity.txt");
        unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0, 0, -10);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 9.6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.03f, 0.08f);
        camGo.AddComponent<AudioListener>();
        var fit = camGo.AddComponent<CameraFit>();

        // Static room
        var room = new GameObject("Room").transform;
        foreach (var n in new[] { "bg_lounge", "moonbeam", "window_jali", "curtains", "sofa", "charpai" })
            Spr(n, n, room, Info(n));

        // Spawn points
        var spawnRoot = new GameObject("Spawns").transform;
        var spawnList = new System.Collections.Generic.List<SpawnPoint>();
        foreach (var s in layout.spawns)
        {
            var go = new GameObject("Spawn_" + s.id);
            go.transform.SetParent(spawnRoot);
            var sp = go.AddComponent<SpawnPoint>();
            sp.kind = (SpawnPoint.Kind)Enum.Parse(typeof(SpawnPoint.Kind), s.kind);
            sp.pan = s.pan;
            sp.unlockNight = Mathf.RoundToInt(s.unlockAt);
            sp.tapArea = new Rect(s.tapX - s.tapW / 2f, s.tapY - s.tapH / 2f, s.tapW, s.tapH);

            var dadi = Spr("Dadi", s.sprite, go.transform, null);
            dadi.transform.position = new Vector3(s.feetX, s.feetY, 0);
            dadi.transform.localScale = Vector3.one * s.scale;
            dadi.GetComponent<SpriteRenderer>().sortingOrder = s.order;
            dadi.SetActive(false);
            sp.dadi = dadi;

            var strip = Info("strip_" + s.id);
            var warn = Spr("Warning", "light_strip", go.transform, strip);
            warn.transform.localScale = new Vector3(strip.w / 2.56f, strip.h / 0.64f, 1);
            warn.SetActive(false);
            sp.warningVisual = warn;

            if (sp.kind == SpawnPoint.Kind.Door)
            {
                var d = Info("door_" + s.id);
                var door = Spr("DoorClosed", "door_panel", go.transform, d);
                door.transform.localScale = new Vector3(d.w / 2.40f, d.h / 4.64f, 1);
                door.SetActive(false);
                sp.doorPanel = door;

                var chotu = Spr("Chotu", "chotu", go.transform, null);
                chotu.transform.position = new Vector3(s.feetX + 0.4f * s.scale, s.feetY, 0);
                chotu.transform.localScale = Vector3.one * s.scale;
                chotu.GetComponent<SpriteRenderer>().sortingOrder = s.order;
                chotu.SetActive(false);
                sp.chotu = chotu;
            }
            spawnList.Add(sp);
        }

        // Bacha
        var bachaGo = new GameObject("Bacha");
        var bacha = bachaGo.AddComponent<Bacha>();
        bacha.eatingSprite = Spr("Eating", "bacha_eat", bachaGo.transform, Info("bacha_eat"));
        bacha.hidingSprite = Spr("Hiding", "bacha_hide", bachaGo.transform, Info("bacha_hide"));

        // Cookie box
        var boxGo = new GameObject("CookieBox");
        var box = boxGo.AddComponent<CookieBox>();
        Spr("Stool", "stool", boxGo.transform, Info("stool"));
        box.box = Spr("Box", "cookie_box", boxGo.transform, Info("cookie_box")).transform;
        box.lid = Spr("Lid", "box_lid", boxGo.transform, Info("box_lid")).transform;
        box.golden = Spr("GoldenCookie", "golden_cookie", boxGo.transform, Info("golden_cookie")).transform;
        var anchor = new GameObject("BoxLabelAnchor").transform;
        anchor.SetParent(boxGo.transform);
        var bl = Info("box_label");
        anchor.position = new Vector3(bl.x, bl.y, 0);

        // Game-over props
        var chappal = Spr("ThrownChappal", "chappal", null, null);
        chappal.GetComponent<SpriteRenderer>().sortingOrder = 100;
        chappal.SetActive(false);
        var crack = Spr("ScreenCrack", "crack", null, null);
        crack.GetComponent<SpriteRenderer>().sortingOrder = 110;
        crack.SetActive(false);

        // Audio
        var audioGo = new GameObject("Audio");
        var am = audioGo.AddComponent<AudioManager>();
        am.ambientSource = audioGo.AddComponent<AudioSource>();
        am.sfxSource = audioGo.AddComponent<AudioSource>();
        am.stepSource = audioGo.AddComponent<AudioSource>();
        foreach (var src in audioGo.GetComponents<AudioSource>()) src.playOnAwake = false;
        am.ambient = Clip("ambient"); am.footstep = Clip("footstep"); am.crunch = Clip("crunch");
        am.knock = Clip("knock"); am.doorClose = Clip("door_close"); am.sting = Clip("sting");
        am.whoosh = Clip("whoosh"); am.glassBreak = Clip("glass_break"); am.jump = Clip("jump"); am.praise = Clip("praise");
        am.cough = Clip("cough"); am.giggle = Clip("giggle"); am.upsBeep = Clip("ups_beep"); am.golden = Clip("golden");
        am.nightStart = Clip("night_start"); am.powerOff = Clip("power_off");

        // Managers
        var gameGo = new GameObject("Game");
        var gm = gameGo.AddComponent<GameManager>();
        var seq = gameGo.AddComponent<GameOverSequence>();
        var dadiGo = new GameObject("Dadi");
        var dc = dadiGo.AddComponent<DadiController>();

        // UI
        var uiGo = new GameObject("UI");
        var doc = uiGo.AddComponent<UIDocument>();
        doc.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/GamePanelSettings.asset");
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Game.uxml");
        var ui = uiGo.AddComponent<UIController>();
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        // Wire references
        ui.document = doc; ui.game = gm; ui.audioManager = am; ui.cam = cam; ui.boxLabelAnchor = anchor;
        ui.howToIcons = new Sprite[EnglishText.HowTo.GetLength(0)];
        for (int i = 0; i < ui.howToIcons.Length; i++)
            ui.howToIcons[i] = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/" + EnglishText.HowTo[i, 0] + ".png");
        dc.spawns = spawnList.ToArray(); dc.game = gm; dc.audioManager = am; dc.ui = ui;
        seq.cam = cam; seq.cameraFit = fit; seq.audioManager = am; seq.chappal = chappal.transform; seq.crack = crack.transform;
        gm.cam = cam; gm.bacha = bacha; gm.cookieBox = box; gm.dadi = dc; gm.audioManager = am; gm.ui = ui; gm.gameOver = seq;

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        return "scene saved " + ScenePath + "; ";
    }

    public static string Settings()
    {
        PlayerSettings.companyName = "GCC Sangat";
        PlayerSettings.productName = "Shh Dadi";
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.gccsangat.shhdadi");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        AssetDatabase.SaveAssets();
        return "player settings set";
    }

    static Layout LoadLayout(string path)
    {
        var sprites = new System.Collections.Generic.List<SpriteInfo>();
        var spawns = new System.Collections.Generic.List<SpawnInfo>();
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        foreach (string line in File.ReadAllLines(path))
        {
            string[] p = line.Split(' ');
            Func<int, float> f = i => float.Parse(p[i], inv);
            if (p[0] == "S")
                sprites.Add(new SpriteInfo { name = p[1], x = f(2), y = f(3), w = f(4), h = f(5), order = (int)f(6) });
            else if (p[0] == "P")
                spawns.Add(new SpawnInfo { id = p[1], kind = p[2], sprite = p[3], unlockAt = f(4), pan = f(5), scale = f(6),
                    feetX = f(7), feetY = f(8), tapX = f(9), tapY = f(10), tapW = f(11), tapH = f(12), order = (int)f(13) });
        }
        return new Layout { sprites = sprites.ToArray(), spawns = spawns.ToArray() };
    }

    static SpriteInfo Info(string name)
    {
        foreach (var s in layout.sprites) if (s.name == name) return s;
        throw new Exception("layout has no " + name);
    }

    static GameObject Spr(string goName, string spriteName, Transform parent, SpriteInfo info)
    {
        var go = new GameObject(goName);
        if (parent != null) go.transform.SetParent(parent);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/" + spriteName + ".png");
        if (sr.sprite == null) throw new Exception("missing sprite " + spriteName);
        if (unlit != null) sr.sharedMaterial = unlit;
        if (info != null)
        {
            go.transform.position = new Vector3(info.x, info.y, 0);
            sr.sortingOrder = info.order;
        }
        return go;
    }

    static AudioClip Clip(string name)
    {
        var c = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + name + ".wav");
        if (c == null) throw new Exception("missing clip " + name);
        return c;
    }
}
