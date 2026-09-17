using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RecipeViewerTrigger : MonoBehaviour
{
    [SerializeField] private RecipeViewer recipeViewer;
    private readonly HashSet<Collider> contacts = new HashSet<Collider>();
    private bool opened;

    private void Reset() { GetComponent<BoxCollider>().isTrigger = true; }
    private void OnTriggerEnter(Collider other)
    {
        if (recipeViewer == null) return;
        var player = other.GetComponentInParent<FirstPersonPlayerController>();
        if (player == null || player != recipeViewer.Player) return;
        if (!contacts.Add(other) || contacts.Count != 1) return;
        opened = recipeViewer.Open();
    }
    private void OnTriggerExit(Collider other)
    {
        if (!contacts.Remove(other) || contacts.Count != 0) return;
        CloseViewer();
    }
    private void CloseViewer()
    {
        if (opened && recipeViewer != null) recipeViewer.Close();
        opened = false;
    }
    private void OnDisable()
    {
        CloseViewer();
        contacts.Clear();
    }
}
