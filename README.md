# MindCTRL — Opera of Blobs

A Unity 6 BCI game for the **2026 BCI Game Jam, Calgary**.

Four coloured blob characters sing at different pitches. A P300 BCI headset (or keyboard in mock mode) selects which blob sings which note. The result is a live blob orchestra controlled by brain signals.

---

## Quick start (no hardware needed)

```bash
# 1. Clone with submodules
git clone --recurse-submodules https://github.com/il-yanko/2026_BCI_gamejam_MindCTRL.git
cd 2026_BCI_gamejam_MindCTRL

# 2. Open in Unity Hub → Add project → select the folder
#    Required version: 6000.3.10f1
#    Unity will import packages automatically (~5 min first time)

# 3. One-time scene setup (30 seconds):
#    File → New Scene (Empty)
#    Hierarchy → right-click → Create Empty → rename "Bootstrapper"
#    Inspector → Add Component → SceneBootstrapper
#    File → Save Scene → Assets/Scenes/Game.unity

# 4. Press Play
```

The scene builds itself entirely at runtime — no prefabs or manual wiring needed.

---

## Keyboard controls (mock mode)

Mock BCI is **on by default** (`useMockBCI = true` in `GameConfig`).

| Keys | Action |
|------|--------|
| `1` `2` `3` `4` | Red blob → pitch Calm / Happy / Excited / Yelling |
| `Q` `W` `E` `R` | Blue blob → pitch 0–3 |
| `A` `S` `D` `F` | Yellow blob → pitch 0–3 |
| `Z` `X` `C` `V` | Green blob → pitch 0–3 |
| `Space` | Play / Pause all blobs |
| `Esc` | Return to Main Menu |

Selecting a pitch makes that blob start singing at that note. Blobs rise/fall on screen to match the selected pitch level. Floating music notes appear above singing blobs.

---

## Game mechanics

- **4 blob characters** — Red, Blue, Yellow, Green — each with a distinct voice range
- **4 pitch levels** per blob (Calm → Happy → Excited → Yelling), shown as small note-head buttons
- **Play/Pause button** — starts or stops the whole orchestra
- **P300 flashing** — 17 stimuli total: 16 note-head buttons + 1 Play/Pause button
- Selecting a note-head via BCI sets that blob's pitch and starts it singing immediately
- Selecting Play/Pause via BCI toggles playback

---

## Adding voice audio (optional)

1. Select the `Bootstrapper` GameObject in the Hierarchy
2. In the Inspector, find the four `VoiceClips` arrays (`RedVoiceClips`, `BlueVoiceClips`, etc.)
3. Assign 4 `AudioClip` assets per character — index 0 = Calm, 1 = Happy, 2 = Excited, 3 = Yelling
4. If no clips are assigned the game still works silently

Audio can be toggled globally: `GameConfig → Enable Audio`.

---

## Project structure

```
Assets/
├── Scripts/
│   ├── Config/
│   │   └── GameConfig.cs               # Singleton settings (useMockBCI, enableAudio, etc.)
│   ├── Game/
│   │   ├── SceneBootstrapper.cs        # Builds the entire scene procedurally at runtime
│   │   ├── GameFlowController.cs       # State machine: MainMenu → Game / Training
│   │   ├── CharacterBlobPresenter.cs   # Blob visuals, audio, sway/bob animation, music notes
│   │   ├── PitchButtonPresenter.cs     # P300 stimulus — one note-head button (index 0–15)
│   │   ├── PlayPauseButtonPresenter.cs # P300 stimulus — Play/Pause button (index 16)
│   │   └── CharacterSelectionHandler.cs# Routes BCI prediction index → game action
│   └── BCI/
│       ├── MindCTRLBCIController.cs    # Wires 17 stimuli into SingleFlashTrialBehaviour
│       ├── MockP300Input.cs            # Keyboard mock (active when useMockBCI = true)
│       └── RuntimePresenterCollection.cs # Fixes null-list issue for AddComponent() usage
├── Plugins/
│   ├── bci-essentials-unity/           # BCI framework (git submodule)
│   ├── LSLStub.cs                      # Compile stub — replaces LSL4Unity when offline
│   └── labstreaminglayer.LSL4Unity.Runtime.asmdef
└── Scenes/
    └── (create your scene here)

bci-essentials-python/                  # Python BCI backend (git submodule)
ML_Pipeline/                            # EEG model development
Tests/                                  # EditMode unit tests
```

---

## Switching to real BCI hardware

1. In the Inspector on the `GameConfig` component, set **Use Mock BCI → unchecked**
2. Install the real LSL4Unity package (replaces `LSLStub.cs`)
3. Start the Python backend:

```bash
cd bci-essentials-python
pip install -e .
python demos/p300_demo.py   # or your custom backend
```

4. Press Play — the game connects to the LSL stream automatically

---

## P300 stimulus layout

```
Stimulus index  Button
──────────────────────────────────────────
 0  1  2  3     Red   blob: Calm/Happy/Excited/Yelling
 4  5  6  7     Blue  blob: Calm/Happy/Excited/Yelling
 8  9 10 11     Yellow blob
12 13 14 15     Green blob
16              Play / Pause button
```

`flatIndex = charIndex * 4 + pitchIndex`

---

## Configuration reference

All settings live on the `GameConfig` component (created automatically by `SceneBootstrapper`).

| Field | Default | Description |
|-------|---------|-------------|
| `useMockBCI` | `true` | Keyboard mock instead of real BCI |
| `enableAudio` | `true` | Global audio on/off |
| `masterVolume` | `1.0` | Volume for all blob voices |
| `flashesPerOption` | `10` | P300 flashes per stimulus per trial |
| `onTime` | `0.1 s` | Flash-on duration |
| `offTime` | `0.075 s` | Flash-off duration |

---

## Team roles

| Area | Folder | What to touch |
|------|--------|----------------|
| Game / UI | `Assets/Scripts/Game/` | Visuals, mechanics, scene layout |
| BCI | `Assets/Scripts/BCI/` | Trial parameters, LSL integration |
| Audio | `SceneBootstrapper` Inspector | Assign `AudioClip[]` arrays |
| ML | `ML_Pipeline/` | EEG model training & ONNX export |

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| Scene is empty on Play | Add `SceneBootstrapper` component to a GameObject |
| Buttons do nothing | Make sure you pressed **New Game** first |
| No flashing | Check Unity console for errors; verify `GamePanel` is active |
| Audio silent | Assign `AudioClip[]` in SceneBootstrapper Inspector, or set `enableAudio = true` |
| BCI not connecting | Ensure Python backend is running; check LSL stream name |
| Submodule folder empty | Run `git submodule update --init --recursive` |

---

## Requirements

- **Unity** 6000.3.10f1 (exact version recommended)
- **OS** macOS 12+, Windows 10+, or Ubuntu 20.04+
- **Python** 3.9+ (only needed for real BCI backend)

---

## Acknowledgements

- [BCI Essentials Unity](https://github.com/kirtonBCIlab/bci-essentials-unity) — Kirton BCI Lab
- [BCI Essentials Python](https://github.com/kirtonBCIlab/bci-essentials-python) — Kirton BCI Lab
- [Lab Streaming Layer](https://github.com/labstreaminglayer/liblsl)
- 2026 BCI Game Jam organizers
