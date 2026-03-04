# MindCTRL BCI Game - Project Setup Guide

## Overview
A BCI game for disabled children featuring 4 color blob characters with different voices. Players use P300 BCI input to select characters and facial expressions (pitch levels).

## System Architecture

### Game Components
1. **Character Blobs** (4 total)
   - Red Blob (Deep voice)
   - Blue Blob (Medium voice)
   - Yellow Blob (High voice)
   - Green Blob (Very High voice)

2. **Facial Expression UI** (4 per blob)
   - Represents pitch/emotion levels
   - Changes with audio playback
   - Selectable via BCI

3. **Audio System**
   - 4 different vocal sounds
   - Real-time pitch adjustment
   - Continuous playback capability
   - Short sound effects for reactions

4. **BCI Integration**
   - P300 flashing paradigm
   - LSL (Lab Streaming Layer) for data transmission
   - Real-time input handling

## Installation Steps

### Prerequisites
- **Unity**: Version 6000.3 (Latest)
- **Python**: 3.9 or later with conda
- **macOS**: ARM64 compatible

### Step 1: Set Up Python Backend (BCI Essentials)

```bash
# Install Miniconda if not already installed
# For macOS ARM64:
bash Miniconda3-latest-MacOSX-arm64.sh

# Create and activate conda environment
conda create -n bessy python=3.9
conda activate bessy

# Clone and install BCI Essentials Python
git clone https://github.com/kirtonBCIlab/bci-essentials-python
cd bci-essentials-python
pip install -e .
```

### Step 2: Create Unity Project

1. Open Unity Hub
2. Create new 3D project with Unity 6000.3
3. Choose desktop platform

### Step 3: Install BCI Unity Packages

In Unity's Package Manager (Window > TextEditor & Panels > Package Manager):
1. Click '+' and select 'Add package from git URL'
2. Add: `https://github.com/labstreaminglayer/LSL4Unity.git`
3. Add: `https://github.com/kirtonBCIlab/bci-essentials-unity.git`

Wait for packages to import. This may take 5-10 minutes.

### Step 4: Project Structure

```
Assets/
├── Scripts/
│   ├── GameManager.cs
│   ├── BCIInputHandler.cs
│   ├── CharacterBlob.cs
│   ├── AudioManager.cs
│   └── UIManager.cs
├── Prefabs/
│   └── CharacterBlob.prefab
├── Audio/
│   ├── deep_voice.wav
│   ├── medium_voice.wav
│   ├── high_voice.wav
│   └── very_high_voice.wav
├── Scenes/
│   └── GameScene.unity
└── Resources/
    └── BCI/ (BCI configuration files)
```

## Game Flow

1. **Initialization**
   - Connect to BCI backend via LSL
   - Initialize audio system
   - Create 4 character blobs on screen

2. **Game Loop**
   - Display P300 flashing paradigm
   - Wait for BCI selection
   - Play audio and update character face
   - Real-time pitch adjustment possible
   - Optional looping feature

3. **BCI Input Mapping**
   - Rows: 4 Characters
   - Columns: 4 Facial Expressions (pitch levels)
   - P300 detection triggers audio playback

## Development Notes

- Use LSL4Unity for BCI data streaming
- Implement event-driven audio playback
- Character face UI should be responsive to voice changes
- Consider accessibility for disabled children (large clickable areas, clear visuals)

## Next Steps

1. Create character blob scripts
2. Implement audio system
3. Build BCI input handler
4. Create P300 paradigm UI
5. Test with BCI backend
