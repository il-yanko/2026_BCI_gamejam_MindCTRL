using UnityEngine;
using UnityEngine.UI;
using BCIEssentials.Stimulus.Presentation;

/// <summary>
/// One cell in the 4×4 pitch-selection grid.
/// Extends StimulusPresentationBehaviour so the framework can flash it as a P300 stimulus.
///
/// Index encoding (used by MindCTRLBCIController):
///   flatIndex = CharacterIndex * 4 + PitchIndex
///
/// When selected by the BCI (or mock keyboard), it tells GameFlowController
/// to set the pitch of its character.
/// </summary>
public class PitchButtonPresenter : StimulusPresentationBehaviour
{
    [Header("Identity")]
    public int CharacterIndex;  // 0-3
    public int PitchIndex;      // 0-3  (0=Calm, 1=Happy, 2=Excited, 3=Yelling)

    [Header("Visuals")]
    public Image ButtonImage;
    public Color NormalColor   = new Color(0.55f, 0.55f, 0.55f, 1f);
    public Color FlashColor    = new Color(1f,    1f,    0.2f,  1f);
    public Color ActiveColor   = new Color(0.3f,  1f,    0.3f,  1f);
    public Color TargetColor   = new Color(0f,    0.8f,  1f,    1f);

    void Awake()
    {
        if (ButtonImage == null) ButtonImage = GetComponent<Image>();
        Refresh();
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
        GameFlowController.Instance?.SetPitch(CharacterIndex, PitchIndex);
    }

    public override void StartTargetIndication()
    {
        if (ButtonImage != null) ButtonImage.color = TargetColor;
    }

    public override void EndTargetIndication() => Refresh();

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// Colours the button based on whether it is the currently active pitch
    /// for its character.
    public void Refresh()
    {
        if (ButtonImage == null) return;
        bool isActive = GameFlowController.Instance != null
                     && GameFlowController.Instance.GetCurrentPitch(CharacterIndex) == PitchIndex;
        ButtonImage.color = isActive ? ActiveColor : NormalColor;
    }
}
