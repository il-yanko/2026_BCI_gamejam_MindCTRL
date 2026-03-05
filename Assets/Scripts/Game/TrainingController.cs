using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Orchestrates P300 training and accuracy evaluation for all 17 stimuli.
///
/// Training:   cycles through all 17 stimuli in random order, N times each,
///             calling StartTrainingTrial() so the Python backend accumulates
///             labelled epochs and trains the classifier.
///
/// Evaluation: runs one-shot testing trials with a cued target and compares
///             the classifier's prediction to the ground truth.
///
/// Mock mode:  works without a real headset — MockPrediction() is called so
///             you can verify the Unity → prediction → game-state path.
/// </summary>
public class TrainingController : MonoBehaviour
{
    [Header("UI — assign from SceneBootstrapper")]
    public Text   CueLabel;
    public Text   ProgressLabel;
    public Text   ResultLabel;
    public Button StartTrainBtn;
    public Button StartEvalBtn;
    public Button StopBtn;

    [Header("Settings")]
    [Tooltip("Full passes through all 17 stimuli during training")]
    public int   TrainingReps  = 3;
    [Tooltip("Number of random stimuli used during evaluation")]
    public int   EvalTrials    = 17;
    [Tooltip("Seconds to display the cue before each trial starts")]
    public float CueDuration   = 2.5f;

    // ── Stimulus display names (indices 0-16) ─────────────────────────────────
    static readonly string[] Names =
    {
        "Red    Calm",    "Red    Happy",    "Red    Excited",    "Red    Yelling!",
        "Blue   Calm",    "Blue   Happy",    "Blue   Excited",    "Blue   Yelling!",
        "Yellow Calm",    "Yellow Happy",    "Yellow Excited",    "Yellow Yelling!",
        "Green  Calm",    "Green  Happy",    "Green  Excited",    "Green  Yelling!",
        "PLAY / PAUSE",
    };

    // ── Runtime ───────────────────────────────────────────────────────────────
    private Coroutine _sequence;
    private int       _lastPrediction = -1;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    void Start()
    {
        // Subscribe to every incoming prediction so evaluation can compare
        var handler = GetComponent<CharacterSelectionHandler>()
                   ?? FindObjectOfType<CharacterSelectionHandler>();
        if (handler != null)
            handler.OnIndexPredicted += idx => _lastPrediction = idx;

        SetUI("Press a button to begin.", "", "");
        SetButtons(trainActive: true, evalActive: true, stopActive: false);
    }

    // ── Public button callbacks ───────────────────────────────────────────────

    public void BeginTraining()
    {
        if (_sequence != null) return;
        _sequence = StartCoroutine(TrainingSequence());
    }

    public void BeginEvaluation()
    {
        if (_sequence != null) return;
        _sequence = StartCoroutine(EvaluationSequence());
    }

    public void StopSequence()
    {
        if (_sequence == null) return;
        StopCoroutine(_sequence);
        _sequence = null;
        SetUI("Stopped.", "", "");
        SetButtons(trainActive: true, evalActive: true, stopActive: false);
    }

    // ── Training sequence ─────────────────────────────────────────────────────

    IEnumerator TrainingSequence()
    {
        var bci = FindObjectOfType<MindCTRLBCIController>();
        if (!ValidateBCI(bci)) yield break;

        bci.StopContinuousTrials();
        SetButtons(trainActive: false, evalActive: false, stopActive: true);

        int total = TrainingReps * 17;
        int done  = 0;

        for (int rep = 0; rep < TrainingReps; rep++)
        {
            var order = Enumerable.Range(0, 17)
                                  .OrderBy(_ => Random.value)
                                  .ToList();
            foreach (int target in order)
            {
                done++;
                SetProgress($"Trial {done} / {total}");
                SetCue($"FOCUS ON:\n{Names[target]}");
                yield return new WaitForSeconds(CueDuration);

                if (GameConfig.Instance != null && GameConfig.Instance.useMockBCI)
                    bci.MockPrediction(target);          // mock: skip real trial
                else
                    yield return bci.RunTrainingTrial(target);
            }
        }

        SetUI(
            "Training complete!",
            "",
            $"Sent {total} labelled trials to the backend.\n" +
            "Wait for the classifier to train, then run Evaluation.");
        SetButtons(trainActive: true, evalActive: true, stopActive: false);
        _sequence = null;
    }

    // ── Evaluation sequence ───────────────────────────────────────────────────

    IEnumerator EvaluationSequence()
    {
        var bci = FindObjectOfType<MindCTRLBCIController>();
        if (!ValidateBCI(bci)) yield break;

        bci.StopContinuousTrials();
        SetButtons(trainActive: false, evalActive: false, stopActive: true);

        bool mockMode = GameConfig.Instance != null && GameConfig.Instance.useMockBCI;

        int correct = 0;
        var indices = Enumerable.Range(0, 17)
                                .OrderBy(_ => Random.value)
                                .Take(EvalTrials)
                                .ToList();

        for (int i = 0; i < indices.Count; i++)
        {
            int target = indices[i];
            SetProgress($"Eval {i + 1} / {EvalTrials}");
            SetCue($"FOCUS ON:\n{Names[target]}");
            yield return new WaitForSeconds(CueDuration);

            _lastPrediction = -1;

            if (mockMode)
                bci.MockPrediction(target);              // always predicts correctly
            else
                yield return bci.RunTestingTrialCoroutine();

            yield return null;   // one frame for OnPrediction to fire

            bool hit = (_lastPrediction == target);
            if (hit) correct++;

            string gotName = (_lastPrediction >= 0 && _lastPrediction < Names.Length)
                           ? Names[_lastPrediction] : "?";

            SetResult(
                $"Last: {(hit ? "[CORRECT]" : $"[WRONG]  got {gotName}")}\n" +
                $"Accuracy: {correct}/{i + 1}  ({100f * correct / (i + 1):0}%)");
        }

        SetUI(
            "Evaluation complete!",
            "",
            $"Final accuracy: {correct} / {EvalTrials}" +
            $"  ({100f * correct / EvalTrials:0}%)");
        SetButtons(trainActive: true, evalActive: true, stopActive: false);
        _sequence = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    bool ValidateBCI(MindCTRLBCIController bci)
    {
        if (bci != null) return true;
        SetUI("ERROR: BCIController not found.", "", "");
        SetButtons(trainActive: true, evalActive: true, stopActive: false);
        _sequence = null;
        return false;
    }

    void SetCue(string t)      { if (CueLabel      != null) CueLabel.text      = t; }
    void SetProgress(string t) { if (ProgressLabel != null) ProgressLabel.text = t; }
    void SetResult(string t)   { if (ResultLabel   != null) ResultLabel.text   = t; }

    void SetUI(string cue, string progress, string result)
    {
        SetCue(cue); SetProgress(progress); SetResult(result);
    }

    void SetButtons(bool trainActive, bool evalActive, bool stopActive)
    {
        if (StartTrainBtn != null) StartTrainBtn.interactable = trainActive;
        if (StartEvalBtn  != null) StartEvalBtn.interactable  = evalActive;
        if (StopBtn       != null) StopBtn.interactable       = stopActive;
    }
}
