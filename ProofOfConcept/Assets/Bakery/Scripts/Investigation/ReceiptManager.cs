using System;
using UnityEngine;

// Scene-owned, session-only storage. Keep this object enabled outside receipt/oven panels.
public class ReceiptManager : MonoBehaviour
{
    [SerializeField] private RecipeMiniGameController miniGameController;
    public event Action Changed;
    public BakeReceipt LatestReceipt { get; private set; }
    public int FlaggedReceiptCount { get; private set; }
    private int nextNumber = 1;
    private MiniGameResult lastHandledResult;

    private void OnEnable()
    {
        if (miniGameController != null) miniGameController.MiniGameCompleted += HandleCompleted;
        else Debug.LogError("ReceiptManager: Assign Mini Game Controller.", this);
    }

    private void HandleCompleted(MiniGameResult result)
    {
        if (result == null || result == lastHandledResult || !result.ProducedFood || result.Recipe == null) return;
        lastHandledResult = result;
        LatestReceipt = new BakeReceipt(nextNumber++, result);
        Changed?.Invoke();
    }

    public void FlagLatestReceipt()
    {
        // A suspicion is recorded, not graded as a correct deduction.
        if (LatestReceipt == null || !LatestReceipt.Flag()) return;
        FlaggedReceiptCount++;
        Changed?.Invoke();
    }

    private void OnDisable()
    {
        if (miniGameController != null) miniGameController.MiniGameCompleted -= HandleCompleted;
    }
}
