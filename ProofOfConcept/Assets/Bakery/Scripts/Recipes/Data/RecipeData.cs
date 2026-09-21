using UnityEngine;

// Shared recipe information. A recipe selects its own mini-game prefab.
// Add specialized recipe classes when a new mechanic needs its own settings.
[CreateAssetMenu(fileName = "NewRecipe", menuName = "Bakery/Recipes/Recipe")]
public class RecipeData : ScriptableObject
{
    [SerializeField] private string recipeName;
    [Tooltip("A prefab asset with a RecipeMiniGame implementation on its ROOT object.")]
    [SerializeField] private RecipeMiniGame miniGamePrefab;

    [TextArea(3, 10)]
    [Tooltip("Optional instructions displayed only at the recipe station.")]
    [SerializeField] private string recipeInstructions;

    public virtual string GetInstructions()
    {
        return string.IsNullOrWhiteSpace(recipeInstructions) ? "No instructions added yet." : recipeInstructions;
    }

    [Header("Receipt / investigation - prototype story text")]
    [Tooltip("Product name printed on receipts. Blank uses Recipe Name.")]
    [SerializeField] private string receiptProductName;
    [TextArea(2, 5)][SerializeField] private string managementExplanation;
    [TextArea(2, 5)][SerializeField] private string supplyRecord;
    [TextArea(2, 5)][SerializeField] private string authorization;

    public string ReceiptProductName => string.IsNullOrWhiteSpace(receiptProductName) ? recipeName : receiptProductName;
    public string ManagementExplanation => managementExplanation;
    public string SupplyRecord => supplyRecord;
    public string Authorization => authorization;

    public string RecipeName => recipeName;
    public RecipeMiniGame MiniGamePrefab => miniGamePrefab;

    public virtual bool TryValidate(out string error)
    {
        if (string.IsNullOrWhiteSpace(recipeName))
        {
            error = "The recipe needs a display name.";
            return false;
        }
        if (miniGamePrefab == null)
        {
            error = "Assign the recipe's Mini Game Prefab.";
            return false;
        }
        error = null;
        return true;
    }
}
