using UnityEngine;

// Keep this root active. Hide only the child containing the model AND colliders.
public class IngredientPickup : MonoBehaviour
{
    [SerializeField] private IngredientInventory inventory;
    [SerializeField] private string ingredientName;
    [SerializeField] private GameObject itemRoot;
    public string IngredientName => ingredientName == null ? "" : ingredientName.Trim();
    public bool CanTake => isActiveAndEnabled && inventory != null && inventory.isActiveAndEnabled &&
        itemRoot != null && itemRoot.activeInHierarchy && !inventory.Has(IngredientName);

    private void OnEnable()
    {
        if (inventory == null || string.IsNullOrWhiteSpace(ingredientName) || itemRoot == null ||
            itemRoot == gameObject || !itemRoot.transform.IsChildOf(transform))
        {
            Debug.LogError("IngredientPickup: Assign Inventory, Ingredient Name, and a separate child Item Root containing model/colliders.", this);
            enabled = false;
            return;
        }
        inventory.Changed += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        if (itemRoot != null && inventory != null) itemRoot.SetActive(!inventory.Has(IngredientName));
    }

    public bool TryTake(IngredientInventory collector)
    {
        return collector == inventory && CanTake && inventory.TryCollect(IngredientName);
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.Changed -= Refresh;
    }
}
