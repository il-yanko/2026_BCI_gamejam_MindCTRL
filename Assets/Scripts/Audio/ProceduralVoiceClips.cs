using UnityEngine;

/// <summary>
/// Generates funny procedural looping voice clips at runtime.
///
/// Each character has a distinct cartoon-voice personality shaped by its
/// harmonic mix and base frequency.  Higher pitch levels pile on vibrato,
/// tremolo and frequency chaos until the Yelling clip becomes a gloriously
/// wobbly squawk.
///
/// Usage:
///   AudioClip[] clips = ProceduralVoiceClips.GenerateForCharacter(charIdx);
///   // or fill individual null slots:
///   clips[p] = ProceduralVoiceClips.Generate(charIdx, p);
/// </summary>
public static class ProceduralVoiceClips
{
    const int   SampleRate = 44100;
    const float Duration   = 2f;   // clip length in seconds — loopable

    // ── Per-character voice personality ───────────────────────────────────────

    // Fundamental frequency (Hz) for pitch level 0 (Calm)
    static readonly float[] BaseFreq =
    {
        260f,   // Red    — mid tenor, bright
        185f,   // Green  — low, bubbly
        330f,   // Blue   — high, reedy
        145f,   // Yellow — very low then jumps to funny falsetto at high levels
    };

    // Harmonic weights [1st … 5th overtone]
    // Shapes the vowel colour: more upper harmonics = brighter / more nasal
    static readonly float[][] Harmonics =
    {
        new[] { 1.00f, 0.65f, 0.25f, 0.10f, 0.05f },  // Red    — round, vowel-ish
        new[] { 1.00f, 0.25f, 0.70f, 0.20f, 0.35f },  // Green  — nasal/kazoo
        new[] { 1.00f, 0.80f, 0.08f, 0.55f, 0.12f },  // Blue   — hollow, tuba-like
        new[] { 1.00f, 0.10f, 0.90f, 0.45f, 0.70f },  // Yellow — reedy / squeaky violin
    };

    // ── Per-pitch-level animation parameters ──────────────────────────────────

    // Pitch multiplier:  Calm=unison, Happy=+4 semitones, Excited=+8 st, Yelling=+octave
    static readonly float[] PitchMult  = { 1.00f, 1.26f, 1.587f, 2.00f };

    // Vibrato rate (Hz) — how fast the pitch wobbles
    static readonly float[] VibRate    = { 4.5f,  6.0f,  10.0f,  19.0f };

    // Vibrato depth (fraction of frequency) — how far pitch deviates
    static readonly float[] VibDepth   = { 0.010f, 0.040f, 0.090f, 0.240f };

    // Tremolo rate (Hz) — how fast the volume pumps
    static readonly float[] TremRate   = { 0.0f,  3.5f,  9.0f,   24.0f };

    // Tremolo depth (0-1) — how much the volume dips
    static readonly float[] TremDepth  = { 0.00f, 0.20f, 0.42f,  0.70f };

    // Extra frequency chaos added to Excited/Yelling (Perlin noise on pitch)
    static readonly float[] ChaosMag   = { 0.00f, 0.00f, 0.04f,  0.12f };

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Returns four looping funny clips for one character.</summary>
    public static AudioClip[] GenerateForCharacter(int charIdx)
    {
        var clips = new AudioClip[4];
        for (int p = 0; p < 4; p++)
            clips[p] = Generate(charIdx, p);
        return clips;
    }

    /// <summary>Generates one looping funny clip.</summary>
    public static AudioClip Generate(int charIdx, int pitchIdx)
    {
        int   n    = (int)(SampleRate * Duration);
        float freq = BaseFreq[charIdx] * PitchMult[pitchIdx];
        float vr   = VibRate  [pitchIdx];
        float vd   = VibDepth [pitchIdx];
        float tr   = TremRate [pitchIdx];
        float td   = TremDepth[pitchIdx];
        float cm   = ChaosMag [pitchIdx];
        float[] h  = Harmonics[charIdx];

        float[] data  = new float[n];
        float   phase = 0f;

        // Pre-bake a noise table for cheap per-sample frequency chaos
        float[] noiseA = BakeNoise(n, charIdx * 7 + pitchIdx);

        // Portamento: slide up from 60 % of pitch over the first 80 ms
        float slideEnd = SampleRate * 0.08f;

        for (int i = 0; i < n; i++)
        {
            float t        = (float)i / SampleRate;
            float slideMul = i < slideEnd ? Mathf.Lerp(0.60f, 1f, i / slideEnd) : 1f;

            // Vibrato (sinusoidal frequency modulation)
            float vibMul  = 1f + vd * Mathf.Sin(Mathf.PI * 2f * vr * t);

            // Perlin-noise frequency chaos (Excited / Yelling only)
            float chaos   = 1f + cm * (noiseA[i] * 2f - 1f);

            float instFreq = freq * slideMul * vibMul * chaos;

            // Accumulate phase
            phase += Mathf.PI * 2f * instFreq / SampleRate;
            if (phase > Mathf.PI * 2f) phase -= Mathf.PI * 2f;

            // Sum harmonics → shapes the vowel / timbre
            float sample = 0f, weight = 0f;
            for (int k = 0; k < h.Length; k++)
            {
                sample += h[k] * Mathf.Sin(phase * (k + 1));
                weight += h[k];
            }
            sample /= weight;

            // Tremolo (amplitude modulation)
            float trem = 1f - td * (0.5f + 0.5f * Mathf.Sin(Mathf.PI * 2f * tr * t));
            sample *= trem;

            // Soft-clip to keep peaks warm rather than harsh
            data[i] = SoftClip(sample * 0.78f);
        }

        // 20 ms crossfade at both ends so the loop is click-free
        CrossfadeEnds(data, (int)(SampleRate * 0.02f));

        var clip = AudioClip.Create($"Voice_C{charIdx}_P{pitchIdx}", n, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>Bake a smoothly varying noise array using Perlin noise.</summary>
    static float[] BakeNoise(int length, int seed)
    {
        var arr    = new float[length];
        float xOff = seed * 3.71f;
        // Perlin noise is sampled slowly so the pitch doesn't jitter too fast
        float xStep = 4f / SampleRate;
        for (int i = 0; i < length; i++)
            arr[i] = Mathf.PerlinNoise(xOff + i * xStep, 0f);
        return arr;
    }

    /// <summary>Smooth tanh-style soft clipper.</summary>
    static float SoftClip(float x)
    {
        // Cheap approximation of tanh that preserves loudness
        if (x >  1f) return  1f;
        if (x < -1f) return -1f;
        return x * (1.5f - 0.5f * x * x);
    }

    /// <summary>Fade the first and last N samples to silence to avoid loop clicks.</summary>
    static void CrossfadeEnds(float[] data, int fadeLen)
    {
        int n = data.Length;
        for (int i = 0; i < fadeLen && i < n / 2; i++)
        {
            float t = (float)i / fadeLen;
            data[i]         *= t;
            data[n - 1 - i] *= t;
        }
    }
}
