using System;

// A snapshot: switching recipes or editing assets cannot rewrite an existing receipt.
public sealed class BakeReceipt
{
    public int Number { get; }
    public string Product { get; }
    public string RecipeName { get; }
    public string Ingredients { get; }
    public string Outcome { get; }
    public string ManagementExplanation { get; }
    public string SupplyRecord { get; }
    public string Authorization { get; }
    public bool IsFlagged { get; private set; }

    public BakeReceipt(int number, MiniGameResult result)
    {
        Number = number;
        Product = result.Recipe.ReceiptProductName;
        RecipeName = result.Recipe.RecipeName;
        Ingredients = string.Join(", ", result.IngredientsUsed);
        Outcome = result.Outcome;
        ManagementExplanation = result.Recipe.ManagementExplanation;
        SupplyRecord = result.Recipe.SupplyRecord;
        Authorization = result.Recipe.Authorization;
    }

    public bool Flag()
    {
        if (IsFlagged) return false;
        IsFlagged = true;
        return true;
    }
}
