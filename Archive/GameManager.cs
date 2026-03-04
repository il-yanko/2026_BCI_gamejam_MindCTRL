using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// GameManager orchestrates the entire BCI game.
/// Manages character blobs, input handling, and game state.
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("GameManager");
                    instance = obj.AddComponent<GameManager>();
                }
            }
            return instance;
        }
    }

    [System.Serializable]
    public class GameSettings
    {
        public bool enableLooping = false;
        public bool enableBCI = true;
        public bool testMode = false;
        public float flashingFrequency = 2.0f; // P300 stimulus frequency in Hz
    }

    [SerializeField] private GameSettings gameSettings = new();
    [SerializeField] private List<CharacterBlob> characters = new();
    [SerializeField] private Canvas gameCanvas;
    [SerializeField] private float p300FlashDuration = 0.5f;

    private int selectedCharacterIndex = -1;
    private int selectedFaceIndex = -1;
    private bool isFlashing = false;
    private float flashTimer = 0f;

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
        }
    }

    private void Start()
    {
        InitializeGame();
    }

    /// <summary>
    /// Initialize game components and settings.
    /// </summary>
    private void InitializeGame()
    {
        Debug.Log("Initializing MindCTRL Game...");

        // Find all character blobs if not assigned
        if (characters.Count == 0)
        {
            characters.AddRange(FindObjectsOfType<CharacterBlob>());
            Debug.Log($"Found {characters.Count} character blobs");
        }

        // Setup audio manager
        AudioManager audioManager = AudioManager.Instance;
        audioManager.SetLooping(gameSettings.enableLooping);

        // Setup BCI input handler
        BCIInputHandler bciHandler = BCIInputHandler.Instance;
        bciHandler.OnCharacterSelected.AddListener(OnCharacterFaceSelected);
        bciHandler.SetBCIEnabled(gameSettings.enableBCI);
        bciHandler.SetTestMode(gameSettings.testMode);

        Debug.Log("Game initialized successfully");
    }

    private void Update()
    {
        if (isFlashing)
        {
            UpdateFlashing();
        }

        // Debug keyboard shortcuts
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartP300Paradigm();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StopAllActivity();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            ToggleLooping();
        }
    }

    /// <summary>
    /// Start P300 flashing stimulus paradigm.
    /// </summary>
    public void StartP300Paradigm()
    {
        if (isFlashing) return;

        Debug.Log("Starting P300 paradigm");
        isFlashing = true;
        flashTimer = 0f;
        FlashCharacters();
    }

    /// <summary>
    /// Stop all game activity.
    /// </summary>
    public void StopAllActivity()
    {
        isFlashing = false;
        AudioManager.Instance.StopAllVoices();
        foreach (var character in characters)
        {
            character.SetSelected(false);
        }
        Debug.Log("All activity stopped");
    }

    /// <summary>
    /// Flash all characters for P300 stimulus.
    /// </summary>
    private void FlashCharacters()
    {
        foreach (var character in characters)
        {
            character.Flash();
        }
    }

    /// <summary>
    /// Update P300 flashing loop.
    /// </summary>
    private void UpdateFlashing()
    {
        flashTimer += Time.deltaTime;

        float flashInterval = 1.0f / gameSettings.flashingFrequency;
        if (flashTimer >= flashInterval)
        {
            flashTimer = 0f;
            FlashCharacters();
        }
    }

    /// <summary>
    /// Handle character and face selection from BCI input.
    /// </summary>
    private void OnCharacterFaceSelected(int characterIndex, int faceIndex)
    {
        if (characterIndex < 0 || characterIndex >= characters.Count)
        {
            Debug.LogWarning($"Invalid character index: {characterIndex}");
            return;
        }

        if (faceIndex < 0 || faceIndex >= 4)
        {
            Debug.LogWarning($"Invalid face index: {faceIndex}");
            return;
        }

        selectedCharacterIndex = characterIndex;
        selectedFaceIndex = faceIndex;

        Debug.Log($"Selected Character {characterIndex}, Face {faceIndex}");

        // Play the selected character's face
        CharacterBlob selectedCharacter = characters[characterIndex];
        selectedCharacter.SetFaceExpression(faceIndex);
        selectedCharacter.SetSelected(true);

        // Visual feedback
        StartCoroutine(ShowSelectionFeedback(selectedCharacter));
    }

    /// <summary>
    /// Show visual feedback after selection.
    /// </summary>
    private System.Collections.IEnumerator ShowSelectionFeedback(CharacterBlob character)
    {
        yield return new WaitForSeconds(1.0f);
        character.SetSelected(false);
    }

    /// <summary>
    /// Toggle voice looping on/off.
    /// </summary>
    public void ToggleLooping()
    {
        gameSettings.enableLooping = !gameSettings.enableLooping;
        AudioManager.Instance.SetLooping(gameSettings.enableLooping);
        Debug.Log($"Looping: {gameSettings.enableLooping}");
    }

    /// <summary>
    /// Set P300 flashing frequency.
    /// </summary>
    public void SetFlashingFrequency(float frequency)
    {
        gameSettings.flashingFrequency = Mathf.Clamp(frequency, 0.5f, 10.0f);
        Debug.Log($"Flash frequency set to {gameSettings.flashingFrequency} Hz");
    }

    // Getters
    public int GetSelectedCharacterIndex() => selectedCharacterIndex;
    public int GetSelectedFaceIndex() => selectedFaceIndex;
    public List<CharacterBlob> GetCharacters() => characters;
    public GameSettings GetSettings() => gameSettings;
}
