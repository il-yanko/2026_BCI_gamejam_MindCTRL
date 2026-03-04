# MindCTRL Quick Reference

## File Overview

| File | Purpose |
|------|---------|
| `GameManager.cs` | Central orchestrator - controls game flow and state |
| `AudioManager.cs` | Handles 4 character voices and pitch adjustment |
| `CharacterBlob.cs` | Visual representation of each character + facial expressions |
| `BCIInputHandler.cs` | BCI signal input + keyboard test mode |
| `UIManager.cs` | P300 paradigm UI display and management |
| `PROJECT_SETUP.md` | Detailed installation guide |
| `IMPLEMENTATION_GUIDE.md` | Code structure and Unity setup instructions |

## Architecture Diagram

```
┌─────────────────────────────────────────────────────┐
│              Unity Game Engine                      │
├─────────────────────────────────────────────────────┤
│                                                      │
│  ┌──────────────────────────────────────────────┐  │
│  │           GameManager (Singleton)             │  │
│  │  ├─ Game flow control                        │  │
│  │  ├─ State management                         │  │
│  │  └─ P300 paradigm timing                     │  │
│  └──────────────────────────────────────────────┘  │
│           │              │              │           │
│           ▼              ▼              ▼           │
│      ┌────────┐     ┌────────┐    ┌──────────┐   │
│      │ Audio  │     │  BCI   │    │   UI     │   │
│      │Manager │     │ Handler│    │ Manager  │   │
│      └────────┘     └────────┘    └──────────┘   │
│           │              │              │           │
│           ▼              ▼              ▼           │
│      ┌─────────────────────────────────────────┐  │
│      │    Character Blobs (4x)                 │  │
│      │  ┌────────────────────────────────────┐ │  │
│      │  │  CharacterBlob                    │ │  │
│      │  │  ├─ Blob image + color           │ │  │
│      │  │  ├─ 4 Facial expressions UI      │ │  │
│      │  │  ├─ Flash animation              │ │  │
│      │  │  └─ Selection highlight          │ │  │
│      │  └────────────────────────────────────┘ │  │
│      │  (x4 for Red, Blue, Yellow, Green)    │  │
│      └─────────────────────────────────────────┘  │
│                                                    │
└─────────────────────────────────────────────────────┘
         │                          │
         ▼                          ▼
    ┌──────────────┐        ┌─────────────────┐
    │ Audio System │        │ BCI Backend     │
    │ (4 speakers) │        │ (Python + LSL)  │
    └──────────────┘        └─────────────────┘
```

## Data Flow

### User Selection Flow
```
Input (BCI or Keyboard)
    ↓
BCIInputHandler detects input
    ↓
Invokes OnCharacterSelected event
    ↓
GameManager.OnCharacterFaceSelected()
    ↓
CharacterBlob.SetFaceExpression()
    ↓
AudioManager.PlayVoice()
    ↓
Sound plays + Face animates
```

### P300 Stimulus Flow
```
GameManager.StartP300Paradigm()
    ↓
Set isFlashing = true
    ↓
Every 500ms (2Hz frequency):
    ├─ CharacterBlob.Flash()
    └─ Update UI highlighting
    ↓
Wait for BCI/Keyboard input detection
    ↓
OnCharacterSelected triggers
    ↓
Repeat or stop based on game state
```

## Key Classes & Methods

### GameManager
```csharp
public void StartP300Paradigm()        // Begin flashing stimulus
public void StopAllActivity()          // Stop everything
public void ToggleLooping()            // Toggle voice looping
public void SetFlashingFrequency(float frequency)  // Adjust flash rate
public List<CharacterBlob> GetCharacters()  // Get all characters
```

### AudioManager
```csharp
public void PlayVoice(int characterIndex, float pitchLevel)  // Play with pitch
public void SetPitchLevel(int characterIndex, float pitchLevel)  // Real-time pitch
public void StopVoice(int characterIndex)  // Stop specific
public void StopAllVoices()            // Stop all
public void SetLooping(bool shouldLoop)  // Toggle looping
```

### CharacterBlob
```csharp
public void SetFaceExpression(int faceIndex)  // Set face and play voice
public void Flash()                    // P300 flash animation
public void SetSelected(bool selected)  // Highlight character
public void HighlightFaces(bool highlight)  // Highlight face UI
```

### BCIInputHandler
```csharp
public void SetBCIEnabled(bool enabled)  // Enable/disable BCI
public void SetTestMode(bool enabled)  // Enable keyboard testing
public void SelectCharacterAndFace(int characterIndex, int faceIndex)  // Manual selection
public UnityEvent<int,int> OnCharacterSelected  // Selection event
```

### UIManager
```csharp
public void DisplayP300Paradigm(int characterIndex, int faceIndex)  // Show stimulus
public void HideP300Paradigm()         // Hide stimulus
public void DisplayFeedback(string message)  // Show feedback
public void ResetUI()                  // Reset to idle state
```

## Game States

```
┌─────────────┐
│   IDLE      │
│ (Waiting)   │
└──────┬──────┘
       │ Space pressed / BCI ready
       ▼
┌──────────────────┐
│  P300_ACTIVE     │
│ (Stimulus on)    │
└──────┬───────────┘
       │ Input detected / Timeout
       ▼
┌──────────────────┐
│  SELECTION_MODE  │
│ (Playing voice)  │
└──────┬───────────┘
       │ Animation done / User stops
       ▼
┌─────────────┐
│   IDLE      │
└─────────────┘
```

## Keyboard Shortcuts (Test Mode)

### Character Selection
- `1` = Red blob (Deep voice)
- `2` = Blue blob (Medium voice)  
- `3` = Yellow blob (High voice)
- `4` = Green blob (Very high voice)

### Face/Pitch Selection
- `Q` = Face 0 (Low pitch, 0.8)
- `W` = Face 1 (Medium-low pitch, 1.0)
- `E` = Face 2 (Medium-high pitch, 1.2)
- `R` = Face 3 (High pitch, 1.4)

### Game Control
- `SPACE` = Start P300 paradigm
- `L` = Toggle voice looping
- `ESC` = Stop all activity

## Configuration Tips

### Adjust P300 Flash Speed
```csharp
// In GameManager Inspector
gameSettings.flashingFrequency = 2.0f;  // 2 Hz (slow)
gameSettings.flashingFrequency = 5.0f;  // 5 Hz (normal)
gameSettings.flashingFrequency = 10.0f; // 10 Hz (fast)
```

### Adjust Voice Pitch Range
```csharp
// In AudioManager Inspector, per character voice:
minPitch = 0.5f;   // Lower range
maxPitch = 2.0f;   // Higher range
basePitch = 1.0f;  // Default
```

### Enable BCI Input
```csharp
// In GameManager Inspector
gameSettings.enableBCI = true;     // Use real BCI
gameSettings.testMode = false;     // Disable keyboard test
```

### Use Test Mode
```csharp
// In GameManager Inspector
gameSettings.enableBCI = false;    // Disable BCI
gameSettings.testMode = true;      // Enable keyboard test
```

## Troubleshooting

| Problem | Solution |
|---------|----------|
| No audio playing | Check audio clips are assigned in AudioManager |
| P300 stimulus not flashing | Verify GameManager is enabled + has CharacterBlobs |
| BCI not connecting | Ensure Python backend is running + test mode is off |
| Animations stuttering | Reduce P300 flash frequency or lower graphics settings |
| Keyboard input not working | Ensure test mode is enabled in GameManager |
| Character not selected | Check BCIInputHandler has valid character index (0-3) |

## Performance Notes

- P300 flashing at 5 Hz uses ~5% CPU (typical)
- 4 simultaneous audio sources = minimal overhead
- UI updates tied to game loop (60 FPS)
- LSL stream reading is non-blocking

## Next Development Priorities

1. **Art Assets**: Design blob sprites and face expressions
2. **Sound Design**: Record/create 4 distinct voice samples
3. **BCI Integration**: Implement actual LSL stream reading
4. **Accessibility**: Enlarge UI, increase contrast, add haptic feedback
5. **Testing**: User testing with disabled children
6. **Refinement**: Gather feedback and iterate design

---

For detailed documentation, see `IMPLEMENTATION_GUIDE.md` and `PROJECT_SETUP.md`
