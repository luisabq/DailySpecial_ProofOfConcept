using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One numbered drop target. Its index represents recipe order, not drop chronology.
[RequireComponent(typeof(Image))]
public class IngredientSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private TMP_Text numberText;
    public IngredientSlotBoard Board { get; private set; }
    public DraggableIngredient Occupant { get; internal set; }
    public int Index { get; private set; }

    public void Initialize(IngredientSlotBoard board, int index)
    {
        Board = board;
        Index = index;
        GetComponent<Image>().raycastTarget = true;
        if (numberText != null)
        {
            numberText.text = (index + 1).ToString();
            numberText.raycastTarget = false;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null || Board == null) return;
        DraggableIngredient ingredient = eventData.pointerDrag.GetComponent<DraggableIngredient>();
        if (ingredient != null) Board.TryDrop(ingredient, this);
    }
}
