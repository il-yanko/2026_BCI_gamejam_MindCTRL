"""
Hardware / stream diagnostic for MindCTRL.

Run this BEFORE backend.py to verify everything is wired up correctly.

    cd ML_Pipeline
    python check_streams.py

Checks performed
----------------
1. EEG stream   — present, correct sample rate, signal not flat / not clipping
2. Marker stream — present, marker format matches what the classifier expects
3. Timing        — EEG and marker clocks are close (< 50 ms offset)

Exit codes
----------
  0  all checks passed
  1  one or more checks failed
"""

import sys
import time
import numpy as np
import pylsl


# ── Tuneable thresholds ────────────────────────────────────────────────────
RESOLVE_TIMEOUT   = 5.0    # seconds to wait for each stream
CAPTURE_DURATION  = 3.0    # seconds of EEG to capture for signal checks
CLIP_THRESHOLD    = 500.0  # µV — above this we call a channel clipped
FLAT_THRESHOLD    = 0.05   # µV std — below this we call a channel flat
MIN_SAMPLE_RATE   = 128    # Hz minimum acceptable
CLOCK_TOLERANCE   = 0.050  # seconds — max acceptable EEG/marker clock offset

PASS = "\033[92m[PASS]\033[0m"
FAIL = "\033[91m[FAIL]\033[0m"
WARN = "\033[93m[WARN]\033[0m"
INFO = "\033[94m[INFO]\033[0m"


def resolve(stream_type: str, timeout: float) -> pylsl.StreamInfo | None:
    print(f"{INFO} Looking for LSL stream (type='{stream_type}')…", flush=True)
    results = pylsl.resolve_byprop("type", stream_type, 1, timeout)
    if not results:
        print(f"{FAIL} No stream found with type='{stream_type}' after {timeout}s")
        return None
    info = results[0]
    print(f"{PASS} Found: '{info.name()}' | {info.channel_count()} ch "
          f"| {info.nominal_srate():.0f} Hz | source_id={info.source_id()}")
    return info


def check_eeg(info: pylsl.StreamInfo) -> bool:
    ok = True

    # Sample rate
    sr = info.nominal_srate()
    if sr < MIN_SAMPLE_RATE:
        print(f"{FAIL} Sample rate {sr:.0f} Hz < minimum {MIN_SAMPLE_RATE} Hz")
        ok = False
    else:
        print(f"{PASS} Sample rate {sr:.0f} Hz")

    # Capture a few seconds of data
    inlet = pylsl.StreamInlet(info, max_buflen=30)
    inlet.open_stream()
    print(f"{INFO} Capturing {CAPTURE_DURATION}s of EEG…", flush=True)

    samples, timestamps = [], []
    deadline = time.time() + CAPTURE_DURATION
    while time.time() < deadline:
        chunk, ts = inlet.pull_chunk(timeout=0.1)
        if chunk:
            samples.extend(chunk)
            timestamps.extend(ts)
    inlet.close_stream()

    if not samples:
        print(f"{FAIL} No samples received during capture window")
        return False

    data = np.array(samples)        # shape: (n_samples, n_channels)
    n_samples, n_ch = data.shape
    actual_sr = n_samples / CAPTURE_DURATION
    print(f"{INFO} Received {n_samples} samples across {n_ch} channels "
          f"(effective {actual_sr:.0f} Hz)")

    # Per-channel checks
    any_flat = False
    any_clip = False
    for ch in range(n_ch):
        col  = data[:, ch]
        std  = float(np.std(col))
        peak = float(np.max(np.abs(col)))
        if std < FLAT_THRESHOLD:
            print(f"{WARN} Channel {ch:2d}: FLAT  (std={std:.3f} µV)")
            any_flat = True
        elif peak > CLIP_THRESHOLD:
            print(f"{WARN} Channel {ch:2d}: CLIP  (peak={peak:.1f} µV)")
            any_clip = True
        else:
            print(f"      Channel {ch:2d}: ok    (std={std:.2f} µV, peak={peak:.1f} µV)")

    if any_flat:
        print(f"{WARN} Flat channels detected — check electrode contact / gel")
    if any_clip:
        print(f"{WARN} Clipping detected — check amplifier gain / electrode placement")
    if not any_flat and not any_clip:
        print(f"{PASS} All channels within normal range")

    return ok


def check_markers(eeg_info: pylsl.StreamInfo) -> bool:
    ok = True

    # Resolve marker stream by type
    print(f"\n{INFO} Looking for Unity marker stream…", flush=True)
    results = pylsl.resolve_byprop("type", "BCI_Essentials_Markers", 1, RESOLVE_TIMEOUT)
    if not results:
        print(f"{FAIL} No marker stream found (type='BCI_Essentials_Markers')")
        print(f"      → Is Unity running and has 'New Game' been pressed?")
        return False

    marker_info = results[0]
    print(f"{PASS} Found: '{marker_info.name()}'")

    # Clock offset between EEG and marker streams
    inlet = pylsl.StreamInlet(marker_info)
    offset = inlet.time_correction()
    if abs(offset) > CLOCK_TOLERANCE:
        print(f"{WARN} Clock offset EEG↔markers: {offset*1000:.1f} ms "
              f"(> {CLOCK_TOLERANCE*1000:.0f} ms tolerance)")
    else:
        print(f"{PASS} Clock offset EEG↔markers: {offset*1000:.1f} ms")

    # Wait briefly for a marker
    print(f"{INFO} Waiting up to {RESOLVE_TIMEOUT}s for a P300 flash marker…")
    inlet.open_stream()
    deadline = time.time() + RESOLVE_TIMEOUT
    received = []
    while time.time() < deadline:
        sample, _ = inlet.pull_sample(timeout=0.2)
        if sample:
            received.append(sample[0])
            if len(received) >= 3:
                break
    inlet.close_stream()

    if not received:
        print(f"{WARN} No markers received — start the game and begin a trial "
              f"to see flash markers here")
        return ok   # not a hard failure yet

    for m in received:
        print(f"      Marker: {m!r}")
        if not m.startswith("p300,s,"):
            print(f"{FAIL} Unexpected marker format. Expected 'p300,s,…'")
            ok = False
        else:
            parts = m.split(",")
            n_stim = int(parts[2])
            if n_stim != 17:
                print(f"{WARN} n_stimuli={n_stim}, expected 17 "
                      f"(16 pitch buttons + 1 play/pause)")
            else:
                print(f"{PASS} Marker format correct (17 stimuli)")

    return ok


def main() -> int:
    print("=" * 60)
    print("  MindCTRL — hardware / stream diagnostic")
    print("=" * 60)

    all_ok = True

    # ── EEG stream ─────────────────────────────────────────────
    print("\n── EEG Stream ──────────────────────────────────────────")
    eeg_info = resolve("EEG", RESOLVE_TIMEOUT)
    if eeg_info is None:
        print(f"      → Start your headset driver and try again")
        all_ok = False
    else:
        if not check_eeg(eeg_info):
            all_ok = False

    # ── Marker stream ──────────────────────────────────────────
    print("\n── Unity Marker Stream ─────────────────────────────────")
    if not check_markers(eeg_info):
        all_ok = False

    # ── Summary ────────────────────────────────────────────────
    print("\n" + "=" * 60)
    if all_ok:
        print(f"{PASS} All checks passed — ready to run backend.py")
    else:
        print(f"{FAIL} Some checks failed — see output above")
    print("=" * 60)

    return 0 if all_ok else 1


if __name__ == "__main__":
    sys.exit(main())
