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
    private readonly int[] _currentPitch = { -1, -1, -1, -1 };  // -1 = nothing selected yet
    private bool _isPlaying;

    /// <summary>
    /// True while the training panel is active.
    /// Predictions arriving via LSL are still recorded by TrainingController for
    /// sham feedback, but must NOT change game state (blob pitches / play toggle).
    /// Equivalent to the backend team's blockOutGoingLSL flag in P300Controller.
    /// </summary>
    public bool IsTrainingActive { get; private set; }

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

        RefreshAllButtons();
        ShowMainMenu();
    }

    // ── Navigation ─────────────────────────────────────────────────────────────

    public void ShowMainMenu()
    {
        IsTrainingActive = false;
        StopGame();
        SetPanelActive(MainMenuPanel, true);
        SetPanelActive(GamePanel,     false);
        SetPanelActive(TrainingPanel, false);
    }

    public void StartNewGame()
    {
        IsTrainingActive = false;
        SetPanelActive(MainMenuPanel, false);
        SetPanelActive(GamePanel,     true);
        SetPanelActive(TrainingPanel, false);

        _isPlaying = false;
        UpdatePlayPauseLabel();
        BCIController?.StartContinuousTrials();
    }

    public void StartTraining()
    {
        IsTrainingActive = true;
        SetPanelActive(MainMenuPanel, false);
        SetPanelActive(GamePanel,     false);
        SetPanelActive(TrainingPanel, true);   // full-screen — contains its own stimulus grid
    }

    // ── Play / Pause ───────────────────────────────────────────────────────────

    public void TogglePlay()
    {
        if (IsTrainingActive) return;   // block during training — don't change game state
        if (_isPlaying) PauseGame();
        else            PlayGame();
    }

    // ── Pitch control (called by CharacterSelectionHandler & MockP300Input) ────

    public void SetPitch(int charIndex, int pitchIndex)
    {
        if (IsTrainingActive) return;   // block during training — don't change game state
        if (charIndex  < 0 || charIndex  >= 4) return;
        if (pitchIndex < 0 || pitchIndex >= 4) return;

        _currentPitch[charIndex] = pitchIndex;
        var blob = Blobs[charIndex];
        blob?.SetCurrentPitch(pitchIndex);
        // Only start singing when the blob is visible (panel active).
        // Skips the PlayVoice call during StartNewGame() pitch resets, which
        // run before GamePanel is shown and would crash the coroutine.
        if (blob != null && !blob.IsSinging && blob.gameObject.activeInHierarchy)
            blob.PlayVoice();
        RefreshButtonsForCharacter(charIndex);
    }

    public int GetCurrentPitch(int charIndex) =>
        (charIndex >= 0 && charIndex < 4) ? _currentPitch[charIndex] : 0;

    // ── Private helpers ───────────────────────────────────────────────────────

    private void PlayGame()
    {
        _isPlaying = true;
        foreach (var b in Blobs)
            if (b != null && b.gameObject.activeInHierarchy) b.PlayVoice();
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
            PlayPauseLabel.text = _isPlaying ? "|| PAUSE" : ">  SING";
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
