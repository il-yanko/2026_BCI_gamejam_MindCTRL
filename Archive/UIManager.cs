using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UIManager handles the P300 paradigm display and user interface.
/// Manages the layout and highlighting of characters and faces.
/// </summary>
public class UIManager : MonoBehaviour
{
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private GridLayoutGroup characterGrid;
    [SerializeField] private float gridSpacing = 20f;
    [SerializeField] private Vector2 characterGridSize = new Vector2(800, 600);

    private Dictionary<int, List<Image>> faceUIElements = new();
    private bool isInitialized = false;

    private void Start()
    {
        InitializeUI();
    }

    /// <summary>
    /// Initialize UI elements for all characters and faces.
    /// </summary>
    private void InitializeUI()
    {
        if (isInitialized) return;

        // Get game manager and characters
        GameManager gameManager = GameManager.Instance;
        List<CharacterBlob> characters = gameManager.GetCharacters();

        if (characters.Count == 0)
        {
            Debug.LogWarning("No characters found in GameManager");
            return;
        }

        // Setup grid layout if exists
        if (characterGrid != null)
        {
            characterGrid.cellSize = new Vector2(characterGridSize.x / 2, characterGridSize.y / 2);
            characterGrid.spacing = new Vector2(gridSpacing, gridSpacing);
        }

        // Initialize face UI elements dictionary
        for (int i = 0; i < characters.Count; i++)
        {
            faceUIElements[i] = new List<Image>();
        }

        isInitialized = true;
        Debug.Log("UI initialized successfully");
    }

    /// <summary>
    /// Display the P300 paradigm - highlight characters and faces for flashing.
    /// </summary>
    public void DisplayP300Paradigm(int characterIndex = -1, int faceIndex = -1)
    {
        GameManager gameManager = GameManager.Instance;
        List<CharacterBlob> characters = gameManager.GetCharacters();

        if (characterIndex >= 0 && characterIndex < characters.Count)
        {
            // Highlight specific character and face
            HighlightCharacter(characterIndex, true);
            if (faceIndex >= 0)
            {
                HighlightFace(characterIndex, faceIndex, true);
            }
        }
        else
        {
            // Show all characters and faces
            for (int i = 0; i < characters.Count; i++)
            {
                HighlightCharacter(i, false);
                for (int j = 0; j < 4; j++)
                {
                    HighlightFace(i, j, false);
                }
            }
        }
    }

    /// <summary>
    /// Highlight specific character for P300 stimulus.
    /// </summary>
    private void HighlightCharacter(int characterIndex, bool highlight)
    {
        List<CharacterBlob> characters = GameManager.Instance.GetCharacters();
        if (characterIndex >= 0 && characterIndex < characters.Count)
        {
            characters[characterIndex].SetSelected(highlight);
        }
    }

    /// <summary>
    /// Highlight specific face for P300 stimulus.
    /// </summary>
    private void HighlightFace(int characterIndex, int faceIndex, bool highlight)
    {
        List<CharacterBlob> characters = GameManager.Instance.GetCharacters();
        if (characterIndex >= 0 && characterIndex < characters.Count)
        {
            // This would be handled by the CharacterBlob component
        }
    }

    /// <summary>
    /// Update the visual representation of a character's current state.
    /// </summary>
    public void UpdateCharacterDisplay(int characterIndex)
    {
        List<CharacterBlob> characters = GameManager.Instance.GetCharacters();
        if (characterIndex >= 0 && characterIndex < characters.Count)
        {
            // Character update is handled by CharacterBlob itself
            // This is a placeholder for any global UI updates
        }
    }

    /// <summary>
    /// Show feedback message to user.
    /// </summary>
    public void DisplayFeedback(string message)
    {
        Debug.Log($"[Feedback] {message}");
        // TODO: Add visual feedback display (toast message, text label, etc.)
    }

    /// <summary>
    /// Hide P300 paradigm display.
    /// </summary>
    public void HideP300Paradigm()
    {
        GameManager gameManager = GameManager.Instance;
        List<CharacterBlob> characters = gameManager.GetCharacters();

        foreach (var character in characters)
        {
            character.SetSelected(false);
            character.HighlightFaces(false);
        }
    }

    /// <summary>
    /// Reset UI to idle state.
    /// </summary>
    public void ResetUI()
    {
        HideP300Paradigm();
        // Reset any animations or states
    }

    public bool IsInitialized() => isInitialized;
}
