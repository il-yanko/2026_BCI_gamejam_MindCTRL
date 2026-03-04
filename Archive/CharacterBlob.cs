using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// CharacterBlob represents a single character with facial expressions.
/// Each blob has 4 faces representing different pitch levels.
/// </summary>
public class CharacterBlob : MonoBehaviour
{
    [System.Serializable]
    public class FacialExpression
    {
        public Image faceUI;
        public Sprite expressionSprite;
        public float pitchLevel = 1.0f;
        public Color highlightColor = Color.yellow;
    }

    [SerializeField] private int characterIndex = 0;
    [SerializeField] private Image blobImage;
    [SerializeField] private Color blobColor = Color.white;
    [SerializeField] private List<FacialExpression> facialExpressions = new();
    [SerializeField] private float transitionSpeed = 5.0f;

    private int currentFaceIndex = 0;
    private bool isSelected = false;
    private CanvasGroup canvasGroup;

    private void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Set initial blob color
        if (blobImage != null)
            blobImage.color = blobColor;

        // Initialize faces
        if (facialExpressions.Count > 0)
            SetFaceExpression(0);
    }

    /// <summary>
    /// Sets the current facial expression (0-3).
    /// </summary>
    public void SetFaceExpression(int faceIndex)
    {
        if (faceIndex < 0 || faceIndex >= facialExpressions.Count)
        {
            Debug.LogWarning($"Invalid face index {faceIndex} for character {characterIndex}");
            return;
        }

        currentFaceIndex = faceIndex;
        FacialExpression expression = facialExpressions[faceIndex];

        // Update face sprite if UI element exists
        if (expression.faceUI != null)
        {
            expression.faceUI.sprite = expression.expressionSprite;
            expression.faceUI.SetNativeSize();
        }

        // Play voice with corresponding pitch
        AudioManager.Instance.PlayVoice(characterIndex, expression.pitchLevel);

        // Visual feedback
        DoFaceTransition();
    }

    /// <summary>
    /// Animates the blob when face changes.
    /// </summary>
    private void DoFaceTransition()
    {
        // Could add animations like scale or color pulse here
        StartCoroutine(AnimateBlob());
    }

    /// <summary>
    /// Simple blob animation on selection.
    /// </summary>
    private System.Collections.IEnumerator AnimateBlob()
    {
        Vector3 originalScale = blobImage.transform.localScale;
        Vector3 targetScale = originalScale * 1.1f;
        float elapsed = 0f;
        float duration = 0.2f;

        // Scale up
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            blobImage.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        // Scale back down
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            blobImage.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        blobImage.transform.localScale = originalScale;
    }

    /// <summary>
    /// Highlights the blob when it's selected by BCI.
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        if (blobImage != null)
        {
            Color targetColor = selected ? new Color(1.2f, 1.2f, 1.2f) : blobColor;
            blobImage.color = Color.Lerp(blobImage.color, targetColor, Time.deltaTime * transitionSpeed);
        }

        // Highlight all faces for P300 paradigm
        HighlightFaces(selected);
    }

    /// <summary>
    /// Shows highlight on faces for P300 flashing paradigm.
    /// </summary>
    public void HighlightFaces(bool highlight)
    {
        foreach (var expression in facialExpressions)
        {
            if (expression.faceUI != null)
            {
                Color targetColor = highlight ? expression.highlightColor : Color.white;
                expression.faceUI.color = Color.Lerp(expression.faceUI.color, targetColor, Time.deltaTime * transitionSpeed);
            }
        }
    }

    /// <summary>
    /// Flash effect for P300 paradigm.
    /// </summary>
    public void Flash()
    {
        StartCoroutine(FlashCoroutine());
    }

    private System.Collections.IEnumerator FlashCoroutine()
    {
        if (canvasGroup == null) yield break;

        float originalAlpha = canvasGroup.alpha;
        
        // Flash bright
        for (float i = 0; i < 0.1f; i += Time.deltaTime)
        {
            canvasGroup.alpha = Mathf.Lerp(originalAlpha, 1.0f, i / 0.1f);
            yield return null;
        }

        // Flash back to normal
        for (float i = 0; i < 0.1f; i += Time.deltaTime)
        {
            canvasGroup.alpha = Mathf.Lerp(1.0f, originalAlpha, i / 0.1f);
            yield return null;
        }

        canvasGroup.alpha = originalAlpha;
    }

    public int GetCharacterIndex() => characterIndex;
    public int GetCurrentFaceIndex() => currentFaceIndex;
    public bool IsSelected() => isSelected;
}
