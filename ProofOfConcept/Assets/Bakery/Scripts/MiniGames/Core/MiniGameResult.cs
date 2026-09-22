// All mini-games report this same result type to the rest of the game.
// Outcome is display text; use Succeeded for gameplay decisions.
public sealed class MiniGameResult
{
    public RecipeData Recipe { get; }
    public bool Succeeded { get; }
    public string Outcome { get; }

    public MiniGameResult(RecipeData recipe, bool succeeded, string outcome)
    {
        Recipe = recipe;
        Succeeded = succeeded;
        Outcome = outcome;
    }
}
