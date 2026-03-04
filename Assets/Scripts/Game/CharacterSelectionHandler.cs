using BCIEssentials.Selection;
using BCIEssentials.LSLFramework;

/// <summary>
/// Receives P300 predictions (index 0-15) and decodes them into a
/// (character, pitch) pair for the 4×4 grid.
///
/// Encoding:  flatIndex = charIndex * 4 + pitchIndex
///            charIndex  = flatIndex / 4
///            pitchIndex = flatIndex % 4
/// </summary>
public class CharacterSelectionHandler : SelectionBehaviour
{
    public override void OnPrediction(Prediction prediction)
    {
        int flat       = prediction.Index;   // 0-15
        int charIndex  = flat / 4;
        int pitchIndex = flat % 4;
        GameFlowController.Instance?.SetPitch(charIndex, pitchIndex);
    }
}
