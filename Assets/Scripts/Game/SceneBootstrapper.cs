using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Procedurally builds the entire Opera of Blobs scene at runtime.
///
/// Setup: add this script to any empty GameObject in the scene.
/// Then assign AudioClips to the four VoiceClips arrays in the Inspector
/// (4 clips per character: index 0 = Calm → 3 = Yelling).
///
/// Controls (mock / keyboard mode):
///   Number keys 1-4   → Red pitch   0-3
///   Q W E R           → Blue pitch  0-3
///   A S D F           → Yellow pitch 0-3
///   Z X C V           → Green pitch  0-3
///   Space             → Toggle Play / Pause
///   Escape            → Return to Main Menu
/// </summary>
public class SceneBootstrapper : MonoBehaviour
{
    [Header("Voice Clips — 4 per character (index 0=Calm, 1=Happy, 2=Excited, 3=Yelling)")]
    public AudioClip[] RedVoiceClips    = new AudioClip[4];
    public AudioClip[] BlueVoiceClips   = new AudioClip[4];
    public AudioClip[] YellowVoiceClips = new AudioClip[4];
    public AudioClip[] GreenVoiceClips  = new AudioClip[4];

    // ── Character data ────────────────────────────────────────────────────────

    static readonly string[] CharNames  = { "Red", "Blue", "Yellow", "Green" };
    static readonly Color[]  BlobColors =
    {
        new Color(0.90f, 0.22f, 0.22f),  // Red
        new Color(0.22f, 0.45f, 0.90f),  // Blue
        new Color(0.95f, 0.82f, 0.10f),  // Yellow
        new Color(0.22f, 0.75f, 0.30f),  // Green
    };

    // Pitch level display names — shown on the blob face and on the buttons
    static readonly string[] PitchNames = { "Calm", "Happy", "Excited", "Yelling!" };

    // Per-pitch tint for the inactive button colour (active always uses PitchButtonPresenter.ActiveColor)
    static readonly Color[] PitchNormalColors =
    {
        new Color(0.20f, 0.30f, 0.50f),  // Calm  – cool blue
        new Color(0.28f, 0.50f, 0.28f),  // Happy – muted green
        new Color(0.55f, 0.38f, 0.10f),  // Excited – amber
        new Color(0.55f, 0.15f, 0.15f),  // Yelling – deep red
    };

    // ── Entry point ───────────────────────────────────────────────────────────

    void Awake()
    {
        // 1. BCI / game-logic system — must run first so singletons are set
        //    before any UI component's Awake tries to call them.
        BuildBCISystem(
            out var flow,
            out var bci,
            out var handler);

        // 2. Canvas + all panels
        var canvas       = BuildCanvas();
        var mainMenu     = BuildMainMenu(canvas.transform);
        var trainingPanel = BuildTrainingPanel(canvas.transform);

        BuildGamePanel(canvas.transform,
            out var gamePanel,
            out var blobs,
            out var pitchButtons,
            out var playBtn,
            out var playLabel);

        // 3. Wire voice clips
        var allClips = new[] { RedVoiceClips, BlueVoiceClips, YellowVoiceClips, GreenVoiceClips };
        for (int i = 0; i < 4; i++)
            blobs[i].VoiceClips = allClips[i];

        // 4. Wire GameFlowController
        flow.MainMenuPanel   = mainMenu;
        flow.GamePanel       = gamePanel;
        flow.TrainingPanel   = trainingPanel;
        flow.Blobs           = blobs;
        flow.PitchButtons    = pitchButtons;
        flow.PlayPauseButton = playBtn;
        flow.PlayPauseLabel  = playLabel;

        // 5. Wire BCI controller
        bci.SelectionHandler = handler;
        bci.Presenters       = pitchButtons;
    }

    // ── BCI / logic system ────────────────────────────────────────────────────

    void BuildBCISystem(
        out GameFlowController      flow,
        out MindCTRLBCIController   bci,
        out CharacterSelectionHandler handler)
    {
        var go = new GameObject("BCISystem");
        go.AddComponent<GameConfig>();          // sets GameConfig.Instance
        flow    = go.AddComponent<GameFlowController>();   // sets GameFlowController.Instance
        handler = go.AddComponent<CharacterSelectionHandler>();
        bci     = go.AddComponent<MindCTRLBCIController>();
        go.AddComponent<MockP300Input>();
    }

    // ── Canvas ────────────────────────────────────────────────────────────────

    Canvas BuildCanvas()
    {
        var go     = new GameObject("Canvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    // ── Main Menu ─────────────────────────────────────────────────────────────

    GameObject BuildMainMenu(Transform root)
    {
        var panel = MakePanel(root, "MainMenuPanel", new Color(0.05f, 0.02f, 0.12f));
        Stretch(panel);

        var vl = panel.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment       = TextAnchor.MiddleCenter;
        vl.spacing              = 30;
        vl.padding              = new RectOffset(0, 0, 80, 80);
        vl.childForceExpandWidth  = false;
        vl.childForceExpandHeight = false;

        MakeText(panel.transform, "Title", "OPERA OF BLOBS",
            84, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, prefW: 900, prefH: 110);

        MakeText(panel.transform, "Sub", "Conduct your blob orchestra!",
            30, new Color(0.75f, 0.65f, 1f), TextAnchor.MiddleCenter,
            FontStyle.Italic, prefW: 700, prefH: 46);

        MakeSpacer(panel.transform, 30);

        MakeButton(panel.transform, "NewGameBtn", "> NEW GAME",
            new Color(0.25f, 0.75f, 0.35f), 38, 340, 76,
            () => GameFlowController.Instance?.StartNewGame(), FontStyle.Bold);

        MakeButton(panel.transform, "TrainingBtn", "TRAINING",
            new Color(0.35f, 0.35f, 0.75f), 32, 280, 64,
            () => GameFlowController.Instance?.StartTraining());

        MakeSpacer(panel.transform, 10);

        MakeButton(panel.transform, "QuitBtn", "QUIT",
            new Color(0.35f, 0.18f, 0.18f), 22, 160, 46,
            () => Application.Quit());

        return panel;
    }

    // ── Game Panel ────────────────────────────────────────────────────────────

    void BuildGamePanel(Transform root,
        out GameObject            gamePanel,
        out CharacterBlobPresenter[] blobs,
        out PitchButtonPresenter[]   pitchButtons,
        out Button                   playBtn,
        out Text                     playLabel)
    {
        var panel = MakePanel(root, "GamePanel", new Color(0.05f, 0.02f, 0.12f));
        Stretch(panel);
        panel.SetActive(false);

        var vl = panel.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment       = TextAnchor.UpperCenter;
        vl.spacing              = 12;
        vl.padding              = new RectOffset(20, 20, 18, 18);
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;

        // Header
        MakeText(panel.transform, "Header", "OPERA  OF  BLOBS",
            44, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold,
            prefH: 62, flexW: true);

        // Blob row
        var rowGO = new GameObject("BlobRow");
        rowGO.transform.SetParent(panel.transform, false);

        var hl = rowGO.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment       = TextAnchor.UpperCenter;
        hl.spacing              = 14;
        hl.childForceExpandWidth  = true;
        hl.childForceExpandHeight = false;
        hl.padding = new RectOffset(6, 6, 0, 0);

        var rle = rowGO.AddComponent<LayoutElement>();
        rle.flexibleWidth  = 1;
        rle.flexibleHeight = 1;
        rle.minHeight      = 560;

        blobs        = new CharacterBlobPresenter[4];
        pitchButtons = new PitchButtonPresenter[16];

        for (int c = 0; c < 4; c++)
        {
            blobs[c] = BuildBlobColumn(rowGO.transform, c, out var colBtns);
            for (int p = 0; p < 4; p++)
                pitchButtons[c * 4 + p] = colBtns[p];
        }

        // Bottom bar
        var bar = new GameObject("BottomBar");
        bar.transform.SetParent(panel.transform, false);

        var bhl = bar.AddComponent<HorizontalLayoutGroup>();
        bhl.childAlignment       = TextAnchor.MiddleCenter;
        bhl.spacing              = 60;
        bhl.childForceExpandWidth  = false;
        bhl.childForceExpandHeight = false;

        var ble = bar.AddComponent<LayoutElement>();
        ble.preferredHeight = 84;
        ble.flexibleWidth   = 1;

        MakeButton(bar.transform, "MenuBtn", "< MENU",
            new Color(0.28f, 0.28f, 0.40f), 24, 160, 58,
            () => GameFlowController.Instance?.ShowMainMenu());

        var playGO = MakeButton(bar.transform, "PlayBtn", ">  PLAY",
            new Color(0.20f, 0.60f, 0.95f), 34, 250, 70,
            () => GameFlowController.Instance?.TogglePlay(), FontStyle.Bold);

        playBtn   = playGO.GetComponent<Button>();
        playLabel = playGO.GetComponentInChildren<Text>();

        gamePanel = panel;
    }

    // ── Training Panel (placeholder) ──────────────────────────────────────────

    GameObject BuildTrainingPanel(Transform root)
    {
        var panel = MakePanel(root, "TrainingPanel", new Color(0.04f, 0.04f, 0.14f));
        Stretch(panel);
        panel.SetActive(false);

        // Root vertical layout
        var vl = panel.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment       = TextAnchor.UpperCenter;
        vl.spacing              = 20;
        vl.padding              = new RectOffset(40, 40, 30, 30);
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;

        // ── Header ────────────────────────────────────────────────────────────
        MakeText(panel.transform, "Header", "TRAINING MODE",
            52, new Color(0.65f, 0.75f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold,
            prefH: 70, flexW: true);

        MakeText(panel.transform, "Sub", "Learn to control the blobs before the performance.",
            26, new Color(0.65f, 0.65f, 0.80f), TextAnchor.MiddleCenter,
            prefH: 40, flexW: true);

        MakeSeparator(panel.transform);

        // ── TBD content area ──────────────────────────────────────────────────
        var contentBox = new GameObject("ContentBox");
        contentBox.transform.SetParent(panel.transform, false);
        var cle = contentBox.AddComponent<LayoutElement>();
        cle.flexibleWidth  = 1;
        cle.flexibleHeight = 1;
        cle.minHeight      = 400;

        var cvl = contentBox.AddComponent<VerticalLayoutGroup>();
        cvl.childAlignment       = TextAnchor.MiddleCenter;
        cvl.childForceExpandWidth  = true;
        cvl.childForceExpandHeight = false;
        cvl.spacing = 16;

        MakeText(contentBox.transform, "TBDLabel", "[ Training content coming soon ]",
            30, new Color(0.45f, 0.45f, 0.60f), TextAnchor.MiddleCenter,
            FontStyle.Italic, prefH: 46, flexW: true);

        MakeText(contentBox.transform, "Hint",
            "You will be able to practise selecting pitches here\nbefore using the BCI headset in the main performance.",
            22, new Color(0.55f, 0.55f, 0.70f), TextAnchor.MiddleCenter,
            prefH: 64, flexW: true);

        MakeSeparator(panel.transform);

        // ── Back button ───────────────────────────────────────────────────────
        var bar = new GameObject("BottomBar");
        bar.transform.SetParent(panel.transform, false);
        var bhl = bar.AddComponent<HorizontalLayoutGroup>();
        bhl.childAlignment       = TextAnchor.MiddleCenter;
        bhl.childForceExpandWidth  = false;
        bhl.childForceExpandHeight = false;
        var ble = bar.AddComponent<LayoutElement>();
        ble.preferredHeight = 72;
        ble.flexibleWidth   = 1;

        MakeButton(bar.transform, "BackBtn", "< MAIN MENU",
            new Color(0.28f, 0.28f, 0.40f), 26, 240, 58,
            () => GameFlowController.Instance?.ShowMainMenu());

        return panel;
    }

    // ── Blob column ───────────────────────────────────────────────────────────

    CharacterBlobPresenter BuildBlobColumn(
        Transform parent, int charIdx,
        out PitchButtonPresenter[] pitchBtns)
    {
        // Card background
        var col = MakePanel(parent, $"Blob_{CharNames[charIdx]}", new Color(1, 1, 1, 0.06f));

        var vl = col.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment       = TextAnchor.UpperCenter;
        vl.spacing              = 7;
        vl.padding              = new RectOffset(10, 10, 14, 14);
        vl.childForceExpandWidth  = false;
        vl.childForceExpandHeight = false;
        col.AddComponent<LayoutElement>().flexibleWidth = 1;

        // ── Blob visual area ───────────────────────────────────────────────────
        // blobBox is sized by the layout; the CharacterBlobPresenter lives one
        // level deeper so the layout never fights the sway animation.
        var blobBox = new GameObject("BlobBox");
        blobBox.transform.SetParent(col.transform, false);
        blobBox.AddComponent<LayoutElement>().preferredHeight = 180;

        var blobGO = new GameObject("BlobPresenter");
        blobGO.transform.SetParent(blobBox.transform, false);
        var brt = blobGO.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;

        // Coloured circle (80 % of the box so there's room to sway without clipping)
        var circleGO  = new GameObject("Circle");
        circleGO.transform.SetParent(blobGO.transform, false);
        var circleImg = circleGO.AddComponent<Image>();
        circleImg.color = BlobColors[charIdx];
        var crt = circleGO.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.10f, 0.10f);
        crt.anchorMax = new Vector2(0.90f, 0.90f);
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;

        // Face label (current pitch name — large, centred over the circle)
        var faceGO  = new GameObject("FaceLabel");
        faceGO.transform.SetParent(blobGO.transform, false);
        var faceTxt = faceGO.AddComponent<Text>();
        faceTxt.text      = PitchNames[1].ToUpper(); // "HAPPY" at start
        faceTxt.fontSize  = 28;
        faceTxt.fontStyle = FontStyle.Bold;
        faceTxt.alignment = TextAnchor.MiddleCenter;
        faceTxt.color     = new Color(1f, 1f, 1f, 0.90f);
        var frt = faceGO.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;

        // CharacterBlobPresenter on blobGO (one level below layout)
        var blob = blobGO.AddComponent<CharacterBlobPresenter>();
        blob.CharacterIndex = charIdx;
        blob.CharacterName  = CharNames[charIdx];
        blob.BlobColor      = BlobColors[charIdx];
        blob.BlobImage      = circleImg;
        blob.FaceLabel      = faceTxt;
        blob.SwayAmount     = 14f;  // pixels — visible at UI scale
        blob.BobAmount      = 7f;

        // ── Character name ─────────────────────────────────────────────────────
        MakeText(col.transform, "Name", CharNames[charIdx].ToUpper(),
            22, BlobColors[charIdx], TextAnchor.MiddleCenter, FontStyle.Bold,
            prefW: 180, prefH: 30);

        // Divider label
        MakeText(col.transform, "PitchLbl", "-- PITCH --",
            14, new Color(0.50f, 0.50f, 0.62f), TextAnchor.MiddleCenter,
            prefW: 180, prefH: 20);

        // ── Pitch buttons (Yelling at top, Calm at bottom) ─────────────────────
        pitchBtns = new PitchButtonPresenter[4];
        for (int p = 3; p >= 0; p--)
            pitchBtns[p] = BuildPitchButton(col.transform, charIdx, p);

        return blob;
    }

    // ── Pitch button ──────────────────────────────────────────────────────────

    PitchButtonPresenter BuildPitchButton(Transform parent, int charIdx, int pitchIdx)
    {
        var go = new GameObject($"Pitch_{pitchIdx}");
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = 170;
        le.preferredHeight = 48;

        // Text label inside the button
        var lblGO = new GameObject("Lbl");
        lblGO.transform.SetParent(go.transform, false);
        var lbl = lblGO.AddComponent<Text>();
        lbl.text      = PitchNames[pitchIdx];
        lbl.fontSize  = 20;
        lbl.alignment = TextAnchor.MiddleCenter;
        lbl.color     = Color.white;
        var lrt = lblGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(4, 4);
        lrt.offsetMax = new Vector2(-4, -4);

        // PitchButtonPresenter manages colour state (Normal / Active / Flash)
        var presenter = go.AddComponent<PitchButtonPresenter>();
        presenter.CharacterIndex = charIdx;
        presenter.PitchIndex     = pitchIdx;
        presenter.ButtonImage    = img;
        presenter.NormalColor    = PitchNormalColors[pitchIdx];
        // ActiveColor / FlashColor / TargetColor use PitchButtonPresenter defaults

        // Unity Button for mouse / touch input
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.navigation    = new Navigation { mode = Navigation.Mode.None };

        var cols = btn.colors;
        cols.normalColor      = Color.white;          // tint multiplied onto img.color
        cols.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        cols.pressedColor     = new Color(0.75f, 0.75f, 0.75f);
        btn.colors = cols;

        int ci = charIdx, pi = pitchIdx;
        btn.onClick.AddListener(() => GameFlowController.Instance?.SetPitch(ci, pi));

        return presenter;
    }

    // ── UI helper methods ─────────────────────────────────────────────────────

    static GameObject MakePanel(Transform parent, string name, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        return go;
    }

    static void Stretch(GameObject go)
    {
        var rt    = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static GameObject MakeText(Transform parent, string name, string text,
        int size, Color color, TextAnchor align, FontStyle style = FontStyle.Normal,
        float prefW = -1, float prefH = -1, bool flexW = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var t = go.AddComponent<Text>();
        t.text      = text;
        t.fontSize  = size;
        t.color     = color;
        t.alignment = align;
        t.fontStyle = style;

        if (prefW >= 0 || prefH >= 0 || flexW)
        {
            var le = go.AddComponent<LayoutElement>();
            if (prefW >= 0) le.preferredWidth  = prefW;
            if (prefH >= 0) le.preferredHeight = prefH;
            if (flexW)      le.flexibleWidth   = 1;
        }
        return go;
    }

    static GameObject MakeButton(Transform parent, string name, string label,
        Color bg, int fontSize, float prefW, float prefH,
        UnityEngine.Events.UnityAction action,
        FontStyle style = FontStyle.Normal)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = bg;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.navigation    = new Navigation { mode = Navigation.Mode.None };

        var lblGO = new GameObject("Lbl");
        lblGO.transform.SetParent(go.transform, false);
        var lbl = lblGO.AddComponent<Text>();
        lbl.text      = label;
        lbl.fontSize  = fontSize;
        lbl.fontStyle = style;
        lbl.alignment = TextAnchor.MiddleCenter;
        lbl.color     = Color.white;
        var lrt = lblGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(8, 4);
        lrt.offsetMax = new Vector2(-8, -4);

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = prefW;
        le.preferredHeight = prefH;

        btn.onClick.AddListener(action);
        return go;
    }

    static void MakeSpacer(Transform parent, float height)
    {
        var go = new GameObject("Spacer");
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredHeight = height;
    }

    static void MakeSeparator(Transform parent)
    {
        var go  = new GameObject("Separator");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.10f);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 2;
        le.flexibleWidth   = 1;
    }
}
