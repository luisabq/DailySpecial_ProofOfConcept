using System;
using System.Collections.Generic;

// Immutable attempt result. Producing food is separate from success:
// undercooked/burnt food generates a receipt; an invalid sequence does not.
public sealed class MiniGameResult
{
    public RecipeData Recipe { get; }
    public bool Succeeded { get; }
    public string Outcome { get; }
    public bool ProducedFood { get; }
    public IReadOnlyList<string> IngredientsUsed { get; }

    public MiniGameResult(RecipeData recipe, bool succeeded, string outcome,
        bool producedFood = false, string[] ingredientsUsed = null)
    {
        Recipe = recipe;
        Succeeded = succeeded;
        Outcome = outcome;
        ProducedFood = producedFood;
        IngredientsUsed = Array.AsReadOnly(ingredientsUsed == null ? new string[0] : (string[])ingredientsUsed.Clone());
    }
}
