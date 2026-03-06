using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using BCIEssentials.Stimulus.Presentation;

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
    // Order matches SceneBootstrapper left-to-right: Red(0) Green(1) Blue(2) Yellow(3)
    static readonly string[] Names =
    {
        "Red    Calm",    "Red    Happy",    "Red    Excited",    "Red    Yelling!",
        "Green  Calm",    "Green  Happy",    "Green  Excited",    "Green  Yelling!",
        "Blue   Calm",    "Blue   Happy",    "Blue   Excited",    "Blue   Yelling!",
        "Yellow Calm",    "Yellow Happy",    "Yellow Excited",    "Yellow Yelling!",
        "SING / PAUSE",
    };

    [Header("Stimulus grid — 17 Image cells built by SceneBootstrapper")]
    public UnityEngine.UI.Image[] TrainingGridCells = new UnityEngine.UI.Image[17];

    // Normal (dim) background colours matching each character
    static readonly Color[] CellNormal =
    {
        new Color(0.36f, 0.08f, 0.08f),  // Red   (indices 0-3)
        new Color(0.07f, 0.28f, 0.10f),  // Green (indices 4-7)
        new Color(0.08f, 0.15f, 0.36f),  // Blue  (indices 8-11)
        new Color(0.33f, 0.29f, 0.03f),  // Yellow(indices 12-15)
    };
    static readonly Color CellPlayPause  = new Color(0.10f, 0.28f, 0.45f);
    static readonly Color CellTargetCol  = new Color(0f,    0.80f, 1f  );   // cyan  — FOCUS ON
    static readonly Color CellPredictCol = new Color(0.15f, 0.90f, 0.35f);  // green — predicted

    // ── Runtime ───────────────────────────────────────────────────────────────
    private Coroutine _sequence;
    private int       _lastPrediction = -1;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    void Start()
    {
        // Subscribe to every incoming prediction so evaluation can compare
        var handler = GetComponent<CharacterSelectionHandler>()
                   ?? FindAnyObjectByType<CharacterSelectionHandler>();
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
        var bci = FindAnyObjectByType<MindCTRLBCIController>();
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

                // Highlight target — equivalent to SPO.OnTrainTarget()
                var targetPresenter = GetPresenterForIndex(bci, target);
                targetPresenter?.StartTargetIndication();
                HighlightCell(target, CellTargetCol);

                yield return new WaitForSeconds(CueDuration);

                // Remove cue highlight — equivalent to SPO.OffTrainTarget()
                targetPresenter?.EndTargetIndication();
                ResetCell(target);

                // Brief pause (matches backend's 0.5s gap before StimulusOn)
                yield return new WaitForSeconds(0.5f);

                _lastPrediction = -1;

                if (GameConfig.Instance != null && GameConfig.Instance.useMockBCI)
                {
                    yield return MockFlashAnimation(bci);
                    bci.MockPrediction(target);
                }
                else
                {
                    // Run grid visual in parallel so training cells flash during real BCI trial
                    var visual = StartCoroutine(TrainingGridCellFlash(bci));
                    yield return bci.RunTrainingTrial(target);
                    StopCoroutine(visual);
                    for (int i = 0; i < 17; i++) ResetCell(i);
                }

                yield return null;  // one frame so OnIndexPredicted can fire

                // Sham feedback — equivalent to SPO.OnSelection() in backend
                if (_lastPrediction >= 0)
                {
                    bool   correct = (_lastPrediction == target);
                    string gotName = (_lastPrediction < Names.Length) ? Names[_lastPrediction] : $"#{_lastPrediction}";
                    Debug.Log($"[Training] Target: {Names[target]}  |  Predicted: {gotName}  |  {(correct ? "CORRECT" : "WRONG")}");
                    SetResult($"Predicted: {gotName}  {(correct ? "✓ CORRECT" : "✗ WRONG")}");

                    var predPresenter = GetPresenterForIndex(bci, _lastPrediction);
                    predPresenter?.StartTargetIndication();
                    HighlightCell(_lastPrediction, CellPredictCol);
                    yield return new WaitForSeconds(0.5f);
                    predPresenter?.EndTargetIndication();
                    ResetCell(_lastPrediction);
                }
                else
                {
                    Debug.Log($"[Training] Target: {Names[target]}  |  No prediction yet (accumulating data)");
                    SetResult("Accumulating training data — no prediction yet…");
                }
            }
        }

        // Tell the backend to fit the classifier now that all labeled epochs are collected
        bci.GetComponent<BCIEssentials.LSLFramework.MarkerWriter>()?.PushTrainingCompleteMarker();

        SetUI(
            "Training complete!",
            "",
            $"Sent {total} labelled trials to the backend.\n" +
            "Classifier training triggered — run Evaluation once the backend confirms training.");
        SetButtons(trainActive: true, evalActive: true, stopActive: false);
        _sequence = null;
    }

    // ── Evaluation sequence ───────────────────────────────────────────────────

    IEnumerator EvaluationSequence()
    {
        var bci = FindAnyObjectByType<MindCTRLBCIController>();
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

            var targetPresenter = GetPresenterForIndex(bci, target);
            targetPresenter?.StartTargetIndication();
            HighlightCell(target, CellTargetCol);

            yield return new WaitForSeconds(CueDuration);

            targetPresenter?.EndTargetIndication();
            ResetCell(target);

            _lastPrediction = -1;

            if (mockMode)
            {
                yield return MockFlashAnimation(bci);
                bci.MockPrediction(target);
            }
            else
            {
                var visual = StartCoroutine(TrainingGridCellFlash(bci));
                yield return bci.RunTestingTrialCoroutine();
                StopCoroutine(visual);
                for (int j = 0; j < 17; j++) ResetCell(j);
            }

            yield return null;   // one frame for OnPrediction to fire

            bool hit = (_lastPrediction == target);
            if (hit) correct++;

            string gotName = (_lastPrediction >= 0 && _lastPrediction < Names.Length)
                           ? Names[_lastPrediction] : "?";

            Debug.Log($"[Eval] Target: {Names[target]}  |  Predicted: {gotName}  |  {(hit ? "CORRECT" : "WRONG")}  |  Running accuracy: {correct}/{i + 1}");
            SetResult(
                $"Last: {(hit ? "[CORRECT]" : $"[WRONG]  got {gotName}")}\n" +
                $"Accuracy: {correct}/{i + 1}  ({100f * correct / (i + 1):0}%)");

            // Sham feedback — highlight predicted cell
            if (_lastPrediction >= 0)
            {
                var predPresenter = GetPresenterForIndex(bci, _lastPrediction);
                predPresenter?.StartTargetIndication();
                HighlightCell(_lastPrediction, CellPredictCol);
                yield return new WaitForSeconds(0.4f);
                predPresenter?.EndTargetIndication();
                ResetCell(_lastPrediction);
            }
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

    /// <summary>
    /// Returns the StimulusPresentationBehaviour for a flat index (0-15 = pitch buttons, 16 = play/pause).
    /// Equivalent to the backend team's objectList[i].GetComponent<SPO>() lookup.
    /// </summary>
    StimulusPresentationBehaviour GetPresenterForIndex(MindCTRLBCIController bci, int index)
    {
        if (index >= 0 && index < 16 && index < bci.Presenters.Length)
            return bci.Presenters[index];
        if (index == 16)
            return bci.PlayPausePresenter;
        return null;
    }

    // ── Training grid visual flash (real BCI mode) ────────────────────────────

    /// <summary>
    /// Highlights training grid cells in the checkerboard pattern to give visual
    /// feedback during real BCI trials. Does NOT call StartStimulusDisplay —
    /// the real trial handles that. Runs as a background coroutine alongside the trial.
    /// </summary>
    IEnumerator TrainingGridCellFlash(MindCTRLBCIController bci)
    {
        int   reps    = bci != null ? bci.FlashesPerOption : 10;
        float onTime  = bci != null ? bci.OnTime           : 0.10f;
        float offTime = bci != null ? bci.OffTime          : 0.075f;

        Color flashCol = (bci != null && bci.Presenters.Length > 0 && bci.Presenters[0] != null)
            ? bci.Presenters[0].FlashColor
            : new Color(1f, 1f, 0.2f, 1f);

        while (true)   // loop until StopCoroutine is called by the caller
        {
            for (int group = 0; group < 2; group++)
            {
                for (int i = 0; i < 16; i++)
                {
                    int c = i / 4, p = i % 4;
                    if ((c + p) % 2 == group) HighlightCell(i, flashCol);
                    else                      ResetCell(i);
                }
                bool ppOn = group == 0;
                if (ppOn) HighlightCell(16, flashCol); else ResetCell(16);
                yield return new WaitForSeconds(onTime);

                for (int i = 0; i < 17; i++) ResetCell(i);
                yield return new WaitForSeconds(offTime);
            }
        }
    }

    // ── Mock checkerboard flash ───────────────────────────────────────────────

    /// <summary>
    /// Mirrors CheckerboardFlashTrialBehaviour exactly:
    ///   • Calls StartStimulusDisplay() / EndStimulusDisplay() on the actual
    ///     PitchButtonPresenter objects — the same methods the real flash uses.
    ///   • Simultaneously paints the training grid cells with the presenter's
    ///     FlashColor so the flash is visible in the training panel.
    ///
    /// Groups per repetition — true checkerboard (matches BlackWhiteMatrixFactory):
    ///   0 — "black" cells where (charIndex + pitchIndex) % 2 == 0
    ///   1 — "white" cells where (charIndex + pitchIndex) % 2 == 1
    /// </summary>
    IEnumerator MockFlashAnimation(MindCTRLBCIController bci)
    {
        int   reps     = bci != null ? bci.FlashesPerOption : 10;
        float onTime   = bci != null ? bci.OnTime           : 0.10f;
        float offTime  = bci != null ? bci.OffTime          : 0.075f;

        // Read FlashColor from the first presenter so training matches the game exactly
        Color flashCol = (bci != null && bci.Presenters.Length > 0 && bci.Presenters[0] != null)
            ? bci.Presenters[0].FlashColor
            : new Color(1f, 1f, 0.2f, 1f);

        for (int rep = 0; rep < reps; rep++)
        {
            for (int group = 0; group < 2; group++)
            {
                for (int i = 0; i < 16; i++)
                {
                    int  c  = i / 4, p = i % 4;
                    bool on = (c + p) % 2 == group;   // true checkerboard

                    if (on)
                    {
                        HighlightCell(i, flashCol);                      // training grid (visible)
                        bci?.Presenters[i]?.StartStimulusDisplay();      // same method as the game
                    }
                    else
                    {
                        ResetCell(i);
                        bci?.Presenters[i]?.EndStimulusDisplay();
                    }
                }
                // Play/Pause (index 16): conceptually (row=4, col=0) → (4+0)%2==0 → black (group 0)
                bool ppOn = group == 0;
                if (ppOn) { HighlightCell(16, flashCol); bci?.PlayPausePresenter?.StartStimulusDisplay(); }
                else      { ResetCell(16);               bci?.PlayPausePresenter?.EndStimulusDisplay(); }

                yield return new WaitForSeconds(onTime);

                // Inter-flash gap — all off
                for (int i = 0; i < 16; i++)
                {
                    ResetCell(i);
                    bci?.Presenters[i]?.EndStimulusDisplay();
                }
                ResetCell(16);
                bci?.PlayPausePresenter?.EndStimulusDisplay();
                yield return new WaitForSeconds(offTime);
            }
        }

        for (int i = 0; i < 17; i++) ResetCell(i);
    }

    // ── Training grid helpers ─────────────────────────────────────────────────

    void HighlightCell(int index, Color color)
    {
        if (index >= 0 && index < TrainingGridCells.Length && TrainingGridCells[index] != null)
            TrainingGridCells[index].color = color;
    }

    void ResetCell(int index)
    {
        if (index < 0 || index >= 17 || TrainingGridCells[index] == null) return;
        if (index == 16) { TrainingGridCells[16].color = CellPlayPause; return; }
        TrainingGridCells[index].color = CellNormal[index / 4];
    }
}
