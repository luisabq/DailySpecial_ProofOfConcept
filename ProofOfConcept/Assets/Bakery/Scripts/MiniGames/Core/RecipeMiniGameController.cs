using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// OvenTrigger opens the recipe picker; StartRecipe launches the chosen mini-game.
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

    [Header("Prototype Recipe Picker")]
    [SerializeField] private RecipeData[] availableRecipes;
    [SerializeField] private TMP_Dropdown recipeDropdown;
    [SerializeField] private Button selectRecipeButton;
    [SerializeField] private TMP_Text managementMessage;
    private readonly List<RecipeData> choices = new List<RecipeData>();
    private bool uiIsOpen;

    public event Action<MiniGameResult> MiniGameCompleted;
    public RecipeData CurrentRecipe { get; private set; }
    public RecipeMiniGame ActiveMiniGame { get; private set; }
    public MiniGameResult LastResult { get; private set; }
    public FirstPersonPlayerController Player => player;
    public bool IsOpen => uiIsOpen;

    private void Awake()
    {
        if (uiPanel != null && !transform.IsChildOf(uiPanel.transform)) uiPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (selectRecipeButton != null) selectRecipeButton.onClick.AddListener(StartSelectedRecipe);
        if (recipeDropdown != null) recipeDropdown.onValueChanged.AddListener(PreviewSelection);
        if (retryButton != null) retryButton.onClick.AddListener(RetryCurrentRecipe);
        if (closeButton != null) closeButton.onClick.AddListener(CloseMiniGame);
    }

    private void Update()
    {
        if (IsOpen && FirstPersonPlayerController.EscapePressed()) CloseMiniGame();
    }

    // Entering the oven opens a menu, not a preset recipe.
    public bool OpenRecipeSelection()
    {
        if (!isActiveAndEnabled || player == null || !player.isActiveAndEnabled || uiPanel == null ||
            miniGameRoot == null || closeButton == null || recipeDropdown == null ||
            selectRecipeButton == null || managementMessage == null)
            return ReportError("Assign all scene UI and recipe picker references.");
        if (transform.IsChildOf(uiPanel.transform) || !miniGameRoot.IsChildOf(uiPanel.transform))
            return ReportError("Keep the manager outside UI Panel and Mini Game Root inside it.");
        if (player.IsUIOpen && !IsOpen) return false;
        choices.Clear();
        var labels = new List<string>();
        if (availableRecipes != null)
            foreach (RecipeData recipe in availableRecipes)
                if (recipe != null && !choices.Contains(recipe))
                { choices.Add(recipe); labels.Add(recipe.RecipeName); }
        if (choices.Count == 0) return ReportError("Add recipes to Available Recipes.");
        recipeDropdown.ClearOptions();
        recipeDropdown.AddOptions(labels);
        recipeDropdown.SetValueWithoutNotify(0);
        uiIsOpen = true;
        uiPanel.SetActive(true);
        if (!uiPanel.activeInHierarchy)
        {
            CloseMiniGame();
            return ReportError("Enable the Canvas/parents containing OvenUI.");
        }
        player.AcquireUI(this);
        if (retryButton != null) retryButton.interactable = LastResult != null;
        PreviewSelection(0);
        return true;
    }

    private void PreviewSelection(int index)
    {
        if (index < 0 || index >= choices.Count || managementMessage == null) return;
        managementMessage.text = "Selected recipe: " + choices[index].RecipeName +
            "\nManagement: " + choices[index].ManagementExplanation +
            "\nStart / Switch Recipe begins a fresh attempt.";
    }

    public void StartSelectedRecipe()
    {
        if (!IsOpen || recipeDropdown == null) return;
        int index = recipeDropdown.value;
        if (index >= 0 && index < choices.Count) StartRecipe(choices[index]);
    }

    public bool StartRecipe(RecipeData recipe)
    {
        if (!isActiveAndEnabled || player == null || !player.isActiveAndEnabled || uiPanel == null ||
            miniGameRoot == null || closeButton == null)
            return ReportError("Assign Player, UI Panel, Mini Game Root, and Close Button on an enabled manager.");
        if (transform.IsChildOf(uiPanel.transform) || !miniGameRoot.IsChildOf(uiPanel.transform))
            return ReportError("Keep the manager outside UI Panel and Mini Game Root inside it.");
        if (player.IsUIOpen && !IsOpen) return false;
        if (recipe == null) return ReportError("Choose a recipe to start.");
        if (!recipe.TryValidate(out string error)) return ReportError(error);
        RecipeMiniGame prefab = recipe.MiniGamePrefab;
        if (prefab.gameObject.scene.IsValid() || prefab.transform.parent != null ||
            !(prefab.transform is RectTransform) || !prefab.enabled || !prefab.gameObject.activeSelf)
            return ReportError("Use an enabled UI prefab asset with its mini-game component on the root.");
        if (!prefab.CanPlay(recipe, out error)) return ReportError(error);

        // A retry replaces the attempt without briefly handing movement back to the player.
        DestroyAttempt();
        CurrentRecipe = recipe;
        uiIsOpen = true;
        int selectedIndex = choices.IndexOf(recipe);
        if (recipeDropdown != null && selectedIndex >= 0)
        {
            recipeDropdown.SetValueWithoutNotify(selectedIndex);
            PreviewSelection(selectedIndex);
        }
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
        uiIsOpen = false;
        if (recipeDropdown != null && recipeDropdown.isActiveAndEnabled) recipeDropdown.Hide();
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
        if (selectRecipeButton != null) selectRecipeButton.onClick.RemoveListener(StartSelectedRecipe);
        if (recipeDropdown != null) recipeDropdown.onValueChanged.RemoveListener(PreviewSelection);
        if (retryButton != null) retryButton.onClick.RemoveListener(RetryCurrentRecipe);
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseMiniGame);
        CloseMiniGame();
    }
}
