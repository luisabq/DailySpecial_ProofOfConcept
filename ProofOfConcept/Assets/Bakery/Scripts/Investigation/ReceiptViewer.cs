using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReceiptViewer : MonoBehaviour
{
    [SerializeField] private FirstPersonPlayerController player;
    [SerializeField] private ReceiptManager receiptManager;
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private TMP_Text receiptText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button flagButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private ScrollRect scrollRect;
    public FirstPersonPlayerController Player => player;
    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (uiPanel != null && !transform.IsChildOf(uiPanel.transform)) uiPanel.SetActive(false);
    }
    private void OnEnable()
    {
        if (flagButton != null) flagButton.onClick.AddListener(FlagReceipt);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (receiptManager != null) receiptManager.Changed += Refresh;
    }
    private void Update()
    {
        if (IsOpen && FirstPersonPlayerController.EscapePressed()) Close();
    }

    public bool Open()
    {
        if (!isActiveAndEnabled || player == null || !player.isActiveAndEnabled || receiptManager == null ||
            uiPanel == null || receiptText == null || statusText == null || flagButton == null || closeButton == null ||
            transform.IsChildOf(uiPanel.transform))
        {
            Debug.LogError("ReceiptViewer: Assign all required references and keep the manager outside its panel.", this);
            return false;
        }
        if (player.IsUIOpen && !IsOpen) return false;
        IsOpen = true;
        uiPanel.SetActive(true);
        player.AcquireUI(this);
        Refresh();
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
        return true;
    }

    private void Refresh()
    {
        if (!IsOpen) return;
        BakeReceipt receipt = receiptManager.LatestReceipt;
        flagButton.interactable = receipt != null && !receipt.IsFlagged;
        if (receipt == null)
        {
            receiptText.text = "No receipt yet. Finish a bake, then return here.";
            statusText.text = "Nothing to inspect yet.";
            return;
        }
        receiptText.text = "COZY CRUMBS — BAKE RECEIPT #" + receipt.Number +
            "\nProduct: " + receipt.Product + "\nRecipe followed: " + receipt.RecipeName +
            "\nIngredients used: " + receipt.Ingredients + "\nBake result: " + receipt.Outcome +
            "\n\nManagement's explanation:\n" + receipt.ManagementExplanation +
            "\n\nSupply record:\n" + receipt.SupplyRecord +
            "\n\nAuthorization:\n" + receipt.Authorization;
        statusText.text = (receipt.IsFlagged ? "Inconsistency noted." : "Inspect the records. Flag anything you find suspicious.") +
            "\nReceipts flagged this session: " + receiptManager.FlaggedReceiptCount;
    }
    private void FlagReceipt() { if (IsOpen && receiptManager != null) receiptManager.FlagLatestReceipt(); }
    public void Close()
    {
        IsOpen = false;
        if (uiPanel != null && !transform.IsChildOf(uiPanel.transform)) uiPanel.SetActive(false);
        if (player != null) player.ReleaseUI(this);
    }
    private void OnDisable()
    {
        if (flagButton != null) flagButton.onClick.RemoveListener(FlagReceipt);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (receiptManager != null) receiptManager.Changed -= Refresh;
        Close();
    }
}
