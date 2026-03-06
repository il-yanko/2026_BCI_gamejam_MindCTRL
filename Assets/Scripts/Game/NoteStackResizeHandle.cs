using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Drag handle placed on the right edge of each NoteStack column.
/// Dragging it left / right resizes all four NoteStack columns simultaneously.
/// Add via SceneBootstrapper — no manual Inspector wiring needed.
/// </summary>
[RequireComponent(typeof(Image))]
public class NoteStackResizeHandle : MonoBehaviour,
    IBeginDragHandler, IDragHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>Shared across all four handles — set by SceneBootstrapper.</summary>
    public List<LayoutElement> NoteStacks;

    private const float MinWidth =  5f;
    private const float MaxWidth = 400f;

    // Absolute-position tracking — avoids cumulative drift from per-frame delta
    private float _widthAtDragStart;
    private float _screenXAtDragStart;

    private Canvas _canvas;
    private Image  _image;

    private static readonly Color NormalColor = new Color(1f, 1f, 1f, 0.18f);
    private static readonly Color HoverColor  = new Color(1f, 1f, 1f, 0.60f);

    void Awake()
    {
        _canvas      = GetComponentInParent<Canvas>();
        _image       = GetComponent<Image>();
        _image.color = NormalColor;
    }

    public void OnPointerEnter(PointerEventData _) => _image.color = HoverColor;
    public void OnPointerExit (PointerEventData _) => _image.color = NormalColor;

    public void OnBeginDrag(PointerEventData e)
    {
        _widthAtDragStart  = NoteStacks != null && NoteStacks.Count > 0
            ? NoteStacks[0].preferredWidth : 80f;
        _screenXAtDragStart = e.position.x;
    }

    public void OnDrag(PointerEventData e)
    {
        if (NoteStacks == null) return;
        float scale    = _canvas != null ? _canvas.scaleFactor : 1f;
        // Total displacement from drag start → no cumulative error
        float newWidth = Mathf.Clamp(
            _widthAtDragStart + (e.position.x - _screenXAtDragStart) / scale,
            MinWidth, MaxWidth);
        foreach (var le in NoteStacks)
            le.preferredWidth = newWidth;
    }
}
