"""
MindCTRL P300 Backend — online classification during gameplay.

Run this BEFORE pressing Play in Unity.

What it does
------------
1. Waits for the EEG LSL stream (your headset driver must be running).
2. Waits for the Unity marker stream ("UnityMarkerStream").
3. After enough training trials Unity sends, the classifier trains itself.
4. From that point on it sends predictions back to Unity via LslMessenger.
   Unity routes them through CharacterSelectionHandler.OnPrediction()
   → GameFlowController.SetPitch() / TogglePlay().

Stimulus layout (matches MindCTRLBCIController)
------------------------------------------------
  Index  0- 3  →  Red    blob, pitches Calm/Happy/Excited/Yelling
  Index  4- 7  →  Blue   blob
  Index  8-11  →  Yellow blob
  Index 12-15  →  Green  blob
  Index 16     →  Play/Pause button

Usage
-----
    cd ML_Pipeline
    python backend.py

Optional: set a fixed random seed for reproducibility
    python backend.py --seed 42
"""

import argparse

from bci_essentials.io.lsl_sources import LslEegSource, LslMarkerSource
from bci_essentials.io.lsl_messenger import LslMessenger
from bci_essentials.bci_controller import BciController
from bci_essentials.paradigm.p300_paradigm import P300Paradigm
from bci_essentials.data_tank.data_tank import DataTank
from bci_essentials.classification.erp_rg_classifier import ErpRgClassifier


def main(seed: int = 42):
    print("MindCTRL P300 backend starting…")
    print("Waiting for EEG stream and Unity marker stream…")

    eeg_source    = LslEegSource()
    marker_source = LslMarkerSource()
    messenger     = LslMessenger()
    paradigm      = P300Paradigm()
    data_tank     = DataTank()

    # XDawn + Riemannian geometry + LDA — strong default for P300.
    classifier = ErpRgClassifier()
    classifier.set_p300_clf_settings(
        n_splits=5,
        lico_expansion_factor=4,   # augment rare P300 class
        oversample_ratio=0,
        undersample_ratio=0,
        random_seed=seed,
        remove_flats=True,
    )

    controller = BciController(
        classifier, eeg_source, marker_source, paradigm, data_tank, messenger
    )

    controller.setup(online=True)
    print("Connected. Waiting for training trials from Unity…")
    controller.run()


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--seed", type=int, default=42)
    args = parser.parse_args()
    main(seed=args.seed)
