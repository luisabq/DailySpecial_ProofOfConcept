using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReceiptViewer : MonoBehaviour
{
    [SerializeField] private FirstPersonPlayerController player;
    [SerializeField] private ReceiptManager receiptManager;
    [SerializeField] private GameObject uiPanel;
    [Header("Receipt fields - independently positioned in the UI")]
    [SerializeField] private TMP_Text receiptNumberText;
    [SerializeField] private TMP_Text productText;
    [SerializeField] private TMP_Text recipeNameText;
    [SerializeField] private TMP_Text ingredientsText;
    [SerializeField] private TMP_Text outcomeText;
    [SerializeField] private TMP_Text managementExplanationText;
    [SerializeField] private TMP_Text supplyRecordText;
    [SerializeField] private TMP_Text authorizationText;

    [Header("Station controls")]
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
            uiPanel == null || !HasReceiptFields() || statusText == null || flagButton == null || closeButton == null ||
            transform.IsChildOf(uiPanel.transform))
        {
            Debug.LogError("ReceiptViewer: Assign all required references and keep the manager outside its panel.", this);
            return false;
        }
        if (player.IsUIOpen && !IsOpen) return false;
        IsOpen = true;
        uiPanel.SetActive(true);
        if (!uiPanel.activeInHierarchy)
        {
            IsOpen = false;
            Debug.LogError("ReceiptViewer: Enable the Canvas and panel parents.", this);
            return false;
        }
        player.AcquireUI(this);
        Refresh();
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
        return true;
    }

    private bool HasReceiptFields()
    {
        return receiptNumberText != null && productText != null && recipeNameText != null &&
            ingredientsText != null && outcomeText != null && managementExplanationText != null &&
            supplyRecordText != null && authorizationText != null;
    }

    private void Refresh()
    {
        if (!IsOpen) return;
        BakeReceipt receipt = receiptManager.LatestReceipt;
        flagButton.interactable = receipt != null && !receipt.IsFlagged;
        if (receipt == null)
        {
            // Clear every field so an empty receipt cannot leave old information visible.
            receiptNumberText.text = "No receipt yet";
            productText.text = "";
            recipeNameText.text = "";
            ingredientsText.text = "";
            outcomeText.text = "";
            managementExplanationText.text = "";
            supplyRecordText.text = "";
            authorizationText.text = "";
            statusText.text = "Finish a bake, then return here.";
            return;
        }
        // Only text values are changed here. Size, position and styling stay under your control.
        // These include labels; edit the prefixes if you prefer separate static label objects.
        receiptNumberText.text = "BAKE RECEIPT #" + receipt.Number;
        productText.text = "Product: " + receipt.Product;
        recipeNameText.text = "Recipe followed: " + receipt.RecipeName;
        ingredientsText.text = "Ingredients used: " + receipt.Ingredients;
        outcomeText.text = "Bake result: " + receipt.Outcome;
        managementExplanationText.text = "Management's explanation:\n" + receipt.ManagementExplanation;
        supplyRecordText.text = "Supply record:\n" + receipt.SupplyRecord;
        authorizationText.text = "Authorization:\n" + receipt.Authorization;
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
