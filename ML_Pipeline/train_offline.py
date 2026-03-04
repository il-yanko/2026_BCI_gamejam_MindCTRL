"""
Train the P300 classifier offline from a recorded .xdf session file.

Use this after collecting a session with LabRecorder (or any XDF-compatible
recorder) while playing MindCTRL.  The XDF file contains both the EEG
stream and the Unity marker stream, so no extra setup is needed.

Usage
-----
    cd ML_Pipeline
    python train_offline.py data/session_001.xdf

    # optionally save the trained model for inspection
    python train_offline.py data/session_001.xdf --save models/p300.joblib

The script prints per-fold accuracy and the mean ± std across folds.
If --save is given it also writes the fitted classifier to disk with joblib.

Data format produced by MindCTRL
---------------------------------
Marker stream name : UnityMarkerStream
Marker format      : p300,s,{n_stimuli},{target+1 or -1},{stimulus_index+1}

  Training marker  : p300,s,17,3,5   → 17 stimuli, target=index 2, flash=index 4
  Testing  marker  : p300,s,17,-1,5  → no known target (classification mode)

Only epochs with a known training target are used here.
"""

import argparse
import joblib

from bci_essentials.io.xdf_sources import XdfEegSource, XdfMarkerSource
from bci_essentials.bci_controller import BciController
from bci_essentials.paradigm.p300_paradigm import P300Paradigm
from bci_essentials.data_tank.data_tank import DataTank
from bci_essentials.classification.erp_rg_classifier import ErpRgClassifier


def main(xdf_path: str, save_path: str | None = None, seed: int = 42):
    print(f"Loading: {xdf_path}")

    eeg_source    = XdfEegSource(xdf_path)
    marker_source = XdfMarkerSource(xdf_path)
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

    controller = BciController(
        classifier, eeg_source, marker_source, paradigm, data_tank
    )
    controller.setup(online=False)
    controller.run()

    if save_path:
        joblib.dump(classifier, save_path)
        print(f"Classifier saved → {save_path}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="Train P300 classifier from a recorded XDF session."
    )
    parser.add_argument("xdf", help="Path to .xdf recording file")
    parser.add_argument("--save", default=None, help="Save trained model to this path")
    parser.add_argument("--seed", type=int, default=42)
    args = parser.parse_args()
    main(args.xdf, args.save, args.seed)
