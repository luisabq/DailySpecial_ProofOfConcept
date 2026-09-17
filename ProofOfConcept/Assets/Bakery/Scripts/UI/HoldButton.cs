using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A pointer hold, not repeated clicks. Releasing or leaving the button stops the hold.
[RequireComponent(typeof(Button))]
public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private Button button;
    private int? pointerId;
    public bool IsHeld => pointerId.HasValue && isActiveAndEnabled &&
        button != null && button.IsInteractable();

    private void Awake() { button = GetComponent<Button>(); }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || pointerId.HasValue ||
            !isActiveAndEnabled || !button.IsInteractable()) return;
        pointerId = eventData.pointerId;
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (pointerId == eventData.pointerId) ResetHold();
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (pointerId == eventData.pointerId) ResetHold();
    }
    public void ResetHold() { pointerId = null; }
    private void OnDisable() { ResetHold(); }
    private void OnApplicationFocus(bool focused) { if (!focused) ResetHold(); }
}
