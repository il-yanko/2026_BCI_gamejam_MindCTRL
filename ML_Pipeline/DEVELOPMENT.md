# ML Pipeline Development Guide

## Overview

The ML Pipeline is designed for P300 ERP signal classification. Developers can create custom ML models that integrate seamlessly with the game.

## Pipeline Architecture

```
EEG Input
    ↓
[Preprocessing]
    ├─ Filter (1-30 Hz bandpass)
    ├─ Re-reference (CAR/LAR)
    └─ Epoch extraction
    ↓
[Feature Extraction]
    ├─ Temporal features (mean, std, peaks)
    ├─ Frequency features (PSD, spectral power)
    ├─ Statistical features
    ├─ Wavelet coefficients
    └─ Domain-specific (P300 latency)
    ↓
[ML Model]
    ├─ Classifier (LDA, SVM, Random Forest, NN)
    ├─ Ensemble methods
    └─ Deep learning (CNN, LSTM, VAE)
    ↓
[Post-Processing]
    ├─ Smoothing (moving average)
    ├─ Confidence scoring
    └─ Outlier rejection
    ↓
Unity Game
(Character Selection)
```

## Model Interface

All ML models must implement `IMLPredictionModel`:

```csharp
public interface IMLPredictionModel
{
    void Initialize(GameConfig.MLSettings settings);
    bool Predict(float[,] eegEpoch, out int charIndex, out int faceIndex, out float confidence);
    void UpdateModel(float[,] eegEpoch, int characterIndex, int faceIndex);
    string GetStatus();
    bool IsReady();
    void Cleanup();
}
```

## Creating a Custom Model

### Step 1: Train in Python

Example with scikit-learn:

```python
# ML_Pipeline/train_p300_model.py
import numpy as np
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import StandardScaler
from sklearn.discriminant_analysis import LinearDiscriminantAnalysis
import joblib

# Load training data
X_train = np.load('data/train_eeg.npy')  # (n_samples, n_channels, n_timepoints)
y_train = np.load('data/train_labels.npy')  # (n_samples,)

# Flatten for 2D classifier
X_train_2d = X_train.reshape(X_train.shape[0], -1)

# Create pipeline
pipeline = Pipeline([
    ('scaler', StandardScaler()),
    ('lda', LinearDiscriminantAnalysis(n_components=4))
])

# Train
pipeline.fit(X_train_2d, y_train)

# Evaluate
score = pipeline.score(X_test_2d, y_test)
print(f"Accuracy: {score:.3f}")

# Save
joblib.dump(pipeline, 'models/p300_classifier.pkl')
```

### Step 2: Export as ONNX (for Unity)

```python
# ML_Pipeline/export_onnx.py
import onnx
import onnxruntime as rt
from sklearn import linear_model
from skl2onnx import convert_sklearn
from skl2onnx.common.data_types import FloatTensorType

# Load trained model
clf = joblib.load('models/p300_classifier.pkl')

# Define input shape
initial_type = [('float_input', FloatTensorType([None, 256]))]  # (batch, features)

# Convert to ONNX
onx = convert_sklearn(clf, initial_types=initial_type)

# Verify
onnx.checker.check_model(onx)

# Save
with open('models/p300_model.onnx', 'wb') as f:
    f.write(onx.SerializeToString())
```

### Step 3: Implement in Unity

```csharp
// Assets/Scripts/Models/P300Classifier.cs
using UnityEngine;

public class P300Classifier : IMLPredictionModel
{
    private float[,] weights;
    private float[] bias;
    private GameConfig.MLSettings settings;

    public void Initialize(GameConfig.MLSettings settings)
    {
        this.settings = settings;
        // Load ONNX model using ONNX Runtime
        // or load pre-trained weights
        LoadModel(settings.modelPath);
    }

    public bool Predict(float[,] eegEpoch, out int charIndex, out int faceIndex, out float confidence)
    {
        // 1. Preprocess: flatten epoch
        float[] features = FlattenEpoch(eegEpoch);
        
        // 2. Extract features
        float[] engineeredFeatures = ExtractFeatures(features);
        
        // 3. Forward pass through model
        float[] predictions = ForwardPass(engineeredFeatures);
        
        // 4. Decode predictions to character and face
        DecodeOutput(predictions, out charIndex, out faceIndex, out confidence);
        
        // 5. Apply threshold
        return confidence >= settings.predictionConfidenceThreshold;
    }

    public void UpdateModel(float[,] eegEpoch, int charIndex, int faceIndex)
    {
        // Optional: implement online learning
    }

    public string GetStatus()
    {
        return "P300 LDA Classifier (8 channels, 256 features)";
    }

    public bool IsReady()
    {
        return weights != null;
    }

    public void Cleanup()
    {
        weights = null;
        bias = null;
    }

    // Helper methods...
}
```

## Data Requirements

### Training Data Format

```
X: (n_samples, n_channels, n_timepoints)
y: (n_samples,)  # Labels: 0-15 (4 characters × 4 faces)

Example:
- 1000 trials
- 8 EEG channels
- 250 Hz sampling rate
- 1 second window = 250 timepoints per trial
```

### Preprocessing Pipeline

1. **Bandpass Filter**: 1-30 Hz (removes artifacts and low-frequency drift)
2. **Artifact Rejection**: Remove trials with excessive noise
3. **Baseline Correction**: Subtract mean of pre-stimulus window
4. **Normalization**: Z-score or min-max scaling

## Feature Engineering Examples

### Time-Domain Features
```python
def extract_temporal_features(epoch):
    """Extract temporal statistics from EEG epoch."""
    features = np.concatenate([
        np.mean(epoch, axis=1),      # Mean
        np.std(epoch, axis=1),       # Std dev
        np.max(epoch, axis=1),       # Max
        np.min(epoch, axis=1),       # Min
        np.ptp(epoch, axis=1)        # Peak-to-peak
    ])
    return features
```

### Frequency-Domain Features
```python
from scipy import signal

def extract_frequency_features(epoch, fs=250):
    """Extract power spectral density features."""
    features = []
    for channel in epoch:
        f, pxx = signal.welch(channel, fs=fs, nperseg=256)
        # Extract power in frequency bands
        delta = np.mean(pxx[(f >= 1) & (f < 4)])
        theta = np.mean(pxx[(f >= 4) & (f < 8)])
        alpha = np.mean(pxx[(f >= 8) & (f < 13)])
        beta = np.mean(pxx[(f >= 13) & (f < 30)])
        features.extend([delta, theta, alpha, beta])
    return np.array(features)
```

### P300-Specific Features
```python
def extract_p300_features(epoch, fs=250):
    """Extract P300-specific features."""
    # P300 typically appears 300ms after stimulus
    p300_window = slice(int(0.3*fs), int(0.5*fs))
    
    features = []
    for channel in epoch:
        p300_signal = channel[p300_window]
        features.extend([
            np.max(p300_signal),      # Peak amplitude
            np.argmax(p300_signal),   # Latency
            np.mean(p300_signal),     # Mean amplitude
        ])
    return np.array(features)
```

## Model Training Best Practices

### 1. Class Imbalance
P300 responses are rare (only true detections produce strong signals).

```python
from sklearn.utils.class_weight import compute_class_weight

weights = compute_class_weight('balanced', 
                               classes=np.unique(y_train), 
                               y=y_train)
```

### 2. Cross-Validation
Use time-aware cross-validation (don't mix same session in train/test):

```python
from sklearn.model_selection import GroupKFold

gkf = GroupKFold(n_splits=5)
for train_idx, test_idx in gkf.split(X, groups=session_ids):
    # Train and test
```

### 3. Hyperparameter Tuning

```python
from sklearn.model_selection import GridSearchCV

param_grid = {
    'lda__n_components': [2, 4, 8],
    'lda__solver': ['svd', 'lsqr', 'eigen']
}

grid = GridSearchCV(pipeline, param_grid, cv=5)
grid.fit(X_train_2d, y_train)
```

## Testing the Model

### In Python

```python
# Test with mock data
import numpy as np

X_test = np.random.randn(10, 8, 250)  # 10 trials, 8 channels, 250 samples
y_pred = model.predict(X_test)
print(f"Predictions: {y_pred}")
```

### In Unity

1. Set `GameConfig.enableMLPrediction = true`
2. Set `GameConfig.modelPath = "Assets/Data/Models/p300_model.onnx"`
3. Implement your model class
4. Set in `MLPredictionManager.SetMLModel(your_model)`
5. Play game and monitor console logs

## Performance Metrics

### Session-Based Accuracy
```python
# Correct predictions per session
accuracy_per_session = np.mean(y_pred == y_true)
```

### Character-Wise Accuracy
```python
# Accuracy for each of 4 characters
for char in range(4):
    mask = y_true // 4 == char
    acc = np.mean(y_pred[mask] == y_true[mask])
    print(f"Character {char}: {acc:.3f}")
```

### Information Transfer Rate (ITR)
```python
# Bits per minute
from scipy.special import comb

def calculate_itr(accuracy, n_targets, trial_duration):
    """
    accuracy: classification accuracy (0-1)
    n_targets: number of possible selections (16)
    trial_duration: time for one selection (seconds)
    """
    if accuracy == 0 or accuracy == 1:
        return 0
    
    p = accuracy
    log_N = np.log2(n_targets)
    itr = (p * np.log2(p) + (1-p) * np.log2((1-p) / (n_targets - 1)) + np.log2(n_targets))
    itr *= 60.0 / trial_duration  # Convert to bits per minute
    return max(0, itr)
```

## Optimization for Unity

### ONNX Runtime
For CPU inference in Unity:

```csharp
using Microsoft.ML.OnnxRuntime;

public class ONNXModel : IMLPredictionModel
{
    private InferenceSession session;

    public void Initialize(GameConfig.MLSettings settings)
    {
        session = new InferenceSession(settings.modelPath);
    }

    public bool Predict(float[,] eegEpoch, out int charIndex, 
                        out int faceIndex, out float confidence)
    {
        // Prepare input
        var input = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor<float>(
                "input",
                new DenseTensor<float>(FlattenEpoch(eegEpoch)))
        };

        // Run inference
        var results = session.Run(input);
        var output = results[0].AsTensor<float>().ToArray();

        // Decode output
        DecodeOutput(output, out charIndex, out faceIndex, out confidence);
        return confidence >= 0.7f;
    }

    // ... other methods
}
```

## File Structure

```
ML_Pipeline/
├── notebooks/
│   ├── 01_data_exploration.ipynb
│   ├── 02_preprocessing.ipynb
│   ├── 03_feature_engineering.ipynb
│   ├── 04_model_training.ipynb
│   └── 05_evaluation.ipynb
├── models/
│   ├── p300_classifier.pkl
│   ├── p300_model.onnx
│   └── README.md
├── data/
│   ├── raw/
│   ├── processed/
│   └── README.md
├── train_p300_model.py
├── export_onnx.py
├── evaluate_model.py
└── DEVELOPMENT.md
```

## Next Steps

1. **Collect Data**: Run MindCTRL game and save EEG + labels
2. **Preprocess**: Filter, epoch extraction, artifact removal
3. **Extract Features**: Temporal, spectral, P300-specific
4. **Train Model**: Using scikit-learn or PyTorch
5. **Export ONNX**: For Unity integration
6. **Implement IMLPredictionModel**: C# wrapper
7. **Test in Unity**: Enable ML and validate accuracy
8. **Deploy**: Build standalone with trained model

---

**Questions? See README.md and DEVELOPER_SETUP.md**
