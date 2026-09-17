using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Settings only for the ingredient-sequence + oven-timing mini-game.
// Bread is an ASSET of this type, not another C# class.
[CreateAssetMenu(fileName = "NewBakingRecipe", menuName = "Bakery/Recipes/Baking Recipe")]
public class BakingRecipeData : RecipeData
{
    [Header("Ingredient Sequence")]
    [Tooltip("Names in the order the player must add them. Repeats are allowed.")]
    [SerializeField] private string[] ingredients = new string[0];

    [Header("Mixing")]
    [Tooltip("Seconds holding Mix Dough. Set to zero to skip mixing for a different baking recipe.")]
    [Min(0f)][SerializeField] private float mixingDuration = 3f;
    public float MixingDuration => mixingDuration;

    // Generated from the actual data so the recipe station cannot show a stale order.
    public override string GetInstructions()
    {
        var text = new StringBuilder("Add these ingredients in order:\n");
        if (ingredients != null)
            for (int i = 0; i < ingredients.Length; i++)
                text.Append(i + 1).Append(". ").Append(ingredients[i]).Append('\n');
        if (mixingDuration > 0f) text.Append("Mix the dough until the mixing bar is full.\n");
        text.Append("Put it in the oven, then remove it in the green zone.");
        return text.ToString();
    }

    [Header("Oven Timing")]
    [Min(0.1f)][SerializeField] private float bakeDuration = 8f;
    [Range(0.01f, 0.98f)][SerializeField] private float perfectStart = 0.55f;
    [Range(0.02f, 0.99f)][SerializeField] private float perfectEnd = 0.75f;

    public IReadOnlyList<string> Ingredients => ingredients;
    public float BakeDuration => bakeDuration;
    public float PerfectStart => perfectStart;
    public float PerfectEnd => perfectEnd;

    public override bool TryValidate(out string error)
    {
        if (!base.TryValidate(out error)) return false;
        if (ingredients == null || ingredients.Length == 0)
        {
            error = "A baking recipe needs at least one ingredient.";
            return false;
        }
        foreach (string ingredient in ingredients)
        {
            if (string.IsNullOrWhiteSpace(ingredient))
            {
                error = "Recipe ingredient names cannot be blank.";
                return false;
            }
        }
        if (float.IsNaN(mixingDuration) || float.IsInfinity(mixingDuration) || mixingDuration < 0f)
        {
            error = "Mixing Duration must be zero or a positive number.";
            return false;
        }
        if (float.IsNaN(bakeDuration) || float.IsInfinity(bakeDuration) || bakeDuration < 0.1f ||
            float.IsNaN(perfectStart) || float.IsNaN(perfectEnd) ||
            perfectStart <= 0f || perfectEnd >= 1f || perfectStart >= perfectEnd)
        {
            error = "Use a positive bake duration and 0 < Perfect Start < Perfect End < 1.";
            return false;
        }
        error = null;
        return true;
    }
}
