using System;
using UnityEngine;
using UnityEngine.UI;

// Opens only when StartRecipe is called (for example by OvenTrigger).
// Shared UI/cursor ownership belongs here, not in a particular mini-game.
public class RecipeMiniGameController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private FirstPersonPlayerController player;
    [Tooltip("Scene UI panel containing MiniGameRoot, Retry, and Close. Keep this manager outside it.")]
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private RectTransform miniGameRoot;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button closeButton;

    public event Action<MiniGameResult> MiniGameCompleted;
    public RecipeData CurrentRecipe { get; private set; }
    public RecipeMiniGame ActiveMiniGame { get; private set; }
    public MiniGameResult LastResult { get; private set; }
    public FirstPersonPlayerController Player => player;
    public bool IsOpen => ActiveMiniGame != null;

    private void Awake()
    {
        if (uiPanel != null && !transform.IsChildOf(uiPanel.transform)) uiPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (retryButton != null) retryButton.onClick.AddListener(RetryCurrentRecipe);
        if (closeButton != null) closeButton.onClick.AddListener(CloseMiniGame);
    }

    private void Update()
    {
        if (IsOpen && FirstPersonPlayerController.EscapePressed()) CloseMiniGame();
    }

    public bool StartRecipe(RecipeData recipe)
    {
        if (!isActiveAndEnabled || player == null || !player.isActiveAndEnabled || uiPanel == null ||
            miniGameRoot == null || closeButton == null)
            return ReportError("Assign Player, UI Panel, Mini Game Root, and Close Button on an enabled manager.");
        if (transform.IsChildOf(uiPanel.transform) || !miniGameRoot.IsChildOf(uiPanel.transform))
            return ReportError("Keep the manager outside UI Panel and Mini Game Root inside it.");
        if (player.IsUIOpen && !IsOpen) return false;
        if (recipe == null) return ReportError("Assign a recipe on the oven trigger.");
        if (!recipe.TryValidate(out string error)) return ReportError(error);
        RecipeMiniGame prefab = recipe.MiniGamePrefab;
        if (prefab.gameObject.scene.IsValid() || prefab.transform.parent != null ||
            !(prefab.transform is RectTransform) || !prefab.enabled || !prefab.gameObject.activeSelf)
            return ReportError("Use an enabled UI prefab asset with its mini-game component on the root.");
        if (!prefab.CanPlay(recipe, out error)) return ReportError(error);

        // A retry replaces the attempt without briefly handing movement back to the player.
        DestroyAttempt();
        CurrentRecipe = recipe;
        uiPanel.SetActive(true);
        if (!miniGameRoot.gameObject.activeInHierarchy)
        {
            CloseMiniGame();
            return ReportError("Mini Game Root must be active when UI Panel is open.");
        }
        player.AcquireUI(this);
        ActiveMiniGame = Instantiate(prefab, miniGameRoot, false);
        ActiveMiniGame.Completed += HandleCompleted;
        if (!ActiveMiniGame.Begin(recipe))
        {
            CloseMiniGame();
            return false;
        }
        return true;
    }

    private void HandleCompleted(MiniGameResult result)
    {
        LastResult = result;
        if (retryButton != null) retryButton.interactable = true;
        MiniGameCompleted?.Invoke(result);
    }

    public void RetryCurrentRecipe()
    {
        if (CurrentRecipe != null && LastResult != null) StartRecipe(CurrentRecipe);
    }

    private void DestroyAttempt()
    {
        if (ActiveMiniGame != null)
        {
            ActiveMiniGame.Completed -= HandleCompleted;
            ActiveMiniGame.Cancel();
            ActiveMiniGame.gameObject.SetActive(false);
            Destroy(ActiveMiniGame.gameObject);
        }
        ActiveMiniGame = null;
        LastResult = null;
        if (retryButton != null) retryButton.interactable = false;
    }

    public void CloseMiniGame()
    {
        DestroyAttempt();
        CurrentRecipe = null;
        if (uiPanel != null && !transform.IsChildOf(uiPanel.transform)) uiPanel.SetActive(false);
        if (player != null) player.ReleaseUI(this);
    }

    private bool ReportError(string message)
    {
        Debug.LogError("RecipeMiniGameController: " + message, this);
        return false;
    }

    private void OnDisable()
    {
        if (retryButton != null) retryButton.onClick.RemoveListener(RetryCurrentRecipe);
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseMiniGame);
        CloseMiniGame();
    }
}
