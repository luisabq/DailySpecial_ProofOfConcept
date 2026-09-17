using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The visible ingredient token. The board owns placement; this handles pointer input.
[RequireComponent(typeof(Image), typeof(CanvasGroup))]
public class DraggableIngredient : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private TMP_Text nameText;
    public IngredientSlotBoard Board { get; private set; }
    public IngredientSlot CurrentSlot { get; internal set; }
    public string IngredientName { get; private set; }
    private RectTransform rect;
    private CanvasGroup group;
    private Vector2 restingSize;
    private int? pointerId;

    public void Initialize(IngredientSlotBoard board, string ingredientName, Sprite sprite)
    {
        Board = board;
        IngredientName = ingredientName;
        rect = (RectTransform)transform;
        restingSize = rect.sizeDelta;
        group = GetComponent<CanvasGroup>();
        Image icon = GetComponent<Image>();
        icon.sprite = sprite;
        icon.preserveAspect = true;
        icon.raycastTarget = true;
        if (nameText != null)
        {
            nameText.text = ingredientName;
            nameText.raycastTarget = false;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Board == null || eventData.button != PointerEventData.InputButton.Left ||
            !Board.BeginDrag(this)) return;
        pointerId = eventData.pointerId;
        // Ignore this icon during raycasts so the slot beneath can receive OnDrop.
        group.blocksRaycasts = false;
        group.alpha = 0.8f;
        rect.SetParent(Board.DragLayer, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = restingSize;
        rect.SetAsLastSibling();
        MoveToPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (pointerId != eventData.pointerId) return;
        MoveToPointer(eventData);
    }

    private void MoveToPointer(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            Board.DragLayer, eventData.position, eventData.pressEventCamera, out Vector2 local))
            rect.localPosition = new Vector3(local.x, local.y, 0f);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (pointerId != eventData.pointerId) return;
        CancelDrag();
    }

    public void CancelDrag()
    {
        if (!pointerId.HasValue) return;
        pointerId = null;
        group.blocksRaycasts = true;
        group.alpha = 1f;
        SnapToPlacement();
        Board.EndDrag(this);
    }

    public void SnapToPlacement()
    {
        rect.SetParent(CurrentSlot == null ? Board.TrayRoot : CurrentSlot.transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = restingSize;
        rect.localScale = Vector3.one;
    }

    private void OnApplicationFocus(bool focused) { if (!focused) CancelDrag(); }
    private void OnDisable()
    {
        // Do not reparent during hierarchy destruction. Each attempt is a fresh prefab.
        pointerId = null;
        if (group != null) { group.blocksRaycasts = true; group.alpha = 1f; }
    }
}
