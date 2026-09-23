using System;
using System.Collections.Generic;
using UnityEngine;

// Single-player, scene-local unlocks. One pickup supplies one recipe attempt,
// including repeated entries of the same ingredient. No quantities or persistence.
public class IngredientInventory : MonoBehaviour
{
    public static IngredientInventory Instance { get; private set; }
    public event Action Changed;
    private readonly HashSet<string> collected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one IngredientInventory is allowed in the scene.", this);
            enabled = false;
            return;
        }
        Instance = this;
    }

    public bool Has(string ingredient)
    {
        return !string.IsNullOrWhiteSpace(ingredient) && collected.Contains(ingredient.Trim());
    }

    public bool TryCollect(string ingredient)
    {
        if (!isActiveAndEnabled || string.IsNullOrWhiteSpace(ingredient) || !collected.Add(ingredient.Trim())) return false;
        Changed?.Invoke();
        return true;
    }

    // Validate the entire set before removing anything. Unrelated pickups remain held.
    public bool TryConsume(IReadOnlyList<string> ingredients)
    {
        if (!isActiveAndEnabled || ingredients == null || ingredients.Count == 0) return false;
        foreach (string ingredient in ingredients) if (!Has(ingredient)) return false;
        foreach (string ingredient in ingredients) collected.Remove(ingredient.Trim());
        Changed?.Invoke();
        return true;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
