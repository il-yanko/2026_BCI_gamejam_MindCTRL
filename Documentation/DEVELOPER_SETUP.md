# MindCTRL - Developer Setup Guide

> **Unity meta files:** Assets in `Assets/` are accompanied by `.meta` files.
> Never edit them by hand unless necessary – if one becomes corrupted or
> reports an invalid GUID, delete it and reopen the project so Unity can
> regenerate it automatically.  This is the recommended fix for the
> `CharacterBlob_Blue.prefab.meta` error mentioned in the workspace.

## Project Structure for Team Development

```
MindCTRL/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/                    # Game core systems
│   │   │   └── GameManager.cs
│   │   ├── BCI/                     # BCI input handling
│   │   │   ├── BCIInputHandler.cs
│   │   │   └── MockBCIController.cs
│   │   ├── Models/                  # ML/Prediction models
│   │   │   └── MLPredictionManager.cs
│   │   ├── UI/                      # UI components
│   │   │   ├── CharacterBlob.cs
│   │   │   └── UIManager.cs
│   │   ├── Audio/                   # Audio system
│   │   │   └── AudioManager.cs
│   │   └── Utilities/               # Shared utilities
│   │       └── GameConfig.cs
│   ├── Data/
│   │   ├── Config/                  # Configuration files
│   │   │   └── settings.json
│   │   └── Models/                  # ML models
│   │       └── p300_model.onnx
│   ├── Prefabs/
│   │   └── CharacterBlob.prefab
│   ├── Audio/                       # Voice files (add later)
│   └── Scenes/
│       └── GameScene.unity
├── bci-essentials-python/           # Python backend (submodule)
├── Assets/Plugins/
│   └── bci-essentials-unity/        # Unity BCI package (submodule)
├── ML_Pipeline/                     # ML development area
│   ├── notebooks/
│   ├── models/
│   ├── data/
│   └── DEVELOPMENT.md
├── Tests/                           # Test scripts
├── Documentation/                   # Team documentation
│   ├── DEVELOPER_SETUP.md          # This file
│   ├── ML_PIPELINE.md
│   ├── TESTING_GUIDE.md
│   └── API_REFERENCE.md
├── LICENSE
└── README.md
```

## Getting Started as a Developer

### 1. Clone Repository with Submodules

```bash
git clone --recurse-submodules https://github.com/yourusername/2026_BCI_gamejam_MindCTRL.git
cd 2026_BCI_gamejam_MindCTRL
```

If you already cloned without submodules:
```bash
git submodule update --init --recursive
```

### 2. Set Up Python Environment

```bash
# Navigate to Python backend
cd bci-essentials-python

# Create conda environment
conda create -n mindctrl-dev python=3.9
conda activate mindctrl-dev

# Install dependencies
pip install -e .
```

### 3. Open Unity Project

- Unity version: **6000.3** or **2021.3 LTS**
- Wait for packages to import (5-10 minutes)
  - LSL4Unity (from Assets/Plugins/bci-essentials-unity)
  - BCI Essentials Unity (from Assets/Plugins/bci-essentials-unity)

### 4. Test with Mock Flag

Once you open the project in Unity:

1. Create a new scene or open existing GameScene
2. Select or create a **GameConfig** GameObject
3. In Inspector, configure:
   - `Use Mock BCI: True` (for testing without hardware)
   - `Enable Audio: False` (for prototyping without sound files)
   - `Enable ML Prediction: False` (until models are ready)

4. Press Play and test with keyboard:
   - **1-4**: Select character
   - **Q-R**: Select face/expression
   - **SPACE**: Trigger selection
   - **T**: Toggle auto-random selection
   - **L**: Toggle looping

## Configuration System

### GameConfig Component

Located: `Assets/Scripts/Utilities/GameConfig.cs`

The **GameConfig** is a singleton that manages all game settings. Add it to your scene and configure it in the Inspector.

#### BCI Settings
- `Use Mock BCI`: Enable mock controller for testing (default: true)
- `Enable BCI Hardware`: Connect to real BCI system (default: false)
- `Flashing Frequency`: P300 stimulus speed (0.5-10 Hz)
- `P300 Threshold`: Confidence threshold (0-1)

#### Audio Settings
- `Enable Audio`: Toggle audio system (default: false for prototype)
- `Master Volume`: Audio volume (0-1)
- `Allow Looping`: Auto-repeat playback
- `Min/Max Pitch`: Voice pitch range

#### ML Settings
- `Enable ML Prediction`: Use ML models (default: false)
- `Model Path`: Location of trained model
- `Confidence Threshold`: Min threshold for predictions
- `EEG Channels`: Number of EEG channel expected
- `Sample Rate`: Hz (usually 250)

#### Game Settings
- `Character Count`: How many blobs (1-10)
- `Faces Per Character`: Expressions per blob (1-5)
- `Debug Logging`: Enable console debug output
- `Log BCI Events`: Log all BCI activity
- `Log ML Predictions`: Log ML model outputs

## Testing Framework

### Mock BCI Controller

Located: `Assets/Scripts/BCI/MockBCIController.cs`

Simulates P300 BCI input without hardware.

**Features:**
- Keyboard input: 1-4 for characters, Q-R for faces
- Auto-random selection mode for stress testing
- Generates dummy EEG data for ML testing
- Full event system integration

**Usage:**
```csharp
MockBCIController mockBCI = MockBCIController.Instance;
mockBCI.TriggerP300(characterIndex, faceIndex);
mockBCI.ToggleRandomSelection(); // Auto-test mode
```

### Running Tests

#### C# EditMode Tests
The project includes several NUnit-based tests in `Tests/Unit/`. To run them:

1. Open the project in Unity.
2. Go to **Window → General → Test Runner**.
3. Select the **EditMode** tab and click **Run All**.

These tests exercise the configuration system, mock controller, ML manager,
etc., and do not require any scene or hardware.

#### Python Tests (Optional)
If you later add Python test scripts, you can execute them with:

```bash
cd Tests
python -m pytest
```

#### Manual Game Prototype Test
1. Open Unity.
2. In GameConfig: `Use Mock BCI = true`, `Enable Audio = false`.
3. Press Play.
4. Use keyboard shortcuts to exercise game logic.

## Development Workflow

### For Game Development

1. **Modify game logic** → Edit files in `Assets/Scripts/Core/`
2. **Test with mock flag** → Set GameConfig.useMockBCI = true
3. **Test in-game** → Play in Unity editor
4. **Build & test** → Create standalone build

### For BCI Integration

1. **Modify BCI handler** → Edit `Assets/Scripts/BCI/BCIInputHandler.cs`
2. **Test with mock first** → Use MockBCIController
3. **Use real hardware** → Set GameConfig.enableBCIHardware = true
4. **Run Python backend** → Start BCI Essentials Python

### For ML Model Development

1. **Develop model** → Work in `ML_Pipeline/` folder
2. **Export ONNX** → Save trained model
3. **Integrate** → Implement IMLPredictionModel interface
4. **Test** → Set GameConfig.enableMLPrediction = true

## Git Workflow

### Creating Feature Branch

```bash
git checkout -b feature/your-feature-name
```

### Common Tasks

#### Adding new BCI feature
```bash
git checkout -b feature/bci-improvement
# Edit files in Assets/Scripts/BCI/
git add Assets/Scripts/BCI/
git commit -m "Improve BCI signal handling"
git push origin feature/bci-improvement
```

#### Adding ML model
```bash
git checkout -b feature/ml-p300-classifier
# Add model to Assets/Data/Models/
# Implement IMLPredictionModel
git add Assets/Data/Models/
git add Assets/Scripts/Models/
git commit -m "Add P300 classification model"
git push origin feature/ml-p300-classifier
```

#### Audio integration
```bash
git checkout -b feature/audio-integration
# Add audio files to Assets/Audio/
# Update AudioManager if needed
git add Assets/Audio/
git commit -m "Add voice audio files"
git push origin feature/audio-integration
```

### Updating Submodules

If BCI Essentials packages are updated:

```bash
git submodule update --remote
git add bci-essentials-python Assets/Plugins/bci-essentials-unity
git commit -m "Update BCI Essentials packages to latest"
```

## Team Responsibilities

### BCI Developer
- Maintains `Assets/Scripts/BCI/`
- Integrates with real BCI hardware
- Manages LSL connections
- Updates BCIInputHandler

### ML Developer
- Develops models in `ML_Pipeline/`
- Implements IMLPredictionModel
- Tests with MockBCIController
- Exports ONNX models

### Game Developer
- Maintains `Assets/Scripts/Core/` and `Assets/Scripts/UI/`
- Manages game mechanics
- Game balancing
- Visual design

### Audio Engineer
- Records/creates voice samples
- Adds audio files to `Assets/Audio/`
- Configures AudioManager
- Tests audio integration

## Common Issues

### Mock BCI Not Working
```
Solution: Verify GameConfig.useMockBCI = true
          Check Console for error messages
          Verify MockBCIController is in scene
```

### Audio Not Playing
```
Solution: Set GameConfig.enableAudio = true
          Add audio files to Assets/Audio/
          Verify AudioClips assigned in AudioManager
          Check volume settings
```

### ML Model Not Loading
```
Solution: Check model path in GameConfig.modelPath
          Verify ONNX model format
          Implement IMLPredictionModel correctly
          Enable debug logging to see errors
```

### Submodule Issues
```
If submodules not updating:
git submodule foreach git pull origin main

If submodules missing:
git submodule update --init --recursive

To remove submodule:
git submodule deinit Assets/Plugins/bci-essentials-unity
```

## Performance Tips

1. **Use Mock BCI** for rapid development (~no overhead)
2. **Disable Audio** during prototype phase (~saves 5-10% CPU)
3. **Disable ML** until models are ready (~saves 20-30% CPU)
4. **Use small EEG channel count** for testing (8 instead of 64)
5. **Profile regularly** - Window > Analysis > Profiler

## Documentation

- **API Reference** → See comments in source files
- **ML Pipeline** → See ML_Pipeline/DEVELOPMENT.md
- **Testing** → See Tests/TESTING_GUIDE.md
- **Deployment** → See README.md

## Questions?

1. Check code comments
2. Check IMPLEMENTATION_GUIDE.md
3. Check QUICK_REFERENCE.md
4. Create GitHub issue
5. Contact project lead

---

**Happy developing! Use the Mock flag extensively for rapid iteration.**
