# MindCTRL - Unity Implementation Guide

## Scripts Overview

You now have 5 core C# scripts ready to use in your Unity project:

### 1. **GameManager.cs** - Main Game Controller
- Orchestrates the entire game
- Manages character selection and face selection
- Handles P300 paradigm stimulus
- Controls game settings (looping, BCI, test mode)

**Key Methods:**
- `StartP300Paradigm()` - Begin flashing stimulus
- `StopAllActivity()` - Stop everything
- `ToggleLooping()` - Toggle voice looping
- `SetFlashingFrequency(float)` - Adjust flash rate

### 2. **AudioManager.cs** - Voice & Audio System
- Manages 4 character voices
- Real-time pitch adjustment
- Looping support
- Independent audio sources for each character

**Key Methods:**
- `PlayVoice(int characterIndex, float pitchLevel)` - Play voice with specific pitch
- `SetPitchLevel(int characterIndex, float pitchLevel)` - Adjust pitch in real-time
- `StopVoice(int characterIndex)` - Stop specific character
- `SetLooping(bool shouldLoop)` - Toggle looping

### 3. **CharacterBlob.cs** - Character Representation
- Represents a single character blob with color
- Manages 4 facial expressions per character
- Handles animations and visual feedback
- Responds to selection and highlighting

**Key Methods:**
- `SetFaceExpression(int faceIndex)` - Set and play specific face/pitch
- `Flash()` - Flash animation for P300 stimulus
- `SetSelected(bool selected)` - Highlight character

### 4. **BCIInputHandler.cs** - BCI Integration
- Handles P300 paradigm input detection
- Manages LSL (Lab Streaming Layer) connection
- Test mode with keyboard input (1-4 for characters, Q-R for faces)
- Events for character and face selection

**Key Methods:**
- `SetBCIEnabled(bool enabled)` - Enable/disable BCI
- `SetTestMode(bool enabled)` - Enable keyboard test mode
- `SelectCharacterAndFace(int characterIndex, int faceIndex)` - Manual selection

**Test Mode Keyboard Layout:**
- **1-4**: Select character (1=Red/Deep, 2=Blue/Medium, 3=Yellow/High, 4=Green/VeryHigh)
- **Q-W-E-R**: Select face/pitch (0-3)

### 5. **UIManager.cs** - UI & Display Management
- Manages P300 paradigm visual display
- Controls highlighting and highlighting during stimulus
- Provides feedback display
- Manages canvas layout

**Key Methods:**
- `DisplayP300Paradigm()` - Show stimulus
- `HideP300Paradigm()` - Hide stimulus
- `DisplayFeedback(string message)` - Show feedback

## Unity Project Setup Instructions

### Step 1: Create Unity Project
1. Open Unity Hub
2. Create new **3D** project with **Unity 6000.3**
3. Select desktop platform
4. Create in your workspace folder

### Step 2: Install BCI Packages
1. Open Package Manager: **Window > TextEditor & Panels > Package Manager**
2. Click **+** → **Add package from git URL**
3. Add URL: `https://github.com/labstreaminglayer/LSL4Unity.git`
   - Wait for import (5-10 minutes)
4. Add URL: `https://github.com/kirtonBCIlab/bci-essentials-unity.git`
   - Wait for import (5-10 minutes)

### Step 3: Create Asset Folders
```
Assets/
├── Scripts/
├── Prefabs/
├── Audio/
├── Scenes/
└── Resources/
    └── BCI/
```

Create these folders in your Assets directory.

### Step 4: Add Scripts to Project
1. Copy these scripts to `Assets/Scripts/`:
   - GameManager.cs
   - AudioManager.cs
   - CharacterBlob.cs
   - BCIInputHandler.cs
   - UIManager.cs

2. Verify no compilation errors (should see 0 errors in console)

### Step 5: Create Base Scene
1. Create new Scene: **File > New Scene**
2. Save as: `Assets/Scenes/GameScene.unity` (or open the provided
   `Assets/Scenes/PrototypeScene.unity` which already contains the 4 blob
   objects and a background music AudioSource).


### Blob Prefabs
Prefabs for the four characters are already included in
`Assets/Prefabs/`. Each prefab has an `AudioSource` component with the
placeholder `Assets/Audio/BackgroundMusic.wav` clip assigned. You can
instantiate them directly or drag them into your own scene.

3. **Setup Canvas:**
   - Right-click Hierarchy > **UI > Canvas**
   - Name it: **MainCanvas**

4. **Add Game Objects:**
   - Right-click Hierarchy > **Create Empty**
   - Name it: **GameManager**
   - Add Component: **GameManager (Script)**
   - Add Component: **UIManager (Script)**

5. **Create Character Blobs:**
   - For each character (4 total):
     - Right-click under MainCanvas > **UI > Button - ImageButton**
     - Name: **Character_Red** (adjust for each)
     - Set RectTransform size to 150x150
     - Position in grid layout
     
   - Add to each Character button:
     - Component: **CharacterBlob (Script)**
     - Component: **CanvasGroup**
     - Set character index (0-3)
     - Set blob color

6. **Create Audio Sources:**
   - Right-click Hierarchy > **Audio > Audio Source**
   - Name: **AudioManager**
   - Add Component: **AudioManager (Script)**

7. **Add BCI Handler:**
   - Right-click Hierarchy > **Create Empty**
   - Name: **BCIHandler**
   - Add Component: **BCIInputHandler (Script)**

### Step 6: Assign Audio Clips
1. Create/import audio files for 4 voices:
   - `deep_voice.wav` - Red character
   - `medium_voice.wav` - Blue character
   - `high_voice.wav` - Yellow character
   - `very_high_voice.wav` - Green character

2. In AudioManager component:
   - Set Character Count to 4
   - For each character, assign:
     - Character Name
     - Voice Clip (imported audio file)
     - Base Pitch (1.0)
     - Min/Max Pitch values

### Step 7: Configure Character Blobs
For each CharacterBlob component:
1. Set **Character Index** (0-3)
2. Create 4 facial expression images:
   - Create images or use simple colored circles
   - Assign to Facial Expressions array
   - Set pitch levels for each face (e.g., 0.8, 1.0, 1.2, 1.4)
3. Assign Blob Image UI element
4. Set Blob Color

### Step 8: Test Setup
1. Press **Play** in Unity
2. Test modes (keyboard input needed):
   - Press **SPACE** to start P300 paradigm (characters flash)
   - Press **1-4** to select character
   - Press **Q-W-E-R** to select face/pitch
   - Press **L** to toggle looping
   - Press **ESC** to stop all activity

## BCI Connection Setup (Advanced)

### Prerequisites
Ensure Python backend is running:

```bash
conda activate bessy
# Run your BCI Essentials Python backend
python -m bci_essentials.stream
```

### Integration Steps
1. BCI Essentials Unity will auto-detect LSL streams
2. The BCIInputHandler will connect to P300 stimulus stream
3. When P300 is detected, it triggers character/face selection
4. Real-time feedback updates UI

**Note:** Requires running BCI backend. For development, use Test Mode.

## Development Checklist

- [ ] Unity 6000.3 project created
- [ ] BCI packages installed (LSL4Unity, BCI Essentials)

## Unit Tests

All automated tests live in the `Tests/Unit/` directory at the root of the
repository so that they are separate from the `Assets` tree. Each test file uses
Unity's built–in NUnit runner and can be executed from the **Test Runner** window
(Win: `Window -> General -> Test Runner`).

Example test classes include:

- `GameConfigTests.cs` – verifies getter/setter behavior and singleton reset.
- `MockBCIControllerTests.cs` – exercises the fake controller and EEG helpers.
- `MLPredictionManagerTests.cs` – checks prediction flow and model swapping.
- `AudioManagerTests.cs` – ensures audio toggles and volume clamping work.
- `BCIInputHandlerTests.cs` – confirms mock initialization and event firing.

To run the tests:

1. Open Unity and load the project.
2. Open **Window > General > Test Runner**.
3. Click **Refresh** if the tests are not listed.
4. Select **EditMode** tests and choose **Run All**.

These tests don't require any scene or hardware and should pass on a clean
import. The `GameConfig.ResetInstanceForTests()` helper is used internally to
avoid state leaking between cases.

Unit tests are an important part of the workflow and should live alongside any
new non-trivial logic you add. Keep them in `Tests/Unit` so they don't clutter
the runtime build and can be executed quickly in edit mode.
- [ ] All 5 scripts added to Assets/Scripts/
- [ ] No compilation errors
- [ ] Canvas and UI elements created
- [ ] Character blobs created and configured
- [ ] Audio manager configured with 4 voices
- [ ] Test mode working (keyboard input)
- [ ] P300 paradigm visual working
- [ ] Audio playback working
- [ ] BCI input connection tested
- [ ] Full game flow tested

## Keyboard Shortcuts (Test Mode)

| Key | Action |
|-----|--------|
| 1-4 | Select character |
| Q-R | Select face |
| SPACE | Start P300 paradigm |
| L | Toggle looping |
| ESC | Stop all activity |

## Next Steps

1. **Create Art Assets:**
   - Design 4 colored blob characters
   - Design 4 facial expression sprites per character
   - Animation sprites for transitions

2. **Add Sound Effects:**
   - Record or generate 4 different voice samples
   - Add reaction sounds for face selection

3. **Enhance UI:**
   - Add visual feedback (score, selections made)
   - Add instructions for children
   - Make UI accessible (large buttons, clear colors)

4. **Integrate with Backend:**
   - Implement actual P300 detection
   - Connect to running BCI Essentials Python
   - Test with real BCI hardware

5. **Testing & Iteration:**
   - Test with target audience ( children)
   - Gather feedback
   - Iterate on design

## Troubleshooting

**Scripts not showing in Inspector:**
- Verify script file names match class names exactly
- Check for compilation errors in Console
- Restart Unity if needed

**Audio not playing:**
- Verify audio clips are imported correctly
- Check volume settings in AudioManager
- Ensure AudioClip is assigned in Inspector

**BCI not connecting:**
- Verify LSL4Unity package is installed
- Check Python backend is running
- Enable Test Mode for development

**Performance issues:**
- Reduce P300 flashing frequency
- Limit number of simultaneous audio sources
- Profile with Unity Profiler

## Support Resources

- [BCI Essentials Docs](https://docs.bci.games/)
- [BCI Essentials Unity GitHub](https://github.com/kirtonBCIlab/bci-essentials-unity)
- [LSL4Unity GitHub](https://github.com/labstreaminglayer/LSL4Unity)
- [Unity Manual](https://docs.unity3d.com/)
