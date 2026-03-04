using UnityEngine;

/// <summary>
/// Singleton that holds all runtime configuration flags and settings.
/// Access via GameConfig.Instance anywhere in the project.
/// </summary>
public class GameConfig : MonoBehaviour
{
    public static GameConfig Instance { get; private set; }

    [Header("BCI Settings")]
    [Tooltip("Use keyboard-driven mock instead of real BCI hardware")]
    public bool useMockBCI = true;

    [Header("Audio Settings")]
    public bool enableAudio = true;
    [Range(0f, 1f)] public float masterVolume = 1f;

    [Header("BCI Trial Parameters")]
    [Tooltip("Number of times each stimulus is flashed per classification")]
    public int flashesPerOption = 10;
    public float onTime  = 0.1f;
    public float offTime = 0.075f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Convenience accessors
    public bool  GetUseMockBCI()     => useMockBCI;
    public bool  GetEnableAudio()    => enableAudio;
    public float GetMasterVolume()   => masterVolume;
    public int   GetFlashesPerOption() => flashesPerOption;

    public void SetAudioEnabled(bool v) { enableAudio  = v; }
    public void SetMasterVolume(float v){ masterVolume = Mathf.Clamp01(v); }
}
