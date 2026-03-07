"""
Simulate a marker-locked EEG LSL stream for end-to-end pipeline testing.

What changed vs the original
-----------------------------
The original version injected P300 deflections at a fixed periodic rate,
completely unrelated to the Unity flash markers.  That made the classifier
see random noise aligned to each stimulus flash → no learning was possible.

This version subscribes to the Unity marker stream and injects a realistic
P300 deflection exactly 300–400 ms after every TARGET stimulus flash, so the
classifier can actually learn the target vs. non-target distinction.

Marker format (sent by MindCTRLBCIController / bci-essentials-unity):
    p300,s,{n_stimuli},{target_1idx or -1},{flash_1idx}
    - target_1idx == -1  → testing trial (no known target)
    - target_1idx == flash_1idx → this flash IS the target → inject P300

Run order
---------
    Terminal 1:  python simulate_eeg.py        # start first
    Terminal 2:  python backend.py             # connects to EEG + marker streams
    Unity:       Play → Training → START TRAINING, then START EVALUATION
"""

import threading
import collections
import time

import numpy as np
import pylsl


# ── Stream parameters ────────────────────────────────────────────────────────
N_CHANNELS    = 8
SAMPLE_RATE   = 256           # Hz
CHUNK_SIZE    = 32            # samples per push  (~125 ms per chunk)
CHANNEL_NAMES = ["Fz", "Cz", "Pz", "P3", "P4", "O1", "O2", "Oz"]

# Channels that carry the P300 (centro-parietal, 0-indexed)
P300_CHANNELS = [1, 2, 3, 4]   # Cz, Pz, P3, P4

# Simulated P300 shape
P300_PEAK_LATENCY = 0.340   # seconds post-stimulus
P300_SIGMA        = 0.060   # Gaussian width (seconds)
P300_AMPLITUDE    = 12.0    # µV peak — clearly above the ~5 µV noise floor

# How long to keep a trigger in memory before discarding it
TRIGGER_TTL = 1.5           # seconds


# ── Shared state (main thread writes EEG; listener thread writes triggers) ───
_target_flash_times: collections.deque = collections.deque()
_lock = threading.Lock()


# ── EEG outlet ───────────────────────────────────────────────────────────────

def make_eeg_outlet() -> pylsl.StreamOutlet:
    info = pylsl.StreamInfo(
        name="SimulatedEEG",
        type="EEG",
        channel_count=N_CHANNELS,
        nominal_srate=SAMPLE_RATE,
        channel_format=pylsl.cf_float32,
        source_id="mindctrl_sim",
    )
    channels = info.desc().append_child("channels")
    for name in CHANNEL_NAMES:
        ch = channels.append_child("channel")
        ch.append_child_value("label", name)
        ch.append_child_value("unit", "microvolts")
        ch.append_child_value("type", "EEG")
    return pylsl.StreamOutlet(info)


# ── Marker listener (background thread) ──────────────────────────────────────

def _marker_listener():
    """
    Resolves the Unity marker stream and records the LSL timestamp of every
    target flash so the main thread can inject a time-locked P300.
    """
    print("[SIM] Looking for Unity marker stream (type='BCI_Essentials_Markers')…")
    results = pylsl.resolve_byprop("type", "BCI_Essentials_Markers", 1, 30.0)
    if not results:
        print("[SIM] WARNING: No marker stream found after 30 s.\n"
              "      P300s will NOT be target-locked — start Unity first.")
        return

    inlet = pylsl.StreamInlet(results[0])
    print("[SIM] Connected to marker stream.  P300s will be target-locked.")

    while True:
        sample, ts = inlet.pull_sample(timeout=2.0)
        if sample is None:
            continue

        # Marker format:  p300,s,{n_stim},{target_1idx or -1},{flash_1idx}...
        parts = sample[0].split(",")
        if len(parts) < 5 or parts[0] != "p300" or parts[1] != "s":
            continue

        try:
            target_1idx = int(parts[3])          # -1 means testing trial
            flash_1idx  = int(parts[4])          # stimulus that just flashed
        except ValueError:
            continue

        # Record trigger only for labelled target flashes
        if target_1idx > 0 and target_1idx == flash_1idx:
            with _lock:
                _target_flash_times.append(ts)


def start_marker_listener():
    t = threading.Thread(target=_marker_listener, daemon=True)
    t.start()


# ── EEG generation ────────────────────────────────────────────────────────────

def generate_chunk(t_lsl_start: float) -> np.ndarray:
    """
    Return (N_CHANNELS × CHUNK_SIZE) float32 array whose timestamps span
    [t_lsl_start, t_lsl_start + CHUNK_SIZE/SAMPLE_RATE).

    A Gaussian P300 is added to P300_CHANNELS for each pending target flash
    whose peak falls within or near this chunk.
    """
    dt       = 1.0 / SAMPLE_RATE
    t        = t_lsl_start + np.arange(CHUNK_SIZE) * dt   # LSL timestamps of each sample
    t_end    = t[-1]

    # Background: Gaussian noise
    data = np.random.randn(N_CHANNELS, CHUNK_SIZE).astype(np.float32) * 5.0

    # Alpha oscillation on occipital channels (makes the signal look realistic)
    alpha = 3.0 * np.sin(2 * np.pi * 10 * t).astype(np.float32)
    data[5] += alpha   # O1
    data[6] += alpha   # O2
    data[7] += alpha   # Oz

    # Inject P300 for each target flash still within its time-to-live window
    with _lock:
        # Prune old triggers
        while _target_flash_times and (t_end - _target_flash_times[0]) > TRIGGER_TTL:
            _target_flash_times.popleft()

        for flash_ts in _target_flash_times:
            peak_t = flash_ts + P300_PEAK_LATENCY          # LSL time of P300 peak
            # Gaussian centred on peak_t evaluated at each sample time
            p300 = (P300_AMPLITUDE
                    * np.exp(-((t - peak_t) ** 2) / (2 * P300_SIGMA ** 2))
                    ).astype(np.float32)
            for ch in P300_CHANNELS:
                data[ch] += p300

    return data


# ── Main loop ────────────────────────────────────────────────────────────────

def main():
    print("Starting marker-locked simulated EEG stream…  (Ctrl-C to stop)")
    outlet = make_eeg_outlet()
    print(f"Broadcasting {N_CHANNELS} channels at {SAMPLE_RATE} Hz")

    start_marker_listener()

    chunk_duration = CHUNK_SIZE / SAMPLE_RATE   # seconds per chunk
    t_next_push    = pylsl.local_clock()

    try:
        while True:
            now = pylsl.local_clock()

            # Throttle: push only when the next chunk is due
            if now < t_next_push:
                time.sleep(max(0.0, t_next_push - now - 0.002))
                continue

            chunk = generate_chunk(t_next_push)

            # push_chunk with explicit timestamp for the first sample;
            # pylsl infers the rest from nominal sample rate.
            outlet.push_chunk(chunk.T.tolist(), timestamp=t_next_push)

            t_next_push += chunk_duration

    except KeyboardInterrupt:
        print("\nSimulation stopped.")


if __name__ == "__main__":
    main()
