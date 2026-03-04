using UnityEngine;

/// <summary>
/// Keyboard mock for the 4×4 pitch-selection grid.
/// Only active when GameConfig.useMockBCI == true.
///
/// Key layout (mirrors the on-screen grid columns):
///   Char 0 (Red):    1  2  3  4    (pitches 0-3)
///   Char 1 (Blue):   Q  W  E  R
///   Char 2 (Yellow): A  S  D  F
///   Char 3 (Green):  Z  X  C  V
///
///   Space  — toggle Play / Pause
///   Escape — return to Main Menu
/// </summary>
[RequireComponent(typeof(MindCTRLBCIController))]
public class MockP300Input : MonoBehaviour
{
    private MindCTRLBCIController _bci;

    void Awake() => _bci = GetComponent<MindCTRLBCIController>();

    void Update()
    {
        if (GameConfig.Instance == null || !GameConfig.Instance.useMockBCI) return;

        if (Input.GetKeyDown(KeyCode.Space))  { GameFlowController.Instance?.TogglePlay();    return; }
        if (Input.GetKeyDown(KeyCode.Escape)) { GameFlowController.Instance?.ShowMainMenu();  return; }

        // Char 0 (Red) ── keys 1 2 3 4
        if (Input.GetKeyDown(KeyCode.Alpha1)) _bci.MockPrediction(0 * 4 + 0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) _bci.MockPrediction(0 * 4 + 1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) _bci.MockPrediction(0 * 4 + 2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) _bci.MockPrediction(0 * 4 + 3);

        // Char 1 (Blue) ── Q W E R
        if (Input.GetKeyDown(KeyCode.Q)) _bci.MockPrediction(1 * 4 + 0);
        if (Input.GetKeyDown(KeyCode.W)) _bci.MockPrediction(1 * 4 + 1);
        if (Input.GetKeyDown(KeyCode.E)) _bci.MockPrediction(1 * 4 + 2);
        if (Input.GetKeyDown(KeyCode.R)) _bci.MockPrediction(1 * 4 + 3);

        // Char 2 (Yellow) ── A S D F
        if (Input.GetKeyDown(KeyCode.A)) _bci.MockPrediction(2 * 4 + 0);
        if (Input.GetKeyDown(KeyCode.S)) _bci.MockPrediction(2 * 4 + 1);
        if (Input.GetKeyDown(KeyCode.D)) _bci.MockPrediction(2 * 4 + 2);
        if (Input.GetKeyDown(KeyCode.F)) _bci.MockPrediction(2 * 4 + 3);

        // Char 3 (Green) ── Z X C V
        if (Input.GetKeyDown(KeyCode.Z)) _bci.MockPrediction(3 * 4 + 0);
        if (Input.GetKeyDown(KeyCode.X)) _bci.MockPrediction(3 * 4 + 1);
        if (Input.GetKeyDown(KeyCode.C)) _bci.MockPrediction(3 * 4 + 2);
        if (Input.GetKeyDown(KeyCode.V)) _bci.MockPrediction(3 * 4 + 3);
    }
}
