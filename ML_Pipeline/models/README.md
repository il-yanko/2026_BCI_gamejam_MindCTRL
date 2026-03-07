# Models

Trained classifiers are saved here.

## Files

| File | Description |
|------|-------------|
| `p300_classifier.joblib` | Fitted `ErpRgClassifier` (XDawn + Riemannian + LDA) |

## Saving a model

```bash
python train_offline.py data/session_001.xdf --save models/p300_classifier.joblib
```

## Loading a model

```python
import joblib
clf = joblib.load("models/p300_classifier.joblib")
```

## Git policy

Large trained models (> 10 MB) should be stored with **Git LFS** or excluded.
Check the `.gitignore` before committing.
