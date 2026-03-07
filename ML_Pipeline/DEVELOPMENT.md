# MindCTRL — ML Pipeline

P300 classification using the `bci-essentials-python` library.
The entire pipeline (epoching, feature extraction, Riemannian geometry, LDA) is handled by `ErpRgClassifier` — you just need to run the right script.

---

## Full data flow

```
EEG headset
    │  LSL stream  (type: EEG, e.g. "SimulatedEEG" or your device name)
    ▼
┌─────────────────────────────────────────────────────────────────┐
│  backend.py  (or  train_offline.py  for XDF files)              │
│                                                                  │
│  LslEegSource  ──►  P300Paradigm  ──►  ErpRgClassifier          │
│  LslMarkerSource        │                    │                   │
│                   epoch EEG around     XDawn covariances         │
│                   each flash event     Riemannian geometry       │
│                   (0–600 ms window)    LDA decision              │
│                                             │                    │
│                                       prediction (0–16)          │
│                                             │                    │
│                                       LslMessenger               │
└─────────────────────────────────────────────────────────────────┘
    │  LSL stream  (type: BCI_Essentials_Predictions)
    ▼
Unity ResponseProvider
    └► CharacterSelectionHandler.OnPrediction()
           ├─ index  0–15  → GameFlowController.SetPitch(char, pitch)
           └─ index 16     → GameFlowController.TogglePlay()
```

**Marker sent by Unity on every flash:**
```
p300,s,{n_stimuli},{target+1 or -1},{stimulus_index+1}

Classification:  p300,s,17,-1,5    (no known target)
Training:        p300,s,17,3,5     (target = index 2, flash = index 4)
```
The LSL timestamp on each marker is what the classifier uses to align EEG epochs.

---

## Setup

```bash
cd ML_Pipeline
pip install -r requirements.txt
# or, if you prefer conda:
# conda env create -f ../bci-essentials-python/environment.yml
# conda activate bci
# pip install -e ../bci-essentials-python
```

---

## Workflow A — Online (real headset + running game)

```
Terminal 1:  python simulate_eeg.py      # skip if using a real headset
Terminal 2:  python backend.py
Terminal 3:  open Unity, press Play → New Game
```

`backend.py` connects to both LSL streams and runs continuously.
As the game sends training markers (Unity Training panel), it trains in real time.
Once trained it sends predictions back and the game responds.

---

## Workflow B — Offline (train from a recorded session)

### Step 1 — Record a session

1. Install **LabRecorder**: https://github.com/labstreaminglayer/App-LabRecorder/releases
2. Start your EEG headset driver
3. Open Unity → Training panel (sends labeled `p300,s,17,{target},…` markers)
4. In LabRecorder select both streams and click **Start**
5. Complete the training session, click **Stop**
6. Save the `.xdf` file to `ML_Pipeline/data/`

### Step 2 — Train

```bash
python train_offline.py data/session_001.xdf --save models/p300_classifier.joblib
```

### Step 3 — Evaluate interactively

Open `notebooks/01_train_and_evaluate.ipynb` in Jupyter:

```bash
jupyter notebook notebooks/01_train_and_evaluate.ipynb
```

---

## Testing without hardware

`simulate_eeg.py` broadcasts a synthetic 8-channel EEG stream with injected
P300 deflections, letting you test the full pipeline on any machine:

```bash
python simulate_eeg.py   # terminal 1 — fake headset
python backend.py        # terminal 2 — classifier
# open Unity             # terminal 3 — game
```

---

## Classifier internals

`ErpRgClassifier` (from `bci-essentials-python`) uses:

| Step | Method |
|------|--------|
| Spatial filter | XDawn covariance estimation |
| Feature space | Riemannian tangent space |
| Classifier | Linear Discriminant Analysis |
| Augmentation | LICO oversampling of the rare P300 class |
| Validation | Stratified k-fold cross-validation |

No manual feature engineering is needed.

---

## Stimulus-to-index mapping

```
Index  0  –  3   Red    blob  (Calm / Happy / Excited / Yelling)
Index  4  –  7   Blue   blob
Index  8  – 11   Yellow blob
Index 12  – 15   Green  blob
Index 16          Play / Pause button
```

`charIndex = index // 4`,  `pitchIndex = index % 4`

---

## File structure

```
ML_Pipeline/
├── backend.py               ← run during live gameplay
├── train_offline.py         ← train from XDF recording
├── simulate_eeg.py          ← fake EEG stream for testing
├── requirements.txt
├── data/
│   ├── README.md
│   └── *.xdf                (gitignored — can be large)
├── models/
│   ├── README.md
│   └── *.joblib             (gitignored)
├── notebooks/
│   └── 01_train_and_evaluate.ipynb
└── DEVELOPMENT.md
```
