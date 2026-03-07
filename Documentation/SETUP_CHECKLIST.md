# MindCTRL Unity Setup Checklist

Complete this checklist to get MindCTRL running in your Unity project.

## Phase 1: Environment Setup

- [ ] **Python Backend**
  - [ ] Install Miniconda (see PROJECT_SETUP.md for instructions)
  - [ ] Create `bessy` conda environment: `conda create -n bessy python=3.9`
  - [ ] Activate it: `conda activate bessy`
  - [ ] Clone BCI Essentials Python: `git clone https://github.com/kirtonBCIlab/bci-essentials-python`
  - [ ] Install: `cd bci-essentials-python && pip install -e .`

## Phase 2: Unity Project Creation

- [ ] **Create New Project**
  - [ ] Open Unity Hub
  - [ ] Create new **3D** project
  - [ ] Select **Unity 6000.3** or **Unity 2021.3 LTS**
  - [ ] Set project location to `/2026_BCI_gamejam_MindCTRL`

- [ ] **Install BCI Packages**
  - [ ] Open Package Manager (Window > TextEditor & Panels > Package Manager)
  - [ ] Add git URL: `https://github.com/labstreaminglayer/LSL4Unity.git`
  - [ ] Wait for LSL4Unity to import (5-10 minutes)
  - [ ] Add git URL: `https://github.com/kirtonBCIlab/bci-essentials-unity.git`
  - [ ] Wait for BCI Essentials to import (5-10 minutes)
  - [ ] Verify no errors in Console

## Phase 3: Project Structure

- [ ] **Create Folders**
  - [ ] Assets/Scripts/
  - [ ] Assets/Prefabs/
  - [ ] Assets/Audio/
  - [ ] Assets/Scenes/
  - [ ] Assets/Resources/BCI/

- [ ] **Add Scripts**
  - [ ] Copy **GameManager.cs** to Assets/Scripts/
  - [ ] Copy **AudioManager.cs** to Assets/Scripts/
  - [ ] Copy **CharacterBlob.cs** to Assets/Scripts/
  - [ ] Copy **BCIInputHandler.cs** to Assets/Scripts/
  - [ ] Copy **UIManager.cs** to Assets/Scripts/
  - [ ] Verify 0 errors in Console

## Phase 4: Audio Setup

- [ ] **Create/Import Audio Files**
  - [ ] Create or import `deep_voice.wav` (Red character)
  - [ ] Create or import `medium_voice.wav` (Blue character)
  - [ ] Create or import `high_voice.wav` (Yellow character)
  - [ ] Create or import `very_high_voice.wav` (Green character)
  - [ ] Place in Assets/Audio/

- [ ] **Configure AudioManager**
  - [ ] Create empty GameObject named "AudioManager"
  - [ ] Add Component: AudioManager (Script)
  - [ ] In Inspector, set Character Count to 4
  - [ ] For each character (0-3):
    - [ ] Set Character Name (Red, Blue, Yellow, Green)
    - [ ] Assign Voice Clip audio file
    - [ ] Set Base Pitch (1.0 default)
    - [ ] Set Min Pitch (0.5-0.9 range)
    - [ ] Set Max Pitch (1.1-2.0 range)

## Phase 5: Scene Setup

- [ ] **Create Main Scene**
  - [ ] File > New Scene (3D)
  - [ ] Save as: Assets/Scenes/GameScene.unity

- [ ] **Create Canvas**
  - [ ] Right-click Hierarchy > UI > Canvas
  - [ ] Rename to "MainCanvas"
  - [ ] Set Canvas Scaler (1920x1080 base)

- [ ] **Create Game Managers**
  - [ ] Right-click Hierarchy > Create Empty
  - [ ] Rename to "GameManager"
  - [ ] Add Component: GameManager (Script)
  - [ ] Add Component: UIManager (Script)

- [ ] **Create BCI Handler**
  - [ ] Right-click Hierarchy > Create Empty
  - [ ] Rename to "BCIHandler"
  - [ ] Add Component: BCIInputHandler (Script)
  - [ ] In Inspector, set Test Mode = true (for keyboard testing)

## Phase 6: Character Setup (Do 4 times for each color)

For each character (Red, Blue, Yellow, Green):

- [ ] **Character Button**
  - [ ] Right-click MainCanvas > UI > Button - ImageButton
  - [ ] Rename: "Character_Red" (Blue, Yellow, Green)
  - [ ] Set RectTransform size: 150x150
  - [ ] Position in grid layout

- [ ] **Add Blob Image**
  - [ ] Select character button
  - [ ] Add Component: CharacterBlob (Script)
  - [ ] Set Character Index (0=Red, 1=Blue, 2=Yellow, 3=Green)
  - [ ] Set Blob Color accordingly

- [ ] **Create Facial Expressions (4 per character)**
  - [ ] Create 4 Image UI elements under character button
  - [ ] Name: Face_0, Face_1, Face_2, Face_3
  - [ ] Position around blob (left, right, top, bottom)
  - [ ] Create or assign expression sprites/images

- [ ] **Configure CharacterBlob**
  - [ ] In CharacterBlob component:
    - [ ] Set Blob Image reference
    - [ ] Set Facial Expressions array size to 4
    - [ ] For each face (0-3):
      - [ ] Assign Face UI element
      - [ ] Assign Sprite image
      - [ ] Set Pitch Level (0.8, 1.0, 1.2, 1.4)
      - [ ] Set Highlight Color

- [ ] **Assign to GameManager**
  - [ ] Select GameManager in Hierarchy
  - [ ] In GameManager component:
    - [ ] Add to Characters list (drag character blob)

## Phase 7: Testing

- [ ] **Verify Setup**
  - [ ] No errors in Console
  - [ ] All scripts compiled successfully
  - [ ] All components assigned in Inspector

- [ ] **Test Audio**
  - [ ] Press Play
  - [ ] Press Q-R to select faces on Character 1
  - [ ] Verify audio plays with correct pitch
  - [ ] Press ESC to stop

- [ ] **Test Input**
  - [ ] Press Play
  - [ ] Press 1-4 to select different characters
  - [ ] Verify correct character is highlighted
  - [ ] Press Q-W-E-R to select faces
  - [ ] Verify correct face and pitch play

- [ ] **Test P300 Paradigm**
  - [ ] Press Play
  - [ ] Press SPACE to start
  - [ ] Verify characters flash at regular interval
  - [ ] Press SPACE again to stop

- [ ] **Test Looping**
  - [ ] Press Play
  - [ ] Press L to toggle looping
  - [ ] Verify audio loops continuously when enabled
  - [ ] Press L to turn off

## Phase 8: BCI Integration (Optional - for real hardware)

- [ ] **Start Python Backend**
  - [ ] In terminal: `conda activate bessy`
  - [ ] Run BCI backend (adjust path as needed)
  - [ ] Verify LSL stream is broadcasting

- [ ] **Enable BCI in Unity**
  - [ ] Select GameManager
  - [ ] In GameManager settings:
    - [ ] Set enableBCI = true
    - [ ] Set testMode = false

- [ ] **Test BCI Connection**
  - [ ] Press Play
  - [ ] Check Console for "BCI connected successfully" message
  - [ ] Perform BCI trigger (e.g., P300 stimulus)
  - [ ] Verify character/face selects automatically

## Phase 9: Refinement

- [ ] **Visual Polish**
  - [ ] Enlarge UI elements for accessibility
  - [ ] Increase contrast on face highlights
  - [ ] Add smooth animations
  - [ ] Verify colors are distinct (red, blue, yellow, green)

- [ ] **Audio Refinement**
  - [ ] Test volume levels (adjust maxVolume in AudioManager)
  - [ ] Verify speech is clear and distinct
  - [ ] Test pitch ranges are appropriate

- [ ] **Performance Check**
  - [ ] Profiler shows < 10% CPU usage
  - [ ] No frame stuttering during flash
  - [ ] Audio plays without clicks/pops

## Phase 10: Documentation & Deployment

- [ ] **Documentation**
  - [ ] Open IMPLEMENTATION_GUIDE.md
  - [ ] Open QUICK_REFERENCE.md
  - [ ] Review PROJECT_SETUP.md

- [ ] **Build Application**
  - [ ] File > Build Settings
  - [ ] Select target platform (Windows, macOS, Linux)
  - [ ] Add GameScene to scenes
  - [ ] Build standalone executable

- [ ] **Test Standalone Build**
  - [ ] Run executable outside of Unity
  - [ ] Verify all features work
  - [ ] Test with actual target users if possible

## Notes

### Keyboard Test Mode Shortcuts
| Key | Action |
|-----|--------|
| 1-4 | Select character |
| Q-R | Select face |
| SPACE | Start/stop P300 |
| L | Toggle looping |
| ESC | Stop all |

### Audio Pitch Guidelines
- **Deep Voice**: 0.7 - 0.9 (Red)
- **Medium Voice**: 0.95 - 1.05 (Blue)
- **High Voice**: 1.15 - 1.3 (Yellow)
- **Very High Voice**: 1.35 - 1.6 (Green)

### Common Issues
1. **No audio**: Check AudioManager assignment in Hierarchy
2. **Characters not appearing**: Verify Canvas is set up properly
3. **BCI not connecting**: Ensure Python backend is running
4. **Low performance**: Reduce P300 flash frequency

## Completion Summary

When all checkboxes are complete:
✅ Python backend set up
✅ Unity project created with BCI packages
✅ All scripts added and compiled
✅ Audio system configured
✅ Characters with faces created
✅ Game fully functional in test mode
✅ Ready for BCI hardware integration

---

For issues or questions, see QUICK_REFERENCE.md and IMPLEMENTATION_GUIDE.md
