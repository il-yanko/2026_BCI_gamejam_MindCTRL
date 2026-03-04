# MindCTRL - BCI Game for Disabled Children

A Unity-based Brain-Computer Interface (BCI) game featuring interactive color blob characters with dynamic voice feedback. Developed for the 2026 BCI Game Jam in Calgary, Canada.

## 🎮 Game Overview

**MindCTRL** is designed to engage disabled children through a fun, interactive environment where players use BCI input (P300 paradigm) to control 4 colored blob characters, each with unique voices and facial expressions.

### Game Features

- **4 Color Blob Characters**: Red (deep voice), Blue (medium voice), Yellow (high voice), Green (very high voice)
- **Dynamic Facial Expressions**: 4 faces per character representing different pitch levels
- **Real-time Voice Control**: Adjust vocal pitch in real-time
- **P300 BCI Integration**: Uses Lab Streaming Layer (LSL) for brain signal input
- **Accessible Design**: Large UI elements, clear visuals, instant feedback
- **Test Mode**: Keyboard input for development without BCI hardware

## 📋 Quick Start

### 1. Prerequisites
- **Unity 6000.3** or **Unity 2021.3 LTS**
- **macOS with ARM64 chip** (or Windows/Linux)
- **Python 3.9+** with Miniconda
- **Git**

### 2. Setup Python Backend

```bash
# Install Miniconda if needed (see PROJECT_SETUP.md)
bash Miniconda3-latest-MacOSX-arm64.sh

# Create BCI environment
conda create -n bessy python=3.9
conda activate bessy

# Install BCI Essentials
git clone https://github.com/kirtonBCIlab/bci-essentials-python
cd bci-essentials-python
pip install -e .
```

### 3. Create Unity Project

1. Open Unity Hub
2. Create new **3D** project with **Unity 6000.3**
3. Install BCI packages via Package Manager:
   - Add git URL: `https://github.com/labstreaminglayer/LSL4Unity.git`
   - Add git URL: `https://github.com/kirtonBCIlab/bci-essentials-unity.git`

### 4. Add Scripts

Copy these C# scripts to `Assets/Scripts/`:
- `GameManager.cs` - Main game controller
- `AudioManager.cs` - Voice and audio management
- `CharacterBlob.cs` - Character representation
- `BCIInputHandler.cs` - BCI input processing
- `UIManager.cs` - UI management

### 5. Test Game

Press **Play** in Unity, then test with keyboard:
- **1-4**: Select character
- **Q-W-E-R**: Select face/pitch
- **SPACE**: Start P300 paradigm (flashing)
- **L**: Toggle voice looping
- **ESC**: Stop activity

## 📁 Project Structure

```
Assets/
├── Scripts/
│   ├── GameManager.cs        # Main game orchestrator
│   ├── AudioManager.cs        # Voice and audio system
│   ├── CharacterBlob.cs       # Character behavior
│   ├── BCIInputHandler.cs     # BCI integration
│   └── UIManager.cs           # UI management
├── Prefabs/
│   └── CharacterBlob.prefab   # Character prefab template
├── Audio/
│   ├── deep_voice.wav
│   ├── medium_voice.wav
│   ├── high_voice.wav
│   └── very_high_voice.wav
├── Scenes/
│   └── GameScene.unity        # Main game scene
└── Resources/
    └── BCI/                   # BCI configuration files
```

## 🎯 Game Design

### Characters
- **Red Blob**: Deep voice (base pitch ~0.8)
- **Blue Blob**: Medium voice (base pitch ~1.0)
- **Yellow Blob**: High voice (base pitch ~1.2)
- **Green Blob**: Very high voice (base pitch ~1.4)

### Facial Expressions
4 faces per character showing different pitch levels:
1. Low pitch (0.8)
2. Medium-low pitch (1.0)
3. Medium-high pitch (1.2)
4. High pitch (1.4)

## 🧠 BCI Integration

This game uses the **P300 Event-Related Potential (ERP)** paradigm:
- Characters flash on screen
- When user sees desired character flash, brain generates P300 signal
- Signal detected as selection

## 📚 Documentation

- **[PROJECT_SETUP.md](PROJECT_SETUP.md)** - Detailed setup guide
- **[IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md)** - Code structure and implementation details
- **[BCI Essentials Docs](https://docs.bci.games/)** - Official documentation

## 🛠️ Development

### Keyboard Controls

| Key | Action |
|-----|--------|
| 1-4 | Select character |
| Q-R | Select face |
| SPACE | Start P300 paradigm |
| L | Toggle looping |
| ESC | Stop all |

## 📝 License & Acknowledgments

- MIT License (see LICENSE file)
- Built with [BCI Essentials](https://github.com/kirtonBCIlab)
- 2026 BCI Game Jam

---

Developed by Team MindCTRL during BCI Game Jam 2026 in Calgary, Canada.
