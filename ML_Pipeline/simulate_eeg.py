"""
Simulate an EEG LSL stream for testing the backend without real hardware.

Broadcasts a synthetic 8-channel EEG signal at 256 Hz with injected P300
deflections — enough to verify the full pipeline end-to-end.

Run this in a separate terminal BEFORE backend.py:

    cd ML_Pipeline
    python simulate_eeg.py
"""

import time
import numpy as np
import pylsl


# ── Stream parameters ──────────────────────────────────────────────────────
N_CHANNELS   = 8
SAMPLE_RATE  = 256          # Hz
CHUNK_SIZE   = 32           # samples per push
CHANNEL_NAMES = ["Fz", "Cz", "Pz", "P3", "P4", "O1", "O2", "Oz"]


def make_outlet() -> pylsl.StreamOutlet:
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


def generate_chunk(t_offset: float, p300_active: bool) -> np.ndarray:
    """Return (N_CHANNELS, CHUNK_SIZE) float32 array."""
    t = t_offset + np.arange(CHUNK_SIZE) / SAMPLE_RATE
    noise = np.random.randn(N_CHANNELS, CHUNK_SIZE).astype(np.float32) * 5.0

    # Background alpha oscillation on occipital channels
    alpha = 3.0 * np.sin(2 * np.pi * 10 * t)
    noise[5] += alpha   # O1
    noise[6] += alpha   # O2
    noise[7] += alpha   # Oz

    # Inject a P300-like deflection on centro-parietal channels
    if p300_active:
        p300 = 8.0 * np.exp(-((t - t_offset - 0.35) ** 2) / (2 * 0.04 ** 2))
        for ch in [1, 2, 3, 4]:   # Cz, Pz, P3, P4
            noise[ch] += p300.astype(np.float32)

    return noise


def main():
    print("Starting simulated EEG stream…  (Ctrl-C to stop)")
    outlet = make_outlet()
    print(f"Broadcasting '{CHANNEL_NAMES}' at {SAMPLE_RATE} Hz")

    chunk_duration = CHUNK_SIZE / SAMPLE_RATE
    t_offset = 0.0
    p300_counter = 0

    try:
        while True:
            # Inject a P300 every ~350 ms to simulate a target response
            p300_counter += 1
            p300_active = (p300_counter % max(1, int(0.35 / chunk_duration))) == 0

            chunk = generate_chunk(t_offset, p300_active)
            # pylsl expects list-of-samples (time × channels)
            outlet.push_chunk(chunk.T.tolist())

            t_offset += chunk_duration
            time.sleep(chunk_duration)
    except KeyboardInterrupt:
        print("\nSimulation stopped.")


if __name__ == "__main__":
    main()
