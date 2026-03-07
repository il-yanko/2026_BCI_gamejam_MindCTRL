using UnityEngine;
using UnityEngine.UI;
using BCIEssentials.Stimulus.Presentation;

/// <summary>
/// P300 stimulus wrapper for the Play / Pause button (stimulus index 16).
///
/// Flashes yellow during a P300 trial. When selected by the BCI (or mock
/// keyboard) it calls GameFlowController.TogglePlay(), toggling playback.
/// The button image reflects play state: blue = paused, green = playing.
/// </summary>
public class PlayPauseButtonPresenter : StimulusPresentationBehaviour
{
    [Header("Visuals")]
    public Image ButtonImage;
    public Color PausedColor  = new Color(0.20f, 0.60f, 0.95f);  // blue
    public Color PlayingColor = new Color(0.20f, 0.75f, 0.40f);  // green
    public Color FlashColor   = new Color(1f,    1f,    0.2f,  1f);
    public Color TargetColor  = new Color(0f,    0.8f,  1f,    1f);

    private bool _lastIsPlaying;

    void Awake()
    {
        if (ButtonImage == null) ButtonImage = GetComponent<Image>();
        Refresh();
    }

    void Update()
    {
        bool isPlaying = GameFlowController.Instance?.IsPlaying ?? false;
        if (isPlaying != _lastIsPlaying) { _lastIsPlaying = isPlaying; Refresh(); }
    }

    // ── StimulusPresentationBehaviour ─────────────────────────────────────────

    public event System.Action OnDisplayStarted;
    public event System.Action OnDisplayEnded;

    public override void StartStimulusDisplay()
    {
        if (ButtonImage != null) ButtonImage.color = FlashColor;
        OnDisplayStarted?.Invoke();
    }

    public override void EndStimulusDisplay()
    {
        OnDisplayEnded?.Invoke();
        Refresh();
    }

    public override void Select()
    {
        GameFlowController.Instance?.TogglePlay();
    }

    public override void StartTargetIndication()
    {
        if (ButtonImage != null) ButtonImage.color = TargetColor;
    }

    public override void EndTargetIndication() => Refresh();

    // ── Helper ────────────────────────────────────────────────────────────────

    public void Refresh()
    {
        if (ButtonImage == null) return;
        bool isPlaying = GameFlowController.Instance?.IsPlaying ?? false;
        _lastIsPlaying = isPlaying;
        ButtonImage.color = isPlaying ? PlayingColor : PausedColor;
    }
}
