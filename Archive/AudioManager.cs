using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// AudioManager handles all voice playback and pitch management for the BCI game.
/// Manages 4 different character voices with real-time pitch adjustment.
/// </summary>
public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public class CharacterVoice
    {
        public string characterName;
        public AudioClip voiceClip;
        public float basePitch = 1.0f; // Base pitch (1.0 = normal)
        [Range(0.5f, 2.0f)] public float minPitch = 0.8f;
        [Range(0.5f, 2.0f)] public float maxPitch = 1.5f;
    }

    private static AudioManager instance;
    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AudioManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("AudioManager");
                    instance = obj.AddComponent<AudioManager>();
                }
            }
            return instance;
        }
    }

    [SerializeField] private List<CharacterVoice> characterVoices = new();
    [SerializeField] private float maxVolume = 0.8f;
    [SerializeField] private bool useLooping = false;

    private Dictionary<int, AudioSource> activeAudioSources = new();
    private Dictionary<int, float> currentPitches = new();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
    }

    private void InitializeAudioSources()
    {
        for (int i = 0; i < characterVoices.Count; i++)
        {
            GameObject audioObj = new GameObject($"AudioSource_{i}");
            audioObj.transform.SetParent(transform);
            AudioSource audioSource = audioObj.AddComponent<AudioSource>();
            audioSource.clip = characterVoices[i].voiceClip;
            audioSource.volume = maxVolume;
            audioSource.loop = useLooping;
            activeAudioSources[i] = audioSource;
            currentPitches[i] = characterVoices[i].basePitch;
        }
    }

    /// <summary>
    /// Plays the voice for a specific character with optional pitch adjustment.
    /// </summary>
    public void PlayVoice(int characterIndex, float pitchLevel = 1.0f)
    {
        if (!IsValidCharacterIndex(characterIndex)) return;

        AudioSource source = activeAudioSources[characterIndex];
        float pitch = Mathf.Clamp(pitchLevel, 
            characterVoices[characterIndex].minPitch, 
            characterVoices[characterIndex].maxPitch);

        currentPitches[characterIndex] = pitch;
        source.pitch = pitch;

        if (!source.isPlaying)
            source.Play();
    }

    /// <summary>
    /// Adjusts pitch in real-time for continuous playback.
    /// </summary>
    public void SetPitchLevel(int characterIndex, float pitchLevel)
    {
        if (!IsValidCharacterIndex(characterIndex)) return;

        currentPitches[characterIndex] = pitchLevel;
        activeAudioSources[characterIndex].pitch = pitchLevel;
    }

    /// <summary>
    /// Stops playback for a specific character.
    /// </summary>
    public void StopVoice(int characterIndex)
    {
        if (!IsValidCharacterIndex(characterIndex)) return;
        activeAudioSources[characterIndex].Stop();
    }

    /// <summary>
    /// Stops all character voices.
    /// </summary>
    public void StopAllVoices()
    {
        foreach (var source in activeAudioSources.Values)
        {
            source.Stop();
        }
    }

    /// <summary>
    /// Sets whether voices should loop continuously.
    /// </summary>
    public void SetLooping(bool shouldLoop)
    {
        useLooping = shouldLoop;
        foreach (var source in activeAudioSources.Values)
        {
            source.loop = shouldLoop;
        }
    }

    public float GetCurrentPitch(int characterIndex)
    {
        return IsValidCharacterIndex(characterIndex) ? currentPitches[characterIndex] : 1.0f;
    }

    public int GetCharacterCount() => characterVoices.Count;

    private bool IsValidCharacterIndex(int index) => index >= 0 && index < characterVoices.Count;
}
