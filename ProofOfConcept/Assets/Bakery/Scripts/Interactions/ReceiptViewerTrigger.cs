using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ReceiptViewerTrigger : MonoBehaviour
{
    [SerializeField] private ReceiptViewer receiptViewer;
    private readonly HashSet<Collider> contacts = new HashSet<Collider>();
    private bool opened;

    private void Reset() { GetComponent<BoxCollider>().isTrigger = true; }
    private void OnTriggerEnter(Collider other)
    {
        if (receiptViewer == null) return;
        var player = other.GetComponentInParent<FirstPersonPlayerController>();
        if (player == null || player != receiptViewer.Player) return;
        if (!contacts.Add(other) || contacts.Count != 1) return;
        opened = receiptViewer.Open();
    }
    private void OnTriggerExit(Collider other)
    {
        if (!contacts.Remove(other) || contacts.Count != 0) return;
        CloseViewer();
    }
    private void CloseViewer()
    {
        if (opened && receiptViewer != null) receiptViewer.Close();
        opened = false;
    }
    private void OnDisable()
    {
        CloseViewer();
        contacts.Clear();
    }
}
