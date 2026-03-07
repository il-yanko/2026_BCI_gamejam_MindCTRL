"""
Synthetic P300 offline test for MindCTRL — no headset or XDF file required.

Generates in-memory synthetic EEG with realistic P300 responses for all 17
MindCTRL stimuli (16 pitch buttons + 1 play/pause), then runs the same offline
BCI pipeline used by train_offline.py to train and cross-validate the
ErpRgClassifier.

Marker protocol (matches bci-essentials-unity SingleFlashTrialBehaviour)
------------------------------------------------------------------------
  "Trial Started"                      ← before each trial
  "p300,s,17,{target+1},{flash+1}"     ← one per flash  (1-indexed)
  "Trial Ends"                         ← after each trial → triggers classify
  "Training Complete"                  ← after all trials → triggers fit()

Usage
-----
    cd ML_Pipeline
    source venv/bin/activate          # Windows: venv\\Scripts\\activate
    python synthetic_p300_test.py

    # more data → higher accuracy (slower)
    python synthetic_p300_test.py --trials 100 --reps 8

    # save the trained classifier
    python synthetic_p300_test.py --save models/synthetic_p300.joblib
"""

import argparse
import numpy as np
import sys
import os

from bci_essentials.io.sources import EegSource, MarkerSource
from bci_essentials.bci_controller import BciController
from bci_essentials.paradigm.p300_paradigm import P300Paradigm
from bci_essentials.data_tank.data_tank import DataTank
from bci_essentials.classification.erp_rg_classifier import ErpRgClassifier

# ── EEG / simulation parameters ──────────────────────────────────────────────

N_CHANNELS       = 8
SAMPLE_RATE      = 256.0          # Hz
CHANNEL_NAMES    = ["Fz", "Cz", "Pz", "P3", "P4", "O1", "O2", "Oz"]
P300_CHANNELS    = [1, 2, 3, 4]   # Cz, Pz, P3, P4  (0-indexed)

N_STIMULI        = 17
FLASH_SOA        = 0.125          # stimulus onset-to-onset (seconds)
BASELINE_DUR     = 0.20           # pre-trial EEG silence (seconds)
POST_TRIAL_DUR   = 0.80           # buffer after last flash — must exceed epoch_end=0.6
P300_LATENCY     = 0.340          # peak latency post-stimulus (seconds)
P300_SIGMA       = 0.060          # Gaussian width (seconds)
P300_AMPLITUDE   = 10.0           # µV peak  (noise floor ≈ 5 µV)

# ── Synthetic data generation ─────────────────────────────────────────────────


def generate_data(
    n_training_trials: int = 50,
    n_reps: int = 5,
    seed: int = 42,
) -> tuple:
    """
    Return (eeg_samples, eeg_timestamps, markers, marker_timestamps).

    eeg_samples      : list[list[float]]  — shape (n_total_samples, N_CHANNELS)
    eeg_timestamps   : list[float]
    markers          : list[list[str]]    — each inner list has one string
    marker_timestamps: list[float]
    """
    rng = np.random.default_rng(seed)
    dt  = 1.0 / SAMPLE_RATE

    all_eeg       = []
    all_eeg_ts    = []
    all_markers   = []
    all_marker_ts = []

    n_baseline  = int(BASELINE_DUR   * SAMPLE_RATE)
    n_post      = int(POST_TRIAL_DUR * SAMPLE_RATE)
    n_per_flash = int(FLASH_SOA      * SAMPLE_RATE)

    t = 0.0   # running LSL-style clock

    print(
        f"Generating {n_training_trials} training trials × "
        f"{n_reps} reps × {N_STIMULI} stimuli …"
    )

    for trial_i in range(n_training_trials):
        target      = int(rng.integers(0, N_STIMULI))
        target_1idx = target + 1   # 1-indexed for marker format

        # n_reps shuffled orderings of all stimuli
        flash_order = np.concatenate(
            [rng.permutation(N_STIMULI) for _ in range(n_reps)]
        )
        n_flashes  = len(flash_order)
        n_flash_all = n_flashes * n_per_flash
        n_total    = n_baseline + n_flash_all + n_post

        # Full timestamp vector for this trial block
        trial_ts = t + np.arange(n_total) * dt

        # Background: Gaussian noise
        trial_eeg = rng.standard_normal((n_total, N_CHANNELS)).astype(np.float32) * 5.0

        # Alpha oscillation on occipital channels (realistic background)
        alpha = (3.0 * np.sin(2.0 * np.pi * 10.0 * trial_ts)).astype(np.float32)
        trial_eeg[:, 5] += alpha   # O1
        trial_eeg[:, 6] += alpha   # O2
        trial_eeg[:, 7] += alpha   # Oz

        # "Trial Started" command marker
        t_first_flash = t + n_baseline * dt
        all_markers.append(["Trial Started"])
        all_marker_ts.append(t_first_flash - BASELINE_DUR)

        # Flash markers + P300 injection
        for flash_i, stim in enumerate(flash_order):
            stim    = int(stim)
            flash_t = t_first_flash + flash_i * FLASH_SOA

            # Flash marker: p300,s,{n},{target_1idx},{stim_1idx}
            marker_str = f"p300,s,{N_STIMULI},{target_1idx},{stim + 1}"
            all_markers.append([marker_str])
            all_marker_ts.append(flash_t)

            # Inject Gaussian P300 for target flash
            if stim == target:
                peak_t = flash_t + P300_LATENCY
                p300   = (
                    P300_AMPLITUDE
                    * np.exp(-((trial_ts - peak_t) ** 2) / (2.0 * P300_SIGMA ** 2))
                ).astype(np.float32)
                for ch in P300_CHANNELS:
                    trial_eeg[:, ch] += p300

        # "Trial Ends" command marker → triggers process_and_classify in BciController
        t_last_flash = t_first_flash + (n_flashes - 1) * FLASH_SOA
        all_markers.append(["Trial Ends"])
        all_marker_ts.append(t_last_flash + FLASH_SOA)

        all_eeg.extend(trial_eeg.tolist())
        all_eeg_ts.extend(trial_ts.tolist())

        t = float(trial_ts[-1]) + dt

    # "Training Complete" → triggers classifier.fit()
    all_markers.append(["Training Complete"])
    all_marker_ts.append(t)

    n_samples  = len(all_eeg_ts)
    n_markers  = len(all_markers)
    flash_only = sum(1 for m in all_markers if m[0].startswith("p300"))
    print(
        f"  EEG samples : {n_samples:,}  "
        f"({n_samples / SAMPLE_RATE:.1f} s)\n"
        f"  Markers     : {n_markers}  "
        f"({flash_only} flash + {n_training_trials} trial-start/end pairs + 1 train-complete)"
    )

    return all_eeg, all_eeg_ts, all_markers, all_marker_ts


# ── EegSource / MarkerSource (in-memory, XDF-style) ──────────────────────────

class SyntheticEegSource(EegSource):
    """Returns all synthetic EEG on the first call to get_samples(); empty thereafter."""

    def __init__(self, samples, timestamps):
        self._samples    = samples
        self._timestamps = timestamps

    @property
    def name(self) -> str:        return "SyntheticEEG"

    @property
    def fsample(self) -> float:   return SAMPLE_RATE

    @property
    def n_channels(self) -> int:  return N_CHANNELS

    @property
    def channel_types(self) -> list[str]:  return ["eeg"] * N_CHANNELS

    @property
    def channel_units(self) -> list[str]:  return ["microvolts"] * N_CHANNELS

    @property
    def channel_labels(self) -> list[str]: return list(CHANNEL_NAMES)

    def get_samples(self) -> tuple[list[list], list]:
        samples, timestamps = self._samples, self._timestamps
        self._samples    = [[]]
        self._timestamps = []
        return [samples, timestamps]

    def time_correction(self) -> float: return 0.0


class SyntheticMarkerSource(MarkerSource):
    """Returns all synthetic markers on the first call to get_markers(); empty thereafter."""

    def __init__(self, markers, timestamps):
        self._markers    = markers
        self._timestamps = timestamps

    @property
    def name(self) -> str: return "SyntheticMarkers"

    def get_markers(self) -> tuple[list[list], list]:
        markers, timestamps = self._markers, self._timestamps
        self._markers    = [[]]
        self._timestamps = []
        return [markers, timestamps]

    def time_correction(self) -> float: return 0.0


# ── Main ─────────────────────────────────────────────────────────────────────

def main(
    n_training_trials: int = 50,
    n_reps: int = 5,
    seed: int = 42,
    save_path: str | None = None,
):
    eeg_samples, eeg_ts, markers, marker_ts = generate_data(
        n_training_trials=n_training_trials,
        n_reps=n_reps,
        seed=seed,
    )

    eeg_source    = SyntheticEegSource(eeg_samples, eeg_ts)
    marker_source = SyntheticMarkerSource(markers, marker_ts)
    paradigm      = P300Paradigm()
    data_tank     = DataTank()

    classifier = ErpRgClassifier()
    classifier.set_p300_clf_settings(
        n_splits=5,
        lico_expansion_factor=4,
        oversample_ratio=0,
        undersample_ratio=0,
        random_seed=seed,
        remove_flats=True,
    )

    print("\nRunning offline BCI pipeline …")
    controller = BciController(
        classifier, eeg_source, marker_source, paradigm, data_tank
    )
    controller.setup(online=False)
    controller.run()

    print("\nDone.")
    if save_path:
        import joblib
        os.makedirs(os.path.dirname(save_path) or ".", exist_ok=True)
        joblib.dump(classifier, save_path)
        print(f"Classifier saved → {save_path}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="Synthetic P300 offline test for MindCTRL (no headset needed)."
    )
    parser.add_argument(
        "--trials", type=int, default=50,
        help="Number of training trials (default: 50)",
    )
    parser.add_argument(
        "--reps", type=int, default=5,
        help="Flash repetitions per trial per stimulus (default: 5)",
    )
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument(
        "--save", default=None,
        help="Save trained classifier to this path (.joblib)",
    )
    args = parser.parse_args()
    main(args.trials, args.reps, args.seed, args.save)
