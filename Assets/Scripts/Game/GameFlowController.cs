using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Central game-state machine.
///
/// States:  MainMenu  →  Game (New Game)
///                    →  Training
///
/// In Game state:
///   • All 4 blobs start at pitch level 2 (index 1 = "Happy" / neutral).
///   • TogglePlay() → all blobs sing their current pitches simultaneously (live orchestration).
///   • SetPitch(c, p) → updates blob face + audio immediately (even while singing).
///   • All 16 pitch buttons are always active P300 stimuli.
///
/// Assign fields in the Inspector.
/// </summary>
public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    [Header("Characters (4 blob display widgets)")]
    public CharacterBlobPresenter[] Blobs = new CharacterBlobPresenter[4];

    [Header("Pitch buttons (16 = 4 chars × 4 pitches, row-major)")]
    /// flatIndex = charIndex * 4 + pitchIndex
    public PitchButtonPresenter[] PitchButtons = new PitchButtonPresenter[16];

    [Header("BCI controller")]
    public MindCTRLBCIController BCIController;

    [Header("UI Panels (assign in Inspector)")]
    public GameObject MainMenuPanel;
    public GameObject GamePanel;
    public GameObject TrainingPanel;

    [Header("Play / Pause button (assign in Inspector)")]
    public Button PlayPauseButton;
    public Text   PlayPauseLabel;

    // ── Runtime state ─────────────────────────────────────────────────────────
    private readonly int[] _currentPitch = { 1, 1, 1, 1 };  // neutral start
    private bool _isPlaying;

    public bool IsPlaying => _isPlaying;

    // ── Unity lifecycle ────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Ensure CharacterIndex matches slot order
        for (int i = 0; i < Blobs.Length; i++)
            if (Blobs[i] != null) Blobs[i].CharacterIndex = i;

        // Apply starting pitches
        for (int c = 0; c < 4; c++)
            Blobs[c]?.SetCurrentPitch(_currentPitch[c]);

        RefreshAllButtons();
        ShowMainMenu();
    }

    // ── Navigation ─────────────────────────────────────────────────────────────

    public void ShowMainMenu()
    {
        StopGame();
        SetPanelActive(MainMenuPanel, true);
        SetPanelActive(GamePanel,     false);
        SetPanelActive(TrainingPanel, false);
    }

    public void StartNewGame()
    {
        // Reset every blob to pitch 1 (neutral)
        for (int c = 0; c < 4; c++) SetPitch(c, 1);

        SetPanelActive(MainMenuPanel, false);
        SetPanelActive(GamePanel,     true);
        SetPanelActive(TrainingPanel, false);

        _isPlaying = false;
        UpdatePlayPauseLabel();
        BCIController?.StartContinuousTrials();
    }

    public void StartTraining()
    {
        SetPanelActive(MainMenuPanel, false);
        SetPanelActive(GamePanel,     false);
        SetPanelActive(TrainingPanel, true);
        // Training logic TBD
    }

    // ── Play / Pause ───────────────────────────────────────────────────────────

    public void TogglePlay()
    {
        if (_isPlaying) PauseGame();
        else            PlayGame();
    }

    // ── Pitch control (called by CharacterSelectionHandler & MockP300Input) ────

    public void SetPitch(int charIndex, int pitchIndex)
    {
        if (charIndex  < 0 || charIndex  >= 4) return;
        if (pitchIndex < 0 || pitchIndex >= 4) return;

        _currentPitch[charIndex] = pitchIndex;
        var blob = Blobs[charIndex];
        blob?.SetCurrentPitch(pitchIndex);
        // If the blob isn't already singing, clicking its pitch head starts it.
        // If it is singing (global play active), SetCurrentPitch already switched the note.
        if (blob != null && !blob.IsSinging)
            blob.PlayVoice();
        RefreshButtonsForCharacter(charIndex);
    }

    public int GetCurrentPitch(int charIndex) =>
        (charIndex >= 0 && charIndex < 4) ? _currentPitch[charIndex] : 0;

    // ── Private helpers ───────────────────────────────────────────────────────

    private void PlayGame()
    {
        _isPlaying = true;
        foreach (var b in Blobs) b?.PlayVoice();
        UpdatePlayPauseLabel();
    }

    private void PauseGame()
    {
        _isPlaying = false;
        foreach (var b in Blobs) b?.PauseVoice();
        UpdatePlayPauseLabel();
    }

    private void StopGame()
    {
        _isPlaying = false;
        BCIController?.StopContinuousTrials();
        foreach (var b in Blobs) b?.StopVoice();
        UpdatePlayPauseLabel();
    }

    private void UpdatePlayPauseLabel()
    {
        if (PlayPauseLabel != null)
            PlayPauseLabel.text = _isPlaying ? "|| PAUSE" : ">  PLAY";
    }

    private void RefreshAllButtons()
    {
        foreach (var btn in PitchButtons) btn?.Refresh();
    }

    private void RefreshButtonsForCharacter(int charIndex)
    {
        foreach (var btn in PitchButtons)
            if (btn != null && btn.CharacterIndex == charIndex) btn.Refresh();
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }
}
