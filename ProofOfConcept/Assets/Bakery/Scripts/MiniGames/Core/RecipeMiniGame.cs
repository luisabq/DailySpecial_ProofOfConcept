using System;
using UnityEngine;

// Shared contract for baking, decorating, mixing, or future mini-games.
// The controller creates a NEW instance for every attempt.
public abstract class RecipeMiniGame : MonoBehaviour
{
    public event Action<MiniGameResult> Completed;
    public RecipeData Recipe { get; private set; }
    public bool IsRunning { get; private set; }
    private bool hasBegun;

    // Must be a read-only check: the controller also calls it on the prefab asset.
    public abstract bool CanPlay(RecipeData recipe, out string error);

    public bool Begin(RecipeData recipe)
    {
        if (hasBegun)
        {
            Debug.LogError("Create a fresh mini-game instance for each attempt.", this);
            return false;
        }
        if (recipe == null)
        {
            Debug.LogError("Cannot begin without a recipe.", this);
            return false;
        }
        if (!recipe.TryValidate(out string error) || !CanPlay(recipe, out error))
        {
            Debug.LogError(error, this);
            return false;
        }

        Recipe = recipe;
        hasBegun = true;
        IsRunning = true;
        OnBegin(recipe);
        return true;
    }

    protected abstract void OnBegin(RecipeData recipe);

    // Duplicate clicks and timeout callbacks cannot submit the same attempt twice.
    protected void Complete(bool succeeded, string outcome, bool producedFood = false, string[] ingredientsUsed = null)
    {
        if (!IsRunning) return;
        IsRunning = false;
        Completed?.Invoke(new MiniGameResult(Recipe, succeeded, outcome, producedFood, ingredientsUsed));
    }

    // Cancellation never reports success or failure to the customer system.
    public void Cancel()
    {
        if (!IsRunning) return;
        IsRunning = false;
        OnCancelled();
    }

    protected virtual void OnCancelled() { }
}
