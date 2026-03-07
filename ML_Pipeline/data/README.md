# Data

Put your recorded `.xdf` session files here.

## How to record a session

1. Install **LabRecorder**: https://github.com/labstreaminglayer/App-LabRecorder/releases
2. Start your EEG headset driver (broadcasts an EEG LSL stream)
3. Start Unity and open the Training panel
4. In LabRecorder, select both streams:
   - `SimulatedEEG` or your headset stream (type: `EEG`)
   - `UnityMarkerStream` (type: `BCI_Essentials_Markers`)
5. Click **Start** in LabRecorder, complete the training session, click **Stop**
6. Move the saved `.xdf` file into this folder

## Naming convention

```
session_YYYYMMDD_NNN.xdf       e.g.  session_20260305_001.xdf
```

## Files tracked by git

`.xdf` files are excluded by `.gitignore` (they can be hundreds of MB).
Commit only aggregated results or small processed `.npy` arrays.
