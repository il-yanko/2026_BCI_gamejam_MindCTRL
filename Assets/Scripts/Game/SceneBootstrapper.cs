using System.Collections.Generic;
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

    [Header("Art — Background / Foreground")]
    public Sprite BackgroundSprite;
    public Sprite ForegroundSprite;
    public Sprite CurtainSprite;
    public Sprite LogoSprite;

    [Header("Art — Character Body Sprites (Red, Green, Blue, Yellow)")]
    public Sprite[] BlobBodySprites = new Sprite[4];
    public GameObject[] BlobHeadObjects = new GameObject[4];

    [Header("Art — Pitch Face Sprites (0 = Calm → 3 = Yelling)")]
    public Sprite[] FaceSprites = new Sprite[4];

    [Header("Layout — Face Sprite Buttons")]
    [Tooltip("Width of the face-sprite column (also controls visible sprite size)")]
    public float FaceStackWidth    = 80f;
    [Tooltip("Height of each face button in pixels — decrease to make buttons smaller and push them to the top")]
    public float FaceButtonHeight  = 80f;
    [Tooltip("Gap in pixels between the four face buttons")]
    public float FaceButtonSpacing = 8f;
    [Tooltip("Pixels from the top before the first face button")]
    public int   FaceStackTopPad   = 0;

    [Header("Layout — Blob Area")]
    [Tooltip("Horizontal gap in pixels between the four blob columns")]
    public float BlobColumnSpacing = 14f;
    [Tooltip("Width÷Height aspect ratio for each blob container (1 = square, 0.75 = portrait, 1.33 = landscape)")]
    public float BlobAspectRatio   = 1f;
    [Tooltip("Width÷Height aspect ratio for the face image inside each pitch button (1 = square)")]
    public float FaceAspectRatio   = 1f;

    // Shared list of all four NoteStack LayoutElements — populated in BuildBlobColumn(),
    // used by each NoteStackResizeHandle so dragging any handle resizes all four at once.
    private readonly List<LayoutElement> _noteStackElements = new List<LayoutElement>();

    // ── Character data ────────────────────────────────────────────────────────
    // Left-to-right order: Red, Green, Blue, Yellow

    static readonly string[] CharNames  = { "Red", "Green", "Blue", "Yellow" };
    static readonly Color[]  BlobColors =
    {
        new Color(0.90f, 0.22f, 0.22f),  // Red
        new Color(0.22f, 0.75f, 0.30f),  // Green
        new Color(0.22f, 0.45f, 0.90f),  // Blue
        new Color(0.95f, 0.82f, 0.10f),  // Yellow
    };

    // Pitch level display names
    static readonly string[] PitchNames = { "Calm", "Happy", "Excited", "Yelling!" };

    // ── Entry point ───────────────────────────────────────────────────────────

    void Awake()
    {
        Application.runInBackground = true;

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
        var canvas   = BuildCanvas();

        // Background shared across all windows
        {
            var bgGO  = new GameObject("Background");
            bgGO.transform.SetParent(canvas.transform, false);
            var bgImg = bgGO.AddComponent<Image>();
            if (BackgroundSprite != null)
                bgImg.sprite = BackgroundSprite;
            else
                bgImg.color = new Color(0.05f, 0.02f, 0.12f);
            bgImg.raycastTarget = false;
            bgImg.preserveAspect = false;
            Stretch(bgGO);
        }

        var mainMenu = BuildMainMenu(canvas.transform);

        // GamePanel added first; TrainingPanel is a later sibling so it renders on top.
        BuildGamePanel(canvas.transform,
            out var gamePanel,
            out var blobs,
            out var pitchButtons,
            out var playBtn,
            out var playLabel,
            out var playPausePresenter);

        // Foreground layer (audience silhouettes — rendered above blobs, pointer-transparent)
        if (ForegroundSprite != null)
        {
            var fgGO  = new GameObject("Foreground");
            fgGO.transform.SetParent(canvas.transform, false);
            var fgImg = fgGO.AddComponent<Image>();
            fgImg.sprite         = ForegroundSprite;
            fgImg.raycastTarget  = false;
            fgImg.preserveAspect = false;
            Stretch(fgGO);
        }

        var trainingPanel = BuildTrainingPanel(canvas.transform, out var tc);

        // 3. Wire voice clips — procedural funny voices fill any null slots
        var allClips = new[] { RedVoiceClips, BlueVoiceClips, YellowVoiceClips, GreenVoiceClips };
        for (int i = 0; i < 4; i++)
        {
            var clips = allClips[i];
            if (clips == null || clips.Length < 4)
            {
                var filled = new AudioClip[4];
                for (int p = 0; p < 4; p++)
                    filled[p] = (clips != null && p < clips.Length && clips[p] != null)
                        ? clips[p]
                        : ProceduralVoiceClips.Generate(i, p);
                clips = filled;
            }
            else
            {
                for (int p = 0; p < 4; p++)
                    if (clips[p] == null)
                        clips[p] = ProceduralVoiceClips.Generate(i, p);
            }
            blobs[i].VoiceClips = clips;
        }

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
        go.AddComponent<AudioListener>();       // exactly one listener in the scene
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
        var panel = new GameObject("MainMenu", typeof(RectTransform));
        panel.transform.SetParent(root, false);
        Stretch(panel);

        var curtains = new GameObject("Curtains");
        curtains.transform.SetParent(panel.transform);
        var curtainImg = curtains.AddComponent<Image>();
        if (CurtainSprite != null)
            curtainImg.sprite = CurtainSprite;
        Stretch(curtains);
        var arFitter = curtains.AddComponent<AspectRatioFitter>();
        arFitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
        arFitter.aspectRatio = 2;
        arFitter.transform.localPosition = new Vector2(0f, 125f);

        var logo = new GameObject("Logo");
        logo.transform.SetParent(panel.transform);
        var logoImg = logo.AddComponent<Image>();
        if (LogoSprite != null)
            logoImg.sprite = LogoSprite;
        logoImg.SetNativeSize();
        logo.transform.localPosition = new Vector2(0f, 100f);

        var buttonPanel = new GameObject("MainMenuButtons", typeof(RectTransform));
        buttonPanel.transform.SetParent(panel.transform, false);
        Stretch(buttonPanel);
        buttonPanel.transform.localPosition = new Vector2(0f, -295f);
        var vl = buttonPanel.AddComponent<HorizontalLayoutGroup>();
        vl.childAlignment       = TextAnchor.MiddleCenter;
        vl.spacing              = 30;
        vl.padding              = new RectOffset(80, 80, 0, 0);
        vl.childForceExpandWidth  = false;
        vl.childForceExpandHeight = false;

        MakeButton(buttonPanel.transform, "NewGameBtn", "> NEW GAME",
            new Color(0.25f, 0.75f, 0.35f), 38, 340, 76,
            () => GameFlowController.Instance?.StartNewGame(), FontStyle.Bold);

        MakeButton(buttonPanel.transform, "TrainingBtn", "TRAINING",
            new Color(0.35f, 0.35f, 0.75f), 32, 280, 64,
            () => GameFlowController.Instance?.StartTraining());

        MakeButton(buttonPanel.transform, "QuitBtn", "QUIT",
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
        // Wrapper — shown/hidden by GameFlowController
        var wrapper = MakeContainer(root, "GamePanel");
        Stretch(wrapper);
        wrapper.SetActive(false);

        // Content panel (layout container, transparent so background shows through)
        var panel = MakeContainer(wrapper.transform, "GameContent");
        Stretch(panel);

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
        hl.childAlignment         = TextAnchor.UpperCenter;
        hl.spacing                = BlobColumnSpacing;
        hl.childForceExpandWidth  = true;
        hl.childForceExpandHeight = true;   // all 4 columns same height
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

        var playGO = MakeButton(bar.transform, "PlayBtn", ">  SING",
            new Color(0.20f, 0.60f, 0.95f), 34, 250, 70,
            () => GameFlowController.Instance?.TogglePlay(), FontStyle.Bold);

        playBtn   = playGO.GetComponent<Button>();
        playLabel = playGO.GetComponentInChildren<Text>();

        // Add P300 stimulus component so the play/pause button is the 17th stimulus
        playPausePresenter             = playGO.AddComponent<PlayPauseButtonPresenter>();
        playPausePresenter.ButtonImage = playGO.GetComponent<Image>();

        gamePanel = wrapper;
    }

    // ── Training Panel ────────────────────────────────────────────────────────

    GameObject BuildTrainingPanel(Transform root, out TrainingController tc)
    {
        tc = FindAnyObjectByType<TrainingController>();

        // Dim cell colours — must match TrainingController.CellNormal[] and CellPlayPause
        Color[] cellNormal =
        {
            new Color(0.36f, 0.08f, 0.08f),  // Red   (indices 0-3)
            new Color(0.07f, 0.28f, 0.10f),  // Green (indices 4-7)
            new Color(0.08f, 0.15f, 0.36f),  // Blue  (indices 8-11)
            new Color(0.33f, 0.29f, 0.03f),  // Yellow(indices 12-15)
        };
        var cellPlayPauseCol = new Color(0.10f, 0.28f, 0.45f);

        var gridCells = new UnityEngine.UI.Image[17];

        // ── Root wrapper — full-screen; flow.TrainingPanel points here ─────────
        var wrapper = MakeContainer(root, "TrainingPanel");
        Stretch(wrapper);
        wrapper.SetActive(false);

        // Opaque dark background
        {
            var bgGO  = new GameObject("Background");
            bgGO.transform.SetParent(wrapper.transform, false);
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color         = new Color(0.04f, 0.04f, 0.14f, 1f);
            bgImg.raycastTarget = false;
            Stretch(bgGO);
        }

        // Content layout
        var panel = MakeContainer(wrapper.transform, "TrainingContent");
        Stretch(panel);
        var vl = panel.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment         = TextAnchor.UpperCenter;
        vl.spacing                = 8;
        vl.padding                = new RectOffset(20, 20, 14, 14);
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;

        // Header
        MakeText(panel.transform, "Header", "TRAINING MODE",
            36, new Color(0.65f, 0.75f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold,
            prefH: 46, flexW: true);

        MakeSeparator(panel.transform);

        // Cue box
        var cueBox = MakePanel(panel.transform, "CueBox", new Color(0.10f, 0.10f, 0.22f));
        cueBox.AddComponent<LayoutElement>().preferredHeight = 80;
        var cueText = MakeText(cueBox.transform, "CueLabel",
            "Press a button to begin.",
            26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold).GetComponent<Text>();
        var cueRT = cueText.GetComponent<RectTransform>();
        cueRT.anchorMin = Vector2.zero;  cueRT.anchorMax = Vector2.one;
        cueRT.offsetMin = new Vector2(12, 6);  cueRT.offsetMax = new Vector2(-12, -6);
        if (tc != null) tc.CueLabel = cueText;

        // ── Stimulus grid (expands to fill remaining vertical space) ───────────
        var gridSection = MakeContainer(panel.transform, "GridSection");
        gridSection.AddComponent<LayoutElement>().flexibleHeight = 1;
        var gsVl = gridSection.AddComponent<VerticalLayoutGroup>();
        gsVl.childAlignment         = TextAnchor.UpperCenter;
        gsVl.spacing                = 6;
        gsVl.childForceExpandWidth  = true;
        gsVl.childForceExpandHeight = false;

        // 4-column character grid
        var charGrid = MakeContainer(gridSection.transform, "CharGrid");
        charGrid.AddComponent<LayoutElement>().flexibleHeight = 1;
        var cgHl = charGrid.AddComponent<HorizontalLayoutGroup>();
        cgHl.childAlignment         = TextAnchor.MiddleCenter;
        cgHl.spacing                = 16;
        cgHl.childForceExpandWidth  = true;
        cgHl.childForceExpandHeight = false;

        for (int c = 0; c < 4; c++)
        {
            var col = MakeContainer(charGrid.transform, $"Col_{CharNames[c]}");
            col.AddComponent<LayoutElement>().flexibleWidth = 1;
            var colVl = col.AddComponent<VerticalLayoutGroup>();
            colVl.childAlignment         = TextAnchor.MiddleCenter;
            colVl.spacing                = 12;
            colVl.childForceExpandWidth  = false;
            colVl.childForceExpandHeight = false;

            // Character name header
            MakeText(col.transform, "CharName", CharNames[c],
                16, BlobColors[c], TextAnchor.MiddleCenter, FontStyle.Bold,
                prefH: 24, flexW: true);

            // 4 pitch cells — top = Yelling (p=3), bottom = Calm (p=0), matching game panel order
            for (int p = 3; p >= 0; p--)
            {
                int flatIdx = c * 4 + p;
                var cellGO  = new GameObject($"Cell_{flatIdx}");
                cellGO.transform.SetParent(col.transform, false);
                var cellImg = cellGO.AddComponent<Image>();
                cellImg.color         = cellNormal[c];
                cellImg.raycastTarget = false;
                var cellLE = cellGO.AddComponent<LayoutElement>();
                cellLE.preferredWidth  = 90;
                cellLE.preferredHeight = 90;
                cellLE.minHeight       = 40;

                var lblGO = MakeText(cellGO.transform, "Lbl", PitchNames[p],
                    16, new Color(0.88f, 0.88f, 0.95f), TextAnchor.MiddleCenter);
                var lrt = lblGO.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;  lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;  lrt.offsetMax = Vector2.zero;

                gridCells[flatIdx] = cellImg;
            }
        }

        // Play/Pause cell — full width, index 16
        var ppGO  = new GameObject("Cell_16");
        ppGO.transform.SetParent(gridSection.transform, false);
        var ppImg = ppGO.AddComponent<Image>();
        ppImg.color         = cellPlayPauseCol;
        ppImg.raycastTarget = false;
        var ppLE = ppGO.AddComponent<LayoutElement>();
        ppLE.preferredHeight = 44;
        ppLE.flexibleWidth   = 1;
        var ppLbl = MakeText(ppGO.transform, "Lbl", "SING  /  PAUSE",
            17, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        var ppLrt = ppLbl.GetComponent<RectTransform>();
        ppLrt.anchorMin = Vector2.zero;  ppLrt.anchorMax = Vector2.one;
        ppLrt.offsetMin = Vector2.zero;  ppLrt.offsetMax = Vector2.zero;
        gridCells[16] = ppImg;

        // Wire stimulus grid cells to TrainingController
        if (tc != null) tc.TrainingGridCells = gridCells;

        // Progress label
        var progressText = MakeText(panel.transform, "ProgressLabel", "",
            20, new Color(0.7f, 0.8f, 1f), TextAnchor.MiddleCenter,
            prefH: 26, flexW: true).GetComponent<Text>();
        if (tc != null) tc.ProgressLabel = progressText;

        // Result box
        var resultBox = MakePanel(panel.transform, "ResultBox", new Color(0.08f, 0.10f, 0.18f));
        resultBox.AddComponent<LayoutElement>().preferredHeight = 54;
        var resultText = MakeText(resultBox.transform, "ResultLabel", "",
            18, new Color(0.6f, 1f, 0.6f), TextAnchor.MiddleCenter).GetComponent<Text>();
        var rrt = resultText.GetComponent<RectTransform>();
        rrt.anchorMin = Vector2.zero;  rrt.anchorMax = Vector2.one;
        rrt.offsetMin = new Vector2(12, 4);  rrt.offsetMax = new Vector2(-12, -4);
        if (tc != null) tc.ResultLabel = resultText;

        MakeSeparator(panel.transform);

        // Button bar
        var bar = MakeContainer(panel.transform, "ButtonBar");
        bar.AddComponent<LayoutElement>().preferredHeight = 56;
        var bhl = bar.AddComponent<HorizontalLayoutGroup>();
        bhl.childAlignment         = TextAnchor.MiddleCenter;
        bhl.spacing                = 16;
        bhl.childForceExpandWidth  = false;
        bhl.childForceExpandHeight = false;

        var trainGO = MakeButton(bar.transform, "TrainBtn", "START TRAINING",
            new Color(0.20f, 0.55f, 0.85f), 22, 240, 50, () => { }, FontStyle.Bold);
        if (tc != null) tc.StartTrainBtn = trainGO.GetComponent<Button>();

        var evalGO  = MakeButton(bar.transform, "EvalBtn", "START EVALUATION",
            new Color(0.20f, 0.70f, 0.35f), 22, 260, 50, () => { }, FontStyle.Bold);
        if (tc != null) tc.StartEvalBtn = evalGO.GetComponent<Button>();

        var stopGO  = MakeButton(bar.transform, "StopBtn", "STOP",
            new Color(0.65f, 0.18f, 0.18f), 20, 110, 50, () => { });
        var stopBtn = stopGO.GetComponent<Button>();
        stopBtn.interactable = false;
        if (tc != null) tc.StopBtn = stopBtn;

        // Back button
        var backBar = MakeContainer(panel.transform, "BackBar");
        backBar.AddComponent<LayoutElement>().preferredHeight = 42;
        var bbhl = backBar.AddComponent<HorizontalLayoutGroup>();
        bbhl.childAlignment         = TextAnchor.MiddleCenter;
        bbhl.childForceExpandWidth  = false;
        bbhl.childForceExpandHeight = false;
        MakeButton(backBar.transform, "BackBtn", "< MAIN MENU",
            new Color(0.28f, 0.28f, 0.40f), 20, 200, 40,
            () => GameFlowController.Instance?.ShowMainMenu());

        // Return wrapper (not panel) so flow.TrainingPanel.SetActive() shows/hides the full overlay
        return wrapper;
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

        // ── Main area: note-head stack (left) + big blob (right) ─────────────
        var mainArea = MakeContainer(col.transform, "MainArea");
        var hl = mainArea.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment       = TextAnchor.UpperCenter;
        hl.spacing              = 6;
        hl.childForceExpandWidth  = false;
        hl.childForceExpandHeight = true;
        var mainLe = mainArea.AddComponent<LayoutElement>();
        mainLe.flexibleWidth  = 1;
        mainLe.flexibleHeight = 1;
        // No minHeight — FaceButtonHeight controls button size; blob stretches freely.

        // Note-head stack on the LEFT — buttons sit at the top, sized by FaceButtonHeight ──
        var noteStack = MakeContainer(mainArea.transform, "NoteStack");
        var nsVl = noteStack.AddComponent<VerticalLayoutGroup>();
        nsVl.childAlignment         = TextAnchor.UpperCenter;
        nsVl.padding                = new RectOffset(0, 0, FaceStackTopPad, 0);
        nsVl.spacing                = FaceButtonSpacing;
        nsVl.childForceExpandWidth  = true;
        nsVl.childForceExpandHeight = false;  // buttons use preferredHeight; reduce to push up
        var nsLe = noteStack.AddComponent<LayoutElement>();
        nsLe.preferredWidth  = FaceStackWidth;
        nsLe.flexibleHeight  = 1;

        // Register for the shared resize handle and add the drag strip
        _noteStackElements.Add(nsLe);

        var handleGO = new GameObject("ResizeHandle");
        handleGO.transform.SetParent(noteStack.transform, false);
        handleGO.AddComponent<Image>();                                   // colour set by NoteStackResizeHandle.Awake()
        handleGO.AddComponent<LayoutElement>().ignoreLayout = true;       // excluded from VL layout
        var handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.anchorMin = new Vector2(1f, 0f);                         // anchored to right edge
        handleRT.anchorMax = Vector2.one;
        handleRT.offsetMin = new Vector2(-8f, 0f);                        // 8 px wide
        handleRT.offsetMax = Vector2.zero;
        handleGO.AddComponent<NoteStackResizeHandle>().NoteStacks = _noteStackElements;

        pitchBtns = new PitchButtonPresenter[4];
        for (int p = 3; p >= 0; p--)
            pitchBtns[p] = BuildNoteHead(noteStack.transform, charIdx, p);

        // Big blob on the RIGHT ────────────────────────────────────────────────
        // blobBox is controlled by the layout; BlobPresenter lives one level
        // below so the sway animation never fights the layout system.
        var blobBox = MakeContainer(mainArea.transform, "BlobBox");
        var bble = blobBox.AddComponent<LayoutElement>();
        bble.flexibleWidth  = 1;
        bble.flexibleHeight = 1;

        var blobGO = MakeContainer(blobBox.transform, "BlobPresenter");
        var brt = (RectTransform)blobGO.transform;
        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot     = new Vector2(0.5f, 0f);
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;
        var arf = blobGO.AddComponent<AspectRatioFitter>();
        arf.aspectMode  = AspectRatioFitter.AspectMode.FitInParent;
        arf.aspectRatio = BlobAspectRatio;

        // Blob body image — the full character sprite, no tint (already coloured)
        var bodyGO  = new GameObject("BlobBody");
        bodyGO.transform.SetParent(blobGO.transform, false);
        var bodyImg = bodyGO.AddComponent<Image>();
        bodyImg.color          = Color.white;
        bodyImg.preserveAspect = true;
        bodyImg.raycastTarget  = false;
        bodyImg.type = Image.Type.Sliced;
        //bodyImg.sprite.pixelsPerUnit = 150;
        if (BlobBodySprites != null && charIdx < BlobBodySprites.Length)
            bodyImg.sprite = BlobBodySprites[charIdx];
        var brt2 = bodyGO.GetComponent<RectTransform>();
        brt2.anchorMin = new Vector2(0f, 0f);
        brt2.anchorMax = new Vector2(1f, 0f);
        brt2.pivot = new Vector2(0.5f, 0f);
        brt2.offsetMin = Vector2.zero;
        brt2.offsetMax = Vector2.zero;

        GameObject headGO = null;
        if (BlobHeadObjects != null && charIdx < BlobHeadObjects.Length)
        {
            headGO = Instantiate(BlobHeadObjects[charIdx], blobGO.transform, false);
            var brt3 = headGO.GetComponent<RectTransform>();
            brt3.anchorMin = new Vector2(0f, 0f);
            brt3.anchorMax = new Vector2(1f, 0f);
            brt3.pivot = new Vector2(0.5f, 1f);
            brt3.offsetMin = Vector2.zero;
            brt3.offsetMax = Vector2.zero;
            brt3.localScale = new Vector3(0.9f, 0.9f, 1f);
        }

        blobGO.AddComponent<AudioSource>();   // must exist before CharacterBlobPresenter.Awake()
        var blob = blobGO.AddComponent<CharacterBlobPresenter>();
        blob.CharacterIndex  = charIdx;
        blob.CharacterName   = CharNames[charIdx];
        blob.BlobColor       = color;
        blob.BlobImage  = bodyImg;
        blob.FaceSwitcher = headGO.GetComponent<FaceSwitcher>();
        blob.HeadObj = headGO;
        blob.SwayAmount = 14f;
        blob.BobAmount  = 7f;

        return blob;
    }

    // ── Note-head button (small blob face, pitch selector) ────────────────────

    PitchButtonPresenter BuildNoteHead(Transform parent, int charIdx, int pitchIdx)
    {
        var blobColor = BlobColors[charIdx];

        var go  = new GameObject($"NoteHead_{pitchIdx}");
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.sprite = RoundedRectSprite;
        img.type   = Image.Type.Sliced;
        img.color  = new Color(0.82f, 0.82f, 0.82f);   // light gray background
        var le  = go.AddComponent<LayoutElement>();
        le.flexibleWidth   = 1;
        le.preferredHeight = FaceButtonHeight;   // no minHeight — freely resizable

        // Pitch-level face sprite (FaceLevel0–3, stacked: Yelling at top / Calm at bottom)
        var faceGO  = new GameObject("Face");
        faceGO.transform.SetParent(go.transform, false);
        var faceImg = faceGO.AddComponent<Image>();
        faceImg.color          = Color.white;
        faceImg.preserveAspect = true;
        faceImg.raycastTarget  = false;
        if (FaceSprites != null && pitchIdx < FaceSprites.Length && FaceSprites[pitchIdx] != null)
            faceImg.sprite = FaceSprites[pitchIdx];
        var frt = faceGO.GetComponent<RectTransform>();
        frt.anchorMin = new Vector2(0.5f, 0.5f);
        frt.anchorMax = new Vector2(0.5f, 0.5f);
        frt.pivot     = new Vector2(0.5f, 0.5f);
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;
        var farf = faceGO.AddComponent<AspectRatioFitter>();
        farf.aspectMode  = AspectRatioFitter.AspectMode.FitInParent;
        farf.aspectRatio = FaceAspectRatio;

        // Dim blob colour when not active; full blob colour when active
        var presenter = go.AddComponent<PitchButtonPresenter>();
        presenter.CharacterIndex = charIdx;
        presenter.PitchIndex     = pitchIdx;
        presenter.ButtonImage    = img;
        presenter.NormalColor    = new Color(0.82f, 0.82f, 0.82f);   // light gray when idle
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

    // ── Rounded-rect sprite (generated once, shared by all note-head buttons) ──

    static Sprite _roundedRect;
    static Sprite RoundedRectSprite
    {
        get
        {
            if (_roundedRect != null) return _roundedRect;
            const int sz = 64, r = 14;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[sz * sz];
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                    px[y * sz + x] = InsideRR(x, y, sz, sz, r)
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(0, 0, 0, 0);
            tex.SetPixels32(px);
            tex.Apply();
            _roundedRect = Sprite.Create(tex, new Rect(0, 0, sz, sz),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(r, r, r, r));   // 9-slice border so corners stay round at any size
            return _roundedRect;
        }
    }

    static bool InsideRR(int x, int y, int w, int h, int r)
    {
        if (x >= r && x < w - r) return true;
        if (y >= r && y < h - r) return true;
        int cx = (x < r) ? r : w - r - 1;
        int cy = (y < r) ? r : h - r - 1;
        float dx = x - cx, dy = y - cy;
        return dx * dx + dy * dy <= (float)r * r;
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
        img.sprite = RoundedRectSprite;
        img.type   = Image.Type.Sliced;
        img.color  = bg;

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
