using UnityEngine;

/// <summary>
/// Handles button clicks on the Main Menu canvas panel.
/// Wire each button's OnClick event to these methods in the Inspector.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public void OnNewGame()  => GameFlowController.Instance?.StartNewGame();
    public void OnTraining() => GameFlowController.Instance?.StartTraining();
    public void OnQuit()     => Application.Quit();
}
