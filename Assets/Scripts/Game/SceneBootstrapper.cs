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
    [Header("BCI Mode")]
    [Tooltip("True  = keyboard mock (Space/1-4/QWER/…). False = real backend predictions via LSL.")]
    public bool UseMockBCI = true;

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

    // ASCII face expressions for the 4 small note-head buttons (index 0=Calm → 3=Yelling)
    static readonly string[] FaceTexts = { "- -\n ~", "^ ^\n u", "o o\n D", "O O\n !" };

    // ── Entry point ───────────────────────────────────────────────────────────

    void Awake()
    {
        // 1. BCI / game-logic system — must run first so singletons are set
        //    before any UI component's Awake tries to call them.
        BuildBCISystem(
            out var flow,
            out var bci,
            out var handler);

        // 2. EventSystem — required for all UI button input
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // 3. Canvas + all panels
        var canvas        = BuildCanvas();
        var mainMenu      = BuildMainMenu(canvas.transform);
        var trainingPanel = BuildTrainingPanel(canvas.transform, out var tc);

        BuildGamePanel(canvas.transform,
            out var gamePanel,
            out var blobs,
            out var pitchButtons,
            out var playBtn,
            out var playLabel,
            out var playPausePresenter);

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
        flow.BCIController   = bci;

        // 5. Wire BCI controller (16 pitch buttons + 1 play/pause = 17 stimuli)
        bci.SelectionHandler   = handler;
        bci.Presenters         = pitchButtons;
        bci.PlayPausePresenter = playPausePresenter;

        // 6. Wire TrainingController UI
        tc.StartTrainBtn.onClick.AddListener(tc.BeginTraining);
        tc.StartEvalBtn.onClick.AddListener(tc.BeginEvaluation);
        tc.StopBtn.onClick.AddListener(tc.StopSequence);
    }

    // ── BCI / logic system ────────────────────────────────────────────────────

    void BuildBCISystem(
        out GameFlowController      flow,
        out MindCTRLBCIController   bci,
        out CharacterSelectionHandler handler)
    {
        var go = new GameObject("BCISystem");
        go.AddComponent<GameConfig>();          // sets GameConfig.Instance
        GameConfig.Instance.useMockBCI = UseMockBCI;
        flow    = go.AddComponent<GameFlowController>();   // sets GameFlowController.Instance
        handler = go.AddComponent<CharacterSelectionHandler>();
        bci     = go.AddComponent<MindCTRLBCIController>();
        go.AddComponent<MockP300Input>();
        go.AddComponent<TrainingController>();
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
        out GameObject               gamePanel,
        out CharacterBlobPresenter[] blobs,
        out PitchButtonPresenter[]   pitchButtons,
        out Button                   playBtn,
        out Text                     playLabel,
        out PlayPauseButtonPresenter playPausePresenter)
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
        var rowGO = MakeContainer(panel.transform, "BlobRow");

        var hl = rowGO.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment       = TextAnchor.UpperCenter;
        hl.spacing              = 14;
        hl.childForceExpandWidth  = true;
        hl.childForceExpandHeight = false;
        hl.padding = new RectOffset(6, 6, 0, 0);

        var rle = rowGO.AddComponent<LayoutElement>();
        rle.flexibleWidth  = 1;
        rle.flexibleHeight = 1;
        rle.minHeight      = 400;

        blobs        = new CharacterBlobPresenter[4];
        pitchButtons = new PitchButtonPresenter[16];

        for (int c = 0; c < 4; c++)
        {
            blobs[c] = BuildBlobColumn(rowGO.transform, c, out var colBtns);
            for (int p = 0; p < 4; p++)
                pitchButtons[c * 4 + p] = colBtns[p];
        }

        // Bottom bar
        var bar = MakeContainer(panel.transform, "BottomBar");

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

        // Add P300 stimulus component so the play/pause button is the 17th stimulus
        playPausePresenter             = playGO.AddComponent<PlayPauseButtonPresenter>();
        playPausePresenter.ButtonImage = playGO.GetComponent<Image>();

        gamePanel = panel;
    }

    // ── Training Panel ────────────────────────────────────────────────────────

    GameObject BuildTrainingPanel(Transform root, out TrainingController tc)
    {
        // Resolve TrainingController added by BuildBCISystem
        tc = FindObjectOfType<TrainingController>();

        var panel = MakePanel(root, "TrainingPanel", new Color(0.04f, 0.04f, 0.14f));
        Stretch(panel);
        panel.SetActive(false);

        var vl = panel.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment         = TextAnchor.UpperCenter;
        vl.spacing                = 14;
        vl.padding                = new RectOffset(40, 40, 24, 24);
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;

        // Header
        MakeText(panel.transform, "Header", "TRAINING MODE",
            48, new Color(0.65f, 0.75f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold,
            prefH: 62, flexW: true);

        MakeSeparator(panel.transform);

        // Cue box
        var cueBox = MakePanel(panel.transform, "CueBox", new Color(0.10f, 0.10f, 0.22f));
        var cueLE  = cueBox.AddComponent<LayoutElement>();
        cueLE.preferredHeight = 160;
        cueLE.flexibleWidth   = 1;

        var cueText = MakeText(cueBox.transform, "CueLabel",
            "Press a button to begin.",
            36, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold,
            prefH: 160, flexW: true).GetComponent<Text>();
        var cueRT = cueText.GetComponent<RectTransform>();
        cueRT.anchorMin = Vector2.zero;
        cueRT.anchorMax = Vector2.one;
        cueRT.offsetMin = new Vector2(12, 8);
        cueRT.offsetMax = new Vector2(-12, -8);
        if (tc != null) tc.CueLabel = cueText;

        // Progress label
        var progressText = MakeText(panel.transform, "ProgressLabel", "",
            22, new Color(0.7f, 0.8f, 1f), TextAnchor.MiddleCenter,
            prefH: 30, flexW: true).GetComponent<Text>();
        if (tc != null) tc.ProgressLabel = progressText;

        // Result box
        var resultBox = MakePanel(panel.transform, "ResultBox", new Color(0.08f, 0.10f, 0.18f));
        var resultLE  = resultBox.AddComponent<LayoutElement>();
        resultLE.preferredHeight = 90;
        resultLE.flexibleWidth   = 1;

        var resultText = MakeText(resultBox.transform, "ResultLabel", "",
            22, new Color(0.6f, 1f, 0.6f), TextAnchor.MiddleCenter,
            prefH: 90, flexW: true).GetComponent<Text>();
        var rrt = resultText.GetComponent<RectTransform>();
        rrt.anchorMin = Vector2.zero;
        rrt.anchorMax = Vector2.one;
        rrt.offsetMin = new Vector2(12, 6);
        rrt.offsetMax = new Vector2(-12, -6);
        if (tc != null) tc.ResultLabel = resultText;

        MakeSeparator(panel.transform);

        // Button bar
        var bar = MakeContainer(panel.transform, "ButtonBar");
        var bhl = bar.AddComponent<HorizontalLayoutGroup>();
        bhl.childAlignment         = TextAnchor.MiddleCenter;
        bhl.spacing                = 20;
        bhl.childForceExpandWidth  = false;
        bhl.childForceExpandHeight = false;
        bar.AddComponent<LayoutElement>().preferredHeight = 70;

        var trainGO = MakeButton(bar.transform, "TrainBtn", "START TRAINING",
            new Color(0.20f, 0.55f, 0.85f), 26, 260, 60, () => { }, FontStyle.Bold);
        var trainBtn = trainGO.GetComponent<Button>();
        if (tc != null) tc.StartTrainBtn = trainBtn;

        var evalGO = MakeButton(bar.transform, "EvalBtn", "START EVALUATION",
            new Color(0.20f, 0.70f, 0.35f), 26, 280, 60, () => { }, FontStyle.Bold);
        var evalBtn = evalGO.GetComponent<Button>();
        if (tc != null) tc.StartEvalBtn = evalBtn;

        var stopGO = MakeButton(bar.transform, "StopBtn", "STOP",
            new Color(0.65f, 0.18f, 0.18f), 24, 130, 60, () => { });
        var stopBtn = stopGO.GetComponent<Button>();
        stopBtn.interactable = false;
        if (tc != null) tc.StopBtn = stopBtn;

        // Back button
        var backBar = MakeContainer(panel.transform, "BackBar");
        var bbhl = backBar.AddComponent<HorizontalLayoutGroup>();
        bbhl.childAlignment         = TextAnchor.MiddleCenter;
        bbhl.childForceExpandWidth  = false;
        bbhl.childForceExpandHeight = false;
        backBar.AddComponent<LayoutElement>().preferredHeight = 52;

        MakeButton(backBar.transform, "BackBtn", "< MAIN MENU",
            new Color(0.28f, 0.28f, 0.40f), 22, 220, 48,
            () => GameFlowController.Instance?.ShowMainMenu());

        return panel;
    }

    // ── Blob column ───────────────────────────────────────────────────────────

    CharacterBlobPresenter BuildBlobColumn(
        Transform parent, int charIdx,
        out PitchButtonPresenter[] pitchBtns)
    {
        var color = BlobColors[charIdx];

        // Card background
        var col = MakePanel(parent, $"Blob_{CharNames[charIdx]}", new Color(1, 1, 1, 0.06f));
        var vl  = col.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment       = TextAnchor.UpperCenter;
        vl.spacing              = 6;
        vl.padding              = new RectOffset(8, 8, 10, 10);
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;
        col.AddComponent<LayoutElement>().flexibleWidth = 1;

        // Character name
        MakeText(col.transform, "Name", CharNames[charIdx].ToUpper(),
            20, color, TextAnchor.MiddleCenter, FontStyle.Bold,
            prefH: 26, flexW: true);

        // ── Main area: big blob (left) + note-head stack (right) ──────────────
        var mainArea = MakeContainer(col.transform, "MainArea");
        var hl = mainArea.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment       = TextAnchor.MiddleCenter;
        hl.spacing              = 6;
        hl.childForceExpandWidth  = false;
        hl.childForceExpandHeight = true;
        var mainLe = mainArea.AddComponent<LayoutElement>();
        mainLe.flexibleWidth  = 1;
        mainLe.flexibleHeight = 1;
        mainLe.minHeight      = 300;

        // Big blob ─────────────────────────────────────────────────────────────
        // blobBox is controlled by the layout; BlobPresenter lives one level
        // below so the sway animation never fights the layout system.
        var blobBox = MakeContainer(mainArea.transform, "BlobBox");
        var bble = blobBox.AddComponent<LayoutElement>();
        bble.flexibleWidth  = 1;
        bble.flexibleHeight = 1;

        var blobGO = MakeContainer(blobBox.transform, "BlobPresenter");
        var brt = (RectTransform)blobGO.transform;
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;

        var circleGO  = new GameObject("Circle");
        circleGO.transform.SetParent(blobGO.transform, false);
        var circleImg = circleGO.AddComponent<Image>();
        circleImg.color = color;
        var crt = circleGO.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.10f, 0.10f);
        crt.anchorMax = new Vector2(0.90f, 0.90f);
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;

        var faceGO  = new GameObject("FaceLabel");
        faceGO.transform.SetParent(blobGO.transform, false);
        var faceTxt = faceGO.AddComponent<Text>();
        faceTxt.font      = DefaultFont;
        faceTxt.text      = FaceTexts[1];  // "^ ^\n u" at start (Happy)
        faceTxt.fontSize  = 32;
        faceTxt.fontStyle = FontStyle.Bold;
        faceTxt.alignment = TextAnchor.MiddleCenter;
        faceTxt.color     = new Color(1f, 1f, 1f, 0.90f);
        var frt = faceGO.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;

        blobGO.AddComponent<AudioSource>();   // must exist before CharacterBlobPresenter.Awake()
        var blob = blobGO.AddComponent<CharacterBlobPresenter>();
        blob.CharacterIndex = charIdx;
        blob.CharacterName  = CharNames[charIdx];
        blob.BlobColor      = color;
        blob.BlobImage      = circleImg;
        blob.FaceLabel      = faceTxt;
        blob.SwayAmount      = 14f;
        blob.BobAmount       = 7f;
        blob.FaceExpressions = FaceTexts;

        // Note-head stack (4 small blob heads, Yelling at top / Calm at bottom) ─
        var noteStack = MakeContainer(mainArea.transform, "NoteStack");
        var nsVl = noteStack.AddComponent<VerticalLayoutGroup>();
        nsVl.childAlignment       = TextAnchor.MiddleCenter;
        nsVl.spacing              = 4;
        nsVl.childForceExpandWidth  = true;
        nsVl.childForceExpandHeight = false;
        noteStack.AddComponent<LayoutElement>().preferredWidth = 72;

        pitchBtns = new PitchButtonPresenter[4];
        for (int p = 3; p >= 0; p--)
            pitchBtns[p] = BuildNoteHead(noteStack.transform, charIdx, p);

        return blob;
    }

    // ── Note-head button (small blob face, pitch selector) ────────────────────

    PitchButtonPresenter BuildNoteHead(Transform parent, int charIdx, int pitchIdx)
    {
        var blobColor = BlobColors[charIdx];

        var go  = new GameObject($"NoteHead_{pitchIdx}");
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        var le  = go.AddComponent<LayoutElement>();
        le.preferredWidth  = 68;
        le.flexibleHeight  = 1;
        le.minHeight       = 56;

        // Two-line ASCII face expression (eyes / mouth)
        var faceGO  = new GameObject("Face");
        faceGO.transform.SetParent(go.transform, false);
        var faceTxt = faceGO.AddComponent<Text>();
        faceTxt.font      = DefaultFont;
        faceTxt.text      = FaceTexts[pitchIdx];
        faceTxt.fontSize  = 13;
        faceTxt.alignment = TextAnchor.MiddleCenter;
        faceTxt.color     = Color.white;
        var frt = faceGO.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;

        // Dim blob colour when not active; full blob colour when active
        var presenter = go.AddComponent<PitchButtonPresenter>();
        presenter.CharacterIndex = charIdx;
        presenter.PitchIndex     = pitchIdx;
        presenter.ButtonImage    = img;
        presenter.NormalColor    = new Color(blobColor.r * 0.35f, blobColor.g * 0.35f, blobColor.b * 0.35f);
        presenter.ActiveColor    = new Color(blobColor.r,          blobColor.g,          blobColor.b);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.navigation    = new Navigation { mode = Navigation.Mode.None };
        var cols = btn.colors;
        cols.normalColor      = Color.white;
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

    // Unity 6 no longer assigns a default font when Text is created via code.
    static Font _font;
    static Font DefaultFont => _font != null ? _font
        : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

    static GameObject MakeText(Transform parent, string name, string text,
        int size, Color color, TextAnchor align, FontStyle style = FontStyle.Normal,
        float prefW = -1, float prefH = -1, bool flexW = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var t = go.AddComponent<Text>();
        t.font      = DefaultFont;
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
        lbl.font      = DefaultFont;
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

    /// <summary>
    /// Creates a layout container inside a Canvas hierarchy.
    /// Adding a transparent Image forces Unity to replace the plain Transform
    /// with a RectTransform, which is required by LayoutGroup and anchored children.
    /// </summary>
    static GameObject MakeContainer(Transform parent, string name)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color         = Color.clear;
        img.raycastTarget = false;
        return go;
    }

    static void MakeSpacer(Transform parent, float height)
    {
        var go = MakeContainer(parent, "Spacer");
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
