using System.Collections;
using UnityEngine;
using BCIEssentials.Behaviours.Trials.P300;
using BCIEssentials.Stimulus.Collections;
using BCIEssentials.LSLFramework;

/// <summary>
/// Orchestrates the P300 BCI trial loop for the 4×4 pitch-button grid.
///
/// All 16 PitchButtonPresenters are registered as P300 stimuli.
/// Prediction index 0-15:  charIndex = index / 4,  pitchIndex = index % 4.
///
/// In mock mode (GameConfig.useMockBCI) the ResponseProvider is NOT started.
/// Call MockPrediction(flatIndex) to simulate a classifier result.
/// </summary>
public class MindCTRLBCIController : MonoBehaviour
{
    [Header("P300 stimuli — 16 pitch buttons (4 chars × 4 pitches, row-major)")]
    public PitchButtonPresenter[]    Presenters        = new PitchButtonPresenter[16];
    /// <summary>17th stimulus — the Play / Pause button (index 16 in predictions).</summary>
    public PlayPauseButtonPresenter  PlayPausePresenter;
    public CharacterSelectionHandler SelectionHandler;

    [Header("Trial parameters")]
    public int   FlashesPerOption   = 10;
    public float OnTime             = 0.1f;
    public float OffTime            = 0.075f;
    public float TrialPauseDuration = 0.5f;

    // Framework components (created at runtime)
    private MarkerWriter               _markerWriter;
    private ResponseProvider           _responseProvider;
    private SingleFlashTrialBehaviour  _trial;
    private StimulusPresenterCollection _presenterCollection;

    private Coroutine _continuousRoutine;

    // ── Unity lifecycle ────────────────────────────────────────────────────────

    void Start()
    {
        _markerWriter        = GetOrAdd<MarkerWriter>();
        _responseProvider    = GetOrAdd<ResponseProvider>();
        _trial               = GetOrAdd<SingleFlashTrialBehaviour>();
        _presenterCollection = GetOrAdd<RuntimePresenterCollection>();

        _trial.FlashesPerOption    = FlashesPerOption;
        _trial.OnTime              = OnTime;
        _trial.OffTime             = OffTime;
        _trial.MarkerWriter        = _markerWriter;
        _trial.PresenterCollection = _presenterCollection;

        foreach (var p in Presenters)
            if (p != null) _presenterCollection.Add(p);
        if (PlayPausePresenter != null) _presenterCollection.Add(PlayPausePresenter);

        if (!GameConfig.Instance.useMockBCI)
            _responseProvider.SubscribePredictions(SelectionHandler.OnPrediction);
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public void StartContinuousTrials()
    {
        if (_continuousRoutine != null) return;
        _continuousRoutine = StartCoroutine(RunContinuous());
    }

    public void StopContinuousTrials()
    {
        if (_continuousRoutine != null)
        {
            StopCoroutine(_continuousRoutine);
            _continuousRoutine = null;
        }
        if (_trial != null && _trial.IsRunning) _trial.Interrupt();
    }

    /// <summary>
    /// Simulate a classifier result (mock / keyboard mode).
    /// flatIndex 0-15: charIndex = flatIndex/4, pitchIndex = flatIndex%4.
    /// </summary>
    public void MockPrediction(int flatIndex)
    {
        int count = PlayPausePresenter != null ? 17 : 16;
        string probs = BuildProbabilities(flatIndex, count);
        Prediction pred = Prediction.ParseValues(
            new string[] { (flatIndex + 1).ToString(), probs });
        SelectionHandler.OnPrediction(pred);
    }

    /// <summary>Run one training trial with a known target index (0-16).</summary>
    public IEnumerator RunTrainingTrial(int targetIndex)
    {
        _trial.StartTrainingTrial(targetIndex);
        yield return _trial.AwaitCompletion();
        yield return new WaitForSeconds(TrialPauseDuration);
    }

    /// <summary>Run one testing trial (no known target) and wait for completion.</summary>
    public IEnumerator RunTestingTrialCoroutine()
    {
        _trial.StartTestingTrial();
        yield return _trial.AwaitCompletion();
        yield return new WaitForSeconds(TrialPauseDuration);
    }

    // ── Private ────────────────────────────────────────────────────────────────

    private IEnumerator RunContinuous()
    {
        while (true)
        {
            if (!_trial.IsRunning) _trial.StartTestingTrial();
            yield return _trial.AwaitCompletion();
            yield return new WaitForSeconds(TrialPauseDuration);
        }
    }

    private T GetOrAdd<T>() where T : UnityEngine.Component =>
        GetComponent<T>() ?? gameObject.AddComponent<T>();

    private static string BuildProbabilities(int winner, int count)
    {
        var parts = new string[count];
        for (int i = 0; i < count; i++) parts[i] = (i == winner) ? "1.0" : "0.0";
        return string.Join(" ", parts);
    }
}
