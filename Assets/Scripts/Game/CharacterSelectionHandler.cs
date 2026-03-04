using BCIEssentials.Selection;
using BCIEssentials.LSLFramework;

/// <summary>
/// Receives P300 predictions and decodes them:
///   index  0-15 → (charIndex, pitchIndex)  for the 4×4 pitch grid
///   index 16    → TogglePlay (17th stimulus = Play/Pause button)
///
/// Encoding for 0-15:  flatIndex = charIndex * 4 + pitchIndex
/// </summary>
public class CharacterSelectionHandler : SelectionBehaviour
{
    public override void OnPrediction(Prediction prediction)
    {
        int flat = prediction.Index;

        if (flat == 16)
        {
            GameFlowController.Instance?.TogglePlay();
            return;
        }

        int charIndex  = flat / 4;
        int pitchIndex = flat % 4;
        GameFlowController.Instance?.SetPitch(charIndex, pitchIndex);
    }
}
