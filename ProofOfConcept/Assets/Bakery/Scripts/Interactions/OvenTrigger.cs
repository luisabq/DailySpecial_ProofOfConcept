using System.Collections.Generic;
using UnityEngine;

// Re-enter the box to reopen after closing. OnTriggerStay deliberately does not reopen it.
[RequireComponent(typeof(BoxCollider))]
public class OvenTrigger : MonoBehaviour
{
    [SerializeField] private RecipeMiniGameController miniGameController;
    [SerializeField] private RecipeData recipe;
    private readonly HashSet<Collider> contacts = new HashSet<Collider>();
    private bool opened;

    private void Reset() { GetComponent<BoxCollider>().isTrigger = true; }
    private void OnTriggerEnter(Collider other)
    {
        if (miniGameController == null) return;
        var player = other.GetComponentInParent<FirstPersonPlayerController>();
        if (player == null || player != miniGameController.Player) return;
        if (!contacts.Add(other) || contacts.Count != 1) return;
        opened = miniGameController.StartRecipe(recipe);
    }
    private void OnTriggerExit(Collider other)
    {
        if (!contacts.Remove(other) || contacts.Count != 0) return;
        CloseOwnedAttempt();
    }
    private void CloseOwnedAttempt()
    {
        if (opened && miniGameController != null && miniGameController.CurrentRecipe == recipe)
            miniGameController.CloseMiniGame();
        opened = false;
    }
    private void OnDisable()
    {
        CloseOwnedAttempt();
        contacts.Clear();
    }
}
