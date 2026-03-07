using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
    public AudioClip mainMenuBackgroundMusic;
    public AudioClip newGameStartMusic;

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

    [Header("Art — Training Dummy Sprites")]
    public Sprite TrainingDummyBodyFace;

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

    private Material _mainMenuMaterial;
    private GameObject _mainMenuPanel;
    private GameObject _curtainObject;

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

        _mainMenuPanel = BuildMainMenu(canvas.transform);

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
        var settingsPanel = BuildSettingsPanel(canvas.transform);

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
        flow.MainMenuPanel   = _mainMenuPanel;
        flow.GamePanel       = gamePanel;
        flow.TrainingPanel   = trainingPanel;
        flow.SettingsPanel   = settingsPanel;
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

        StartMainMenuMusic();
    }

    // ── BCI / logic system ────────────────────────────────────────────────────

    void BuildBCISystem(
        out GameFlowController      flow,
        out MindCTRLBCIController   bci,
        out CharacterSelectionHandler handler)
    {
        var go = new GameObject("BCISystem");
        if (FindAnyObjectByType<AudioListener>() == null)
            go.AddComponent<AudioListener>();   // exactly one listener in the scene
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
        _mainMenuMaterial = new Material(Shader.Find("UI/Default"));
        _mainMenuMaterial.name = "MainMenuMaterial";

        var panel = new GameObject("MainMenu", typeof(RectTransform));
        panel.transform.SetParent(root, false);
        Stretch(panel);

        var curtains = new GameObject("Curtains");
        curtains.transform.SetParent(panel.transform, false);
        var curtainImg = curtains.AddComponent<Image>();
        if (CurtainSprite != null)
            curtainImg.sprite = CurtainSprite;
        else
            curtainImg.color = Color.clear;
        Stretch(curtains);
        var arFitter = curtains.AddComponent<AspectRatioFitter>();
        arFitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
        arFitter.aspectRatio = 2;
        arFitter.transform.localPosition = new Vector2(0f, 125f);
        _curtainObject = curtains;

        // Logo — constrained to 900 px wide, aspect-correct height
        var logo = new GameObject("Logo");
        logo.transform.SetParent(panel.transform, false);
        var logoImg = logo.AddComponent<Image>();  // Image adds the RectTransform
        logoImg.preserveAspect = true;
        logoImg.raycastTarget  = false;
        if (LogoSprite != null)
            logoImg.sprite = LogoSprite;
        logoImg.material = _mainMenuMaterial;
        var logoRT = logo.GetComponent<RectTransform>();
        logoRT.anchorMin        = new Vector2(0.5f, 0.5f);
        logoRT.anchorMax        = new Vector2(0.5f, 0.5f);
        logoRT.pivot            = new Vector2(0.5f, 0.5f);
        logoRT.sizeDelta        = new Vector2(900f, 300f);
        logoRT.anchoredPosition = new Vector2(0f, 165f);
        var logoArf = logo.AddComponent<AspectRatioFitter>();
        logoArf.aspectMode  = AspectRatioFitter.AspectMode.WidthControlsHeight;
        logoArf.aspectRatio = LogoSprite != null
            ? (float)LogoSprite.texture.width / LogoSprite.texture.height
            : 3f;

        var buttonPanel = new GameObject("MainMenuButtons", typeof(RectTransform));
        buttonPanel.transform.SetParent(panel.transform, false);
        Stretch(buttonPanel);
        buttonPanel.transform.localPosition = new Vector2(0f, -200f);
        var vl = buttonPanel.AddComponent<HorizontalLayoutGroup>();
        vl.childAlignment       = TextAnchor.MiddleCenter;
        vl.spacing              = 24;
        vl.padding              = new RectOffset(40, 40, 0, 0);
        vl.childForceExpandWidth  = false;
        vl.childForceExpandHeight = false;

        var btnBg   = new Color(0.97f, 0.78f, 0.88f);   // lighter pink
        var btnText = new Color(0.28f, 0.08f, 0.42f);   // dark purple

        MakeButton(buttonPanel.transform, "NewGameBtn", "NEW GAME",
            btnBg, 34, 340, 76,
            () => StartCoroutine(StartGameAnimation()), FontStyle.Bold, btnText, _mainMenuMaterial);

        MakeButton(buttonPanel.transform, "TrainingBtn", "TRAINING",
            btnBg, 34, 340, 76,
            () => GameFlowController.Instance?.StartTraining(), FontStyle.Bold, btnText, _mainMenuMaterial);

        MakeButton(buttonPanel.transform, "SettingsBtn", "SETTINGS",
            btnBg, 34, 340, 76,
            () => GameFlowController.Instance?.ShowSettings(), FontStyle.Bold, btnText, _mainMenuMaterial);

        var audioPlayer = panel.AddComponent<AudioSource>();
        audioPlayer.clip = mainMenuBackgroundMusic;

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
        var panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = new Vector2(150f,  60f);   // left=50, bottom=60
        panelRT.offsetMax = new Vector2(-100f, 0f);   // right=100, top=0

        var vl = panel.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment       = TextAnchor.UpperCenter;
        vl.spacing              = 8;
        vl.padding              = new RectOffset(20, 20, 4, 18);
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;

        // Header
        MakeText(panel.transform, "Header", "OPERA  OF  BLOBS",
            38, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold,
            prefH: 44, flexW: true);

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

        var cellNormal = new Color(0.463f, 0.392f, 0.498f, 0.75f);  // #76647f

        var gridCells = new UnityEngine.UI.Image[16];

        // ── Root wrapper — full-screen; flow.TrainingPanel points here ─────────
        var wrapper = MakeContainer(root, "TrainingPanel");
        Stretch(wrapper);
        wrapper.SetActive(false);

        // Semi-transparent overlay — lets the canvas-level Background sprite show through
        {
            var bgGO  = new GameObject("Background");
            bgGO.transform.SetParent(wrapper.transform, false);
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color         = new Color(0.04f, 0.04f, 0.14f, 0.55f);
            bgImg.raycastTarget = false;
            Stretch(bgGO);
        }

        // Content layout
        var panel = MakeContainer(wrapper.transform, "TrainingContent");
        Stretch(panel);
        var vl = panel.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment         = TextAnchor.UpperCenter;
        vl.spacing                = 8;
        vl.padding                = new RectOffset(20, 20, 14, 120);
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;

        // Header
        MakeText(panel.transform, "Header", "TRAINING MODE",
            36, new Color(0.65f, 0.75f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold,
            prefH: 46, flexW: true);

        MakeSeparator(panel.transform);

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
        cgHl.spacing                = 32;
        cgHl.childForceExpandWidth  = false;
        cgHl.childForceExpandHeight = true;

        for (int c = 0; c < 4; c++)
        {
            var col = MakeContainer(charGrid.transform, $"Col_{CharNames[c]}");
            col.AddComponent<LayoutElement>().flexibleWidth = 1;
            var colVl = col.AddComponent<VerticalLayoutGroup>();
            colVl.childAlignment         = TextAnchor.MiddleCenter;
            colVl.spacing                = 48;
            colVl.childForceExpandWidth  = false;
            colVl.childForceExpandHeight = false;

            // 4 pitch cells — top = Yelling (p=3), bottom = Calm (p=0), matching game panel order
            for (int p = 3; p >= 0; p--)
            {
                int flatIdx = c * 4 + p;
                var cellGO  = new GameObject($"Cell_{flatIdx}");
                cellGO.transform.SetParent(col.transform, false);

                // Rounded-rect background — same style as game face buttons
                var cellImg = cellGO.AddComponent<Image>();
                cellImg.sprite        = RoundedRectSprite;
                cellImg.type          = Image.Type.Sliced;
                cellImg.color         = cellNormal;
                cellImg.raycastTarget = false;
                var cellLE = cellGO.AddComponent<LayoutElement>();
                cellLE.preferredWidth  = 120f;
                cellLE.preferredHeight = 120f;

                // Face sprite child — same as game NoteHead
                var faceGO  = new GameObject("Face");
                faceGO.transform.SetParent(cellGO.transform, false);
                var faceImg = faceGO.AddComponent<Image>();
                faceImg.color          = Color.white;
                faceImg.preserveAspect = true;
                faceImg.raycastTarget  = false;
                if (FaceSprites != null && p < FaceSprites.Length && FaceSprites[p] != null)
                    faceImg.sprite = FaceSprites[p];
                var frt = faceGO.GetComponent<RectTransform>();
                frt.anchorMin = new Vector2(0.5f, 0.5f);
                frt.anchorMax = new Vector2(0.5f, 0.5f);
                frt.pivot     = new Vector2(0.5f, 0.5f);
                frt.offsetMin = Vector2.zero;
                frt.offsetMax = Vector2.zero;
                var farf = faceGO.AddComponent<AspectRatioFitter>();
                farf.aspectMode  = AspectRatioFitter.AspectMode.FitInParent;
                farf.aspectRatio = FaceAspectRatio;

                gridCells[flatIdx] = cellImg;
            }

            // TD_Body_Face divider after each column (4 total)
            {
                var divGO  = new GameObject($"BodyDivider_{c}");
                divGO.transform.SetParent(charGrid.transform, false);
                var divImg = divGO.AddComponent<Image>();
                divImg.sprite         = TrainingDummyBodyFace;
                divImg.preserveAspect = true;
                divImg.raycastTarget  = false;
                var divLE = divGO.AddComponent<LayoutElement>();
                divLE.flexibleWidth  = 1f;
                divLE.flexibleHeight = 1f;
            }
        }

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

        // Foreground layer (audience silhouettes) — rendered above training grid, pointer-transparent
        if (ForegroundSprite != null)
        {
            var fgGO  = new GameObject("Foreground");
            fgGO.transform.SetParent(wrapper.transform, false);
            var fgImg = fgGO.AddComponent<Image>();
            fgImg.sprite         = ForegroundSprite;
            fgImg.raycastTarget  = false;
            fgImg.preserveAspect = false;
            Stretch(fgGO);
        }

        // Return wrapper (not panel) so flow.TrainingPanel.SetActive() shows/hides the full overlay
        return wrapper;
    }

    // ── Settings Panel ────────────────────────────────────────────────────────

    GameObject BuildSettingsPanel(Transform root)
    {
        var wrapper = MakeContainer(root, "SettingsPanel");
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

        var panel = MakeContainer(wrapper.transform, "SettingsContent");
        Stretch(panel);
        var vl = panel.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment         = TextAnchor.UpperCenter;
        vl.spacing                = 20;
        vl.padding                = new RectOffset(80, 80, 50, 50);
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;

        // Header
        MakeText(panel.transform, "Header", "SETTINGS",
            40, new Color(0.98f, 0.78f, 0.88f), TextAnchor.MiddleCenter, FontStyle.Bold,
            prefH: 56, flexW: true);

        MakeSeparator(panel.transform);

        // ── Volume row ────────────────────────────────────────────────────────
        var volRow = MakeContainer(panel.transform, "VolumeRow");
        volRow.AddComponent<LayoutElement>().preferredHeight = 56;
        var hl = volRow.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment         = TextAnchor.MiddleLeft;
        hl.spacing                = 28;
        hl.padding                = new RectOffset(24, 24, 0, 0);
        hl.childForceExpandWidth  = false;
        hl.childForceExpandHeight = false;

        MakeText(volRow.transform, "VolumeLabel", "MASTER VOLUME",
            22, Color.white, TextAnchor.MiddleLeft,
            prefW: 250, prefH: 36);

        // Slider GO — Image gives it a RectTransform, acts as the track
        var sliderGO = new GameObject("VolumeSlider");
        sliderGO.transform.SetParent(volRow.transform, false);
        var trackImg = sliderGO.AddComponent<Image>();
        trackImg.color = new Color(0.22f, 0.22f, 0.32f);
        var sliderLE = sliderGO.AddComponent<LayoutElement>();
        sliderLE.preferredWidth  = 440;
        sliderLE.preferredHeight = 36;

        // Fill Area
        var fillArea   = MakeContainer(sliderGO.transform, "Fill Area");
        var fillAreaRT = (RectTransform)fillArea.transform;
        fillAreaRT.anchorMin = new Vector2(0f, 0.2f);
        fillAreaRT.anchorMax = new Vector2(1f, 0.8f);
        fillAreaRT.offsetMin = new Vector2(4f, 0f);
        fillAreaRT.offsetMax = new Vector2(-4f, 0f);

        var fillGO  = new GameObject("Fill");
        fillGO.transform.SetParent(fillArea.transform, false);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0.92f, 0.55f, 0.72f);
        var fillRT  = (RectTransform)fillGO.transform;
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        // Handle Slide Area
        var handleArea   = MakeContainer(sliderGO.transform, "Handle Slide Area");
        var handleAreaRT = (RectTransform)handleArea.transform;
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.offsetMin = new Vector2(8f, 0f);
        handleAreaRT.offsetMax = new Vector2(-8f, 0f);

        var handleGO  = new GameObject("Handle");
        handleGO.transform.SetParent(handleArea.transform, false);
        var handleImg = handleGO.AddComponent<Image>();
        handleImg.color = Color.white;
        var handleRT  = (RectTransform)handleGO.transform;
        handleRT.anchorMin       = new Vector2(0f, 0.5f);
        handleRT.anchorMax       = new Vector2(0f, 0.5f);
        handleRT.pivot           = new Vector2(0.5f, 0.5f);
        handleRT.sizeDelta       = new Vector2(28f, 28f);
        handleRT.anchoredPosition = Vector2.zero;

        // Wire Slider component
        var slider = sliderGO.AddComponent<Slider>();
        slider.fillRect     = fillRT;
        slider.handleRect   = handleRT;
        slider.targetGraphic = handleImg;
        slider.direction    = Slider.Direction.LeftToRight;
        slider.minValue     = 0f;
        slider.maxValue     = 1f;
        slider.value        = GameConfig.Instance != null ? GameConfig.Instance.masterVolume : 1f;
        slider.onValueChanged.AddListener(v =>
        {
            if (GameConfig.Instance != null) GameConfig.Instance.SetMasterVolume(v);
        });

        MakeSeparator(panel.transform);

        // Back button
        var backBar = MakeContainer(panel.transform, "BackBar");
        backBar.AddComponent<LayoutElement>().preferredHeight = 60;
        var bhl = backBar.AddComponent<HorizontalLayoutGroup>();
        bhl.childAlignment         = TextAnchor.MiddleCenter;
        bhl.childForceExpandWidth  = false;
        bhl.childForceExpandHeight = false;
        MakeButton(backBar.transform, "BackBtn", "< MAIN MENU",
            new Color(0.92f, 0.55f, 0.72f), 26, 280, 56,
            () => GameFlowController.Instance?.ShowMainMenu(), FontStyle.Bold);

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

        // ── Main area: blob fills full width; note-stack overlaid on left ────────
        var mainArea = MakeContainer(col.transform, "MainArea");
        var mainLe = mainArea.AddComponent<LayoutElement>();
        mainLe.flexibleWidth  = 1;
        mainLe.flexibleHeight = 1;
        // No layout group — children use absolute RectTransform positioning.

        // Blob fills the entire main area ─────────────────────────────────────
        var blobBox = MakeContainer(mainArea.transform, "BlobBox");
        Stretch(blobBox);   // fills mainArea fully; no longer shares row with NoteStack

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
        if (BlobHeadObjects != null && charIdx < BlobHeadObjects.Length && BlobHeadObjects[charIdx] != null)
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
        blob.FaceSwitcher = headGO?.GetComponent<FaceSwitcher>();
        blob.HeadObj = headGO;
        blob.SwayAmount = 14f;
        blob.BobAmount  = 7f;

        // Note-head stack — created AFTER blob so it renders on top ───────────
        // Anchored to the left edge of mainArea as an overlay (does not affect blob width).
        var noteStack = MakeContainer(mainArea.transform, "NoteStack");
        var nsRT = noteStack.GetComponent<RectTransform>();
        nsRT.anchorMin = new Vector2(0f, 0f);
        nsRT.anchorMax = new Vector2(0f, 1f);   // full height, pinned to left edge
        nsRT.offsetMin = new Vector2(0f,   0f);   // X=0, Bottom=0
        nsRT.offsetMax = new Vector2(120f, 20f); // Width=120, Top=20
        var nsVl = noteStack.AddComponent<VerticalLayoutGroup>();
        nsVl.childAlignment         = TextAnchor.UpperCenter;
        nsVl.padding                = new RectOffset(0, 0, FaceStackTopPad, 0);
        nsVl.spacing                = 48f;
        nsVl.childForceExpandWidth  = true;
        nsVl.childForceExpandHeight = false;
        var nsLe = noteStack.AddComponent<LayoutElement>();
        nsLe.preferredWidth = 120f;  // used by NoteStackResizeHandle
        nsLe.ignoreLayout   = true;  // excluded from mainArea — blob is unaffected
        _noteStackElements.Add(nsLe);

        var handleGO = new GameObject("ResizeHandle");
        handleGO.transform.SetParent(noteStack.transform, false);
        handleGO.AddComponent<Image>();
        handleGO.AddComponent<LayoutElement>().ignoreLayout = true;
        var handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.anchorMin = new Vector2(1f, 0f);
        handleRT.anchorMax = Vector2.one;
        handleRT.offsetMin = new Vector2(-8f, 0f);
        handleRT.offsetMax = Vector2.zero;
        handleGO.AddComponent<NoteStackResizeHandle>().NoteStacks = _noteStackElements;

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

        var faceNormal = new Color(0.463f, 0.392f, 0.498f, 1f);  // #76647f

        var img = go.AddComponent<Image>();
        img.sprite = RoundedRectSprite;
        img.type   = Image.Type.Sliced;
        img.color  = faceNormal;
        var le  = go.AddComponent<LayoutElement>();
        le.flexibleWidth   = 1;
        le.preferredHeight = 120f;   // match training cell size

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
        presenter.NormalColor    = faceNormal;
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

    private IEnumerator StartGameAnimation()
    {
        StopMainMenuMusic();

        var mainColor = _mainMenuMaterial.color;
        float duration = 1.5f;
        float elapsed  = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            _mainMenuMaterial.color = new Color(mainColor.r, mainColor.g, mainColor.b, 1-Mathf.Log10(Mathf.Lerp(1f, 10f, t)));
            yield return null;
        }
        _mainMenuMaterial.color = new Color(mainColor.r, mainColor.g, mainColor.b, 0);

        var startPos = _curtainObject.transform.localPosition;
        var finalPos = new Vector3(0f, 1100f, 0f);
        duration = 3f;
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            _curtainObject.transform.localPosition = Vector2.Lerp(startPos, finalPos, t);//new Color(mainColor.r, mainColor.g, mainColor.b, 1-Mathf.Log10(Mathf.Lerp(1f, 10f, t)));
            print($"curtain: {_curtainObject.transform.localPosition}");
            yield return null;
        }

        _mainMenuPanel.SetActive(false);
        _curtainObject.transform.localPosition = startPos;
        _mainMenuMaterial.color = new Color(mainColor.r, mainColor.g, mainColor.b, 1);
        GameFlowController.Instance?.StartNewGame();
    }

    private void StartMainMenuMusic()
    {
        _mainMenuPanel.GetComponent<AudioSource>().Play();
    }

    private void StopMainMenuMusic()
    {
        StartCoroutine(StopMainMenuMusicImpl());
    }

    private IEnumerator StopMainMenuMusicImpl()
    {
        var transitionObj = new GameObject();
        transitionObj.transform.SetParent(_mainMenuPanel.transform, false);
        var transitionSource = transitionObj.AddComponent<AudioSource>();
        transitionSource.clip = newGameStartMusic;
        transitionSource.Play();

        var audioSource = _mainMenuPanel.GetComponent<AudioSource>();
        var duration = 3f;
        var elapsed = 0f;
        var startVolume = audioSource.volume;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }
        audioSource.Stop();
        audioSource.volume = startVolume;
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
        FontStyle style = FontStyle.Normal,
        Color labelColor = default, Material material = null)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.sprite = RoundedRectSprite;
        img.type   = Image.Type.Sliced;
        img.color  = bg;
        img.material = material;

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
        lbl.color     = labelColor == default(Color) ? Color.white : labelColor;
        lbl.material  = material;
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
