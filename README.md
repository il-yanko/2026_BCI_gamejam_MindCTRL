# MindCTRL - BCI Game for Children

[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Python 3.9+](https://img.shields.io/badge/python-3.9+-blue.svg)]()
[![Unity 2021.3+](https://img.shields.io/badge/unity-2021.3+-blue.svg)]()
[![Status](https://img.shields.io/badge/status-Active%20Development-brightgreen)]()

A Unity-based Brain-Computer Interface (BCI) game for disabled children featuring interactive color blob characters with dynamic voice control. Fully configurable with **Mock BCI flag** for testing without hardware, and designed for team development with built-in ML pipeline architecture.

Developed for the **2026 BCI Game Jam** in Calgary, Canada.

## 🎮 Game Overview

**MindCTRL** engages disabled children through a fun, interactive environment where players use BCI input (P300 paradigm) to control 4 colored blob characters, each with unique voices and facial expressions.

### ⭐ Key Features

**Gameplay**
- 4 Color Blob Characters: Red (deep voice), Blue (medium voice), Yellow (high voice), Green (very high voice)
- 4 Dynamic Facial Expressions per character (different pitch levels)
- Real-time voice pitch control (optional audio)
- P300 BCI integration (LSL via BCI Essentials)

**Development & Testing**
- 🧪 **Mock BCI Flag**: Test full game without hardware - just one config toggle!
- 🔧 **GameConfig System**: All settings in Inspector, no code changes needed
- ⌨️ **Keyboard Test Mode**: 1-4 for characters, Q-R for faces, SPACE to trigger
- 🤖 **ML Pipeline Ready**: Extensible architecture for custom ML models
- 👥 **Multi-Developer**: Git submodules, organized folders, clear responsibilities

**Accessibility**
- Large UI elements (>150px)
- Clear, distinct colors
- Instant visual feedback
- No time pressure (brain signals don't rush)

## 🚀 Quick Start

### For Prototyping (Right Now!)

```bash
# 1. Clone with submodules
git clone --recurse-submodules <your-repo-url>
cd 2026_BCI_gamejam_MindCTRL

# 2. Open in Unity 6000.3 (wait for imports ~10 min)

# 3. Create a scene with:
# - GameConfig component (enable Mock BCI, disable Audio)
# - GameManager component  
# - 4 CharacterBlob UI buttons

# 4. Press Play and test with keyboard
# 1-4: Select character
# Q-R: Select face
# SPACE: Trigger selection
```

### For Full Development

**See documentation:**
1. **[DEVELOPER_SETUP.md](Documentation/DEVELOPER_SETUP.md)** - Team setup & workflow
2. **[SETUP_CHECKLIST.md](SETUP_CHECKLIST.md)** - Step-by-step implementation
3. **[ML_Pipeline/DEVELOPMENT.md](ML_Pipeline/DEVELOPMENT.md)** - ML integration

## 📁 Project Structure

```
MindCTRL/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/                 # Game core systems
│   │   │   └── GameManager.cs
│   │   ├── BCI/                  # BCI & Mock controller
│   │   │   ├── BCIInputHandler.cs
│   │   │   └── MockBCIController.cs
│   │   ├── Models/               # ML models & prediction
│   │   │   └── MLPredictionManager.cs
│   │   ├── UI/                   # UI components
│   │   │   └── CharacterBlob.cs
│   │   ├── Audio/                # Audio system
│   │   │   └── AudioManager.cs
│   │   └── Utilities/            # Configuration & helpers
│   │       └── GameConfig.cs
│   ├── Data/
│   │   ├── Config/               # Configuration files
│   │   └── Models/               # ML models (ONNX)
│   ├── Scenes/
│   │   └── GameScene.unity
│   └── Prefabs/
├── bci-essentials-python/        # Python backend (submodule)
├── Assets/Plugins/
│   └── bci-essentials-unity/     # Unity BCI package (submodule)
├── ML_Pipeline/                  # ML development
│   ├── notebooks/
│   ├── models/
│   ├── data/
│   └── DEVELOPMENT.md
├── Documentation/                # Team documentation
│   ├── DEVELOPER_SETUP.md
│   ├── TESTING_GUIDE.md
│   └── API_REFERENCE.md
└── Tests/                        # Test scripts
├── Assets/
│   ├── Prefabs/                 # Character blob prefabs (red, blue, yellow, green)
│   ├── Audio/                   # BackgroundMusic.wav placeholder
│   └── Scenes/                  # Unity scenes (PrototypeScene.unity)
```

## 🧪 Mock BCI Flag

The **Mock BCI** flag enables full game testing without any hardware.

### Enable Mock BCI (Default for Development)

```
Scene Setup:
1. Create GameConfig GameObject
2. In Inspector:
   - Use Mock BCI: ✓ (enabled)
   - Enable BCI Hardware: ✗ (disabled)
   - Enable Audio: ✗ (for prototype without sounds)
3. Press Play
```

### Keyboard Controls (Mock Mode)

| Key | Action |
|-----|--------|
| **1-4** | Select character (Red, Blue, Yellow, Green) |
| **Q-W-E-R** | Select face/expression (0-3) |
| **SPACE** | Trigger P300 selection |
| **T** | Toggle auto-random selection |
| **L** | Toggle voice looping |
| **ESC** | Stop all activity |

### Switch to Real Hardware

```
Scene Setup:
1. In GameConfig Inspector:
   - Use Mock BCI: ✗ (disabled)
   - Enable BCI Hardware: ✓ (enabled)
2. Run Python backend: conda activate bessy && python bci_backend.py
3. Press Play
```

The game seamlessly switches between mock and real input - same code, different configuration!

## 🎯 GameConfig System

All game settings in **one place** - no code changes needed.

### Configuration Options

**BCI Settings**
- `Use Mock BCI`: Enable mock controller for testing
- `Enable BCI Hardware`: Connect to real BCI system
- `Flashing Frequency`: P300 stimulus speed (0.5-10 Hz)
- `P300 Threshold`: Confidence threshold (0-1)

**Audio Settings** (Optional)
- `Enable Audio`: Toggle audio system
- `Master Volume`: Audio volume (0-1)
- `Allow Looping`: Auto-repeat playback
- `Min/Max Pitch`: Voice pitch range

**ML Settings** (Optional)
- `Enable ML Prediction`: Use ML models
- `Model Path`: Location of trained model
- `Confidence Threshold`: Min prediction confidence
- `EEG Channels`: Number of channels (8, 16, 32, 64)
- `Sample Rate`: Hz (usually 250)

**Game Settings**
- `Character Count`: Number of blobs (1-10)
- `Faces Per Character`: Expressions per blob (1-5)
- `Debug Logging`: Console output
- `Log BCI Events`: Log all BCI activity
- `Log ML Predictions`: Log model outputs

## 🤖 ML Pipeline Architecture

Extensible design for custom ML models.

### For ML Developers

1. **Train Model** in Python (see ML_Pipeline/DEVELOPMENT.md)
2. **Export as ONNX** for Unity
3. **Implement IMLPredictionModel** C# interface
4. **Set in MLPredictionManager**
5. Enable `enableMLPrediction` in GameConfig
6. Game reads predictions and triggers selections!

### Example Model Integration

```csharp
// Implement IMLPredictionModel
public class MyP300Classifier : IMLPredictionModel
{
    public void Initialize(GameConfig.MLSettings settings) { }
    public bool Predict(float[,] eegEpoch, out int charIndex, 
                        out int faceIndex, out float confidence) { }
    public void UpdateModel(float[,] eegEpoch, int charIndex, int faceIndex) { }
    public string GetStatus() { }
    public bool IsReady() { }
    public void Cleanup() { }
}

// Use in game
MLPredictionManager.Instance.SetMLModel(new MyP300Classifier());
```

See **ML_Pipeline/DEVELOPMENT.md** for complete guide.

## 🛠️ Development Workflow

### For Game Developers
1. Modify `Assets/Scripts/Core/` and `Assets/Scripts/UI/`
2. Test with Mock BCI enabled
3. Use GameConfig for all settings
4. No audio files needed for prototype

### For BCI Developers
1. Modify `Assets/Scripts/BCI/`
2. Test with MockBCIController first
3. Integrate real LSL stream
4. Use BCIInputHandler event system

### For ML Developers
1. Work in `ML_Pipeline/` folder
2. Train models on collected EEG data
3. Export as ONNX
4. Implement IMLPredictionModel
5. Test with mock EEG data from MockBCIController

### For Audio Engineers
1. Create/record 4 voice samples
2. Drop in `Assets/Audio/`
3. Enable `Audio Settings > Enable Audio`
4. Adjust volume and pitch ranges

## 🔄 Git Workflow

### Clone with Submodules
```bash
git clone --recurse-submodules <repo>
```

### Update Submodules
```bash
git submodule update --remote
```

### Create Feature Branch
```bash
git checkout -b feature/your-feature-name
# Make changes
git commit -m "Describe changes"
git push origin feature/your-feature-name
```

### Common Workflows

**Adding BCI feature:**
```bash
git checkout -b feature/bci-improvement
# Edit Assets/Scripts/BCI/
git add Assets/Scripts/BCI/
git commit -m "Improve BCI signal handling"
```

**Adding ML model:**
```bash
git checkout -b feature/ml-classifier
# Add model to Assets/Data/Models/
# Implement IMLPredictionModel
git add Assets/Data/Models/ Assets/Scripts/Models/
git commit -m "Add P300 classifier"
```

**Adding audio:**
```bash
git checkout -b feature/audio-integration
# Add audio files
git add Assets/Audio/
git commit -m "Add voice audio files"
```

## 📚 Documentation

### User Documentation
- **[README.md](README.md)** - This file
- **[START_HERE.md](Documentation/START_HERE.md)** - Getting started
- **[QUICK_REFERENCE.md](Documentation/QUICK_REFERENCE.md)** - Architecture & shortcuts

### Developer Documentation
- **[DEVELOPER_SETUP.md](Documentation/DEVELOPER_SETUP.md)** - Team setup & workflow
- **[SETUP_CHECKLIST.md](Documentation/SETUP_CHECKLIST.md)** - Step-by-step implementation
- **[ML_Pipeline/DEVELOPMENT.md](ML_Pipeline/DEVELOPMENT.md)** - ML integration guide
- **[IMPLEMENTATION_GUIDE.md](Documentation/IMPLEMENTATION_GUIDE.md)** - Code configuration
- **[FILE_INVENTORY.md](Documentation/FILE_INVENTORY.md)** - Complete file listing

## 🧪 Testing

### Unit Testing
The repository contains two types of unit tests:

* **C# EditMode tests** located in `Tests/Unit/`. These are executed inside the
  Unity editor using the built‑in NUnit runner (Window → General → Test
  Runner → EditMode → Run All).
* **Python tests** (optional) under the same folder; you can run them with
  `pytest` if you later add any backend scripts.

To run the Python tests (if any are present):

```bash
cd Tests
python -m pytest
```

### Game Testing
1. Open Unity and load `Assets/Scenes/PrototypeScene.unity` (includes 4
   character blobs with background music already attached).
2. In GameConfig: `Use Mock BCI = true`, `Enable Audio = false` if you prefer
   testing without the music.
3. Press Play
4. Use keyboard shortcuts
5. Verify character selection and feedback

### BCI Hardware Testing
1. Run BCI backend
2. In GameConfig: `Use Mock BCI = false`, `Enable BCI Hardware = true`
3. Run game
4. Monitor console for "P300 Detected" messages

## 🏆 Team Responsibilities

| Role | Folder | Tasks |
|------|--------|-------|
| **Game Dev** | `Assets/Scripts/Core/UI/` | Game mechanics, UI, visuals |
| **BCI Dev** | `Assets/Scripts/BCI/` | LSL integration, real hardware |
| **ML Dev** | `ML_Pipeline/` | Model training, ONNX export |
| **Audio Eng** | `Assets/Audio/` | Voice recordings, pitch tuning |

## 📋 System Requirements

### Minimum
- **OS**: macOS 10.12+, Windows 10+, Ubuntu 18.04+
- **RAM**: 4GB
- **Storage**: 2GB for Unity project
- **Python**: 3.9+
- **Unity**: 2021.3 LTS or 6000.3

### Recommended
- **RAM**: 8GB+
- **GPU**: NVIDIA/AMD (optional, for ML training)
- **Internet**: For package installation

## 🎓 Learning Resources

### In Code
- **GameManager.cs** - Complete game loop
- **AudioManager.cs** - Singleton pattern + audio
- **CharacterBlob.cs** - UI component pattern
- **BCIInputHandler.cs** - Event system
- **GameConfig.cs** - Configuration system
- **MockBCIController.cs** - Testing framework

### In Documentation
- Architecture diagrams (see QUICK_REFERENCE.md)
- Data flow charts
- Game state diagrams
- API reference for all classes

## 🚀 Deployment

### Build Standalone

```
File > Build Settings
1. Select platform (Windows/macOS/Linux)
2. Add GameScene to scenes list
3. Build
```

### With BCI Backend

```bash
# Terminal 1: Start Python backend
conda activate bessy
python bci_essentials_backend.py

# Terminal 2: Run built game
./MindCTRL.exe  # or .app on macOS
```

## 🐛 Troubleshooting

| Problem | Solution |
|---------|----------|
| Mock BCI not working | Check GameConfig.useMockBCI = true |
| Audio not playing | Set enableAudio = true, add audio clips |
| Game crashes | Check console, verify all components assigned |
| BCI not connecting | Ensure Python backend running, check LSL stream |
| ML not loading | Check modelPath correct, verify ONNX format |

**See DEVELOPER_SETUP.md for more issues.**

## 📞 Support

- **Code Issues**: Check code comments
- **Setup Issues**: See SETUP_CHECKLIST.md
- **Development**: See DEVELOPER_SETUP.md
- **ML Questions**: See ML_Pipeline/DEVELOPMENT.md
- **Architecture**: See QUICK_REFERENCE.md

## 📝 License

MIT License - See [LICENSE](LICENSE) file

## 🙏 Acknowledgments

- **BCI Essentials**: [Kirton BCI Lab](https://github.com/kirtonBCIlab)
- **Lab Streaming Layer**: [LSL Community](https://github.com/labstreaminglayer/liblsl)
- **2026 BCI Game Jam**: Organizers and participants

## 🔗 Resources

- [BCI Essentials Docs](https://docs.bci.games/)
- [BCI Essentials Python](https://github.com/kirtonBCIlab/bci-essentials-python)
- [BCI Essentials Unity](https://github.com/kirtonBCIlab/bci-essentials-unity)
- [LSL4Unity](https://github.com/labstreaminglayer/LSL4Unity)
- [Unity Manual](https://docs.unity3d.com/)

## 📊 Project Status

✅ **Core Framework**: Complete
✅ **Configuration System**: Complete
✅ **Mock BCI Controller**: Complete
✅ **ML Pipeline Architecture**: Complete
✅ **Multi-Developer Setup**: Complete
⏳ **Art Assets**: Pending
⏳ **Voice Audio**: Pending
⏳ **BCI Hardware**: Pending integration
⏳ **ML Models**: Pending from team

---

## 🎯 Next Steps

1. **Read** [DEVELOPER_SETUP.md](Documentation/DEVELOPER_SETUP.md)
2. **Clone** with submodules: `git clone --recurse-submodules <url>`
3. **Open** in Unity 6000.3
4. **Enable** Mock BCI in GameConfig
5. **Press** Play and test!

**Developed for 2026 BCI Game Jam, Calgary, Canada**

*Use the Mock flag for rapid iteration. Test with real hardware when ready.*
