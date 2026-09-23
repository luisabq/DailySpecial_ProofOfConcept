using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Put on Player. Cast from the camera center and respect the FIRST solid hit,
// so walls/shelves block pickups instead of allowing interaction through them.
public class IngredientPickupInteractor : MonoBehaviour
{
    [SerializeField] private FirstPersonPlayerController player;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private IngredientInventory inventory;
    [SerializeField] private TMP_Text promptText;
    [SerializeField, Min(0.1f)] private float reach = 3f;
    [Tooltip("Include ingredients AND solid world geometry. Exclude the player's layer.")]
    [SerializeField] private LayerMask raycastMask = Physics.DefaultRaycastLayers;

    private void Start()
    {
        if (player == null || playerCamera == null || inventory == null)
        {
            Debug.LogError("IngredientPickupInteractor: Assign Player, Player Camera and Inventory.", this);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        SetPrompt("");
        if (player == null || playerCamera == null || inventory == null || !player.isActiveAndEnabled ||
            !inventory.isActiveAndEnabled || player.IsUIOpen || !Application.isFocused ||
            Time.timeScale <= 0f || Cursor.lockState != CursorLockMode.Locked) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, reach, raycastMask, QueryTriggerInteraction.Ignore)) return;
        IngredientPickup pickup = hit.collider.GetComponentInParent<IngredientPickup>();
        if (pickup == null || !pickup.CanTake) return;
        SetPrompt("[E] Take " + pickup.IngredientName);
        if (TakePressed() && pickup.TryTake(inventory)) SetPrompt("");
    }

    private static bool TakePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    private void SetPrompt(string text) { if (promptText != null) promptText.text = text; }
    private void OnDisable() { SetPrompt(""); }
}
