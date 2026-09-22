using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Two-page recipe book. Keeps the original station trigger/cursor API.
public class RecipeViewer : MonoBehaviour
{
    [Serializable]
    private class IngredientVisual
    {
        public string ingredientName;
        public Sprite sprite;
    }

    [Header("Station")]
    [SerializeField] private FirstPersonPlayerController player;
    [SerializeField] private RecipeData[] recipes;
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private Button closeButton;

    [Header("Left Page")]
    [SerializeField] private RectTransform recipeButtonsRoot;
    [SerializeField] private RecipeBookButton recipeButtonPrefab;

    [Header("Right Page")]
    [SerializeField] private TMP_Text recipeTitleText;
    [SerializeField] private Image finishedRecipeImage;
    [SerializeField] private TMP_Text instructionsText;
    [SerializeField] private RectTransform ingredientIconsRoot;
    [SerializeField] private Image ingredientIconPrefab;
    [Tooltip("Optional ScrollRect for the description, not the entire book.")]
    [SerializeField] private ScrollRect instructionsScrollRect;
    [SerializeField] private IngredientVisual[] ingredientVisuals;

    private readonly Dictionary<RecipeData, RecipeBookButton> buttons = new Dictionary<RecipeData, RecipeBookButton>();
    private readonly List<Image> ingredientIcons = new List<Image>();
    private readonly Dictionary<string, Sprite> spriteLookup = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
    private RecipeData selectedRecipe;
    public FirstPersonPlayerController Player => player;
    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (uiPanel != null && !transform.IsChildOf(uiPanel.transform)) uiPanel.SetActive(false);
    }
    private void OnEnable() { if (closeButton != null) closeButton.onClick.AddListener(Close); }
    private void Update()
    {
        if (IsOpen && FirstPersonPlayerController.EscapePressed()) Close();
    }
    public bool Open()
    {
        if (!ValidateSetup()) return false;
        if (player.IsUIOpen && !IsOpen) return false;
        IsOpen = true;
        uiPanel.SetActive(true);
        if (!uiPanel.activeInHierarchy)
        {
            IsOpen = false;
            Debug.LogError("RecipeViewer: Enable the Canvas/parents of the book panel.", this);
            return false;
        }
        player.AcquireUI(this);
        BuildSpriteLookup();
        BuildRecipeButtons();
        return true;
    }
    private bool ValidateSetup()
    {
        if (!isActiveAndEnabled || player == null || !player.isActiveAndEnabled || uiPanel == null ||
            closeButton == null || recipeButtonsRoot == null || recipeButtonPrefab == null ||
            recipeTitleText == null || finishedRecipeImage == null || instructionsText == null ||
            ingredientIconsRoot == null || ingredientIconPrefab == null)
        {
            Debug.LogError("RecipeViewer: Assign all station, left-page and right-page references.", this);
            return false;
        }
        if (transform.IsChildOf(uiPanel.transform) || !recipeButtonsRoot.IsChildOf(uiPanel.transform) ||
            !ingredientIconsRoot.IsChildOf(uiPanel.transform) || recipeButtonsRoot == ingredientIconsRoot ||
            !recipeTitleText.transform.IsChildOf(uiPanel.transform) || !instructionsText.transform.IsChildOf(uiPanel.transform) ||
            !finishedRecipeImage.transform.IsChildOf(uiPanel.transform) || !closeButton.transform.IsChildOf(uiPanel.transform))
        {
            Debug.LogError("RecipeViewer: Keep manager outside the book and UI references inside it. Use separate button/icon containers.", this);
            return false;
        }
        if (!recipeButtonPrefab.IsConfigured || recipeButtonPrefab.gameObject.scene.IsValid() ||
            ingredientIconPrefab.gameObject.scene.IsValid() || recipeButtonPrefab.transform.parent != null ||
            ingredientIconPrefab.transform.parent != null || !recipeButtonPrefab.gameObject.activeSelf ||
            !ingredientIconPrefab.gameObject.activeSelf || !recipeButtonPrefab.enabled)
        {
            Debug.LogError("RecipeViewer: Use enabled prefab assets; assign the recipe button's Icon reference.", this);
            return false;
        }
        return true;
    }
    private void BuildSpriteLookup()
    {
        spriteLookup.Clear();
        if (ingredientVisuals == null) return;
        foreach (IngredientVisual visual in ingredientVisuals)
        {
            if (visual == null || string.IsNullOrWhiteSpace(visual.ingredientName)) continue;
            string key = visual.ingredientName.Trim();
            if (spriteLookup.ContainsKey(key))
            {
                Debug.LogWarning("RecipeViewer: Duplicate ingredient visual ignored: " + key, this);
                continue;
            }
            spriteLookup.Add(key, visual.sprite);
        }
    }
    private void BuildRecipeButtons()
    {
        // Only remove generated objects, never book artwork or other scene UI.
        foreach (RecipeBookButton button in buttons.Values)
            if (button != null) { button.gameObject.SetActive(false); Destroy(button.gameObject); }
        buttons.Clear();
        RecipeData first = null;
        if (recipes != null)
            foreach (RecipeData recipe in recipes)
            {
                if (recipe == null || buttons.ContainsKey(recipe)) continue;
                RecipeData entry = recipe;
                RecipeBookButton button = Instantiate(recipeButtonPrefab, recipeButtonsRoot, false);
                button.Initialize(entry, () => SelectRecipe(entry));
                buttons.Add(entry, button);
                if (first == null) first = entry;
            }
        SelectRecipe(selectedRecipe != null && buttons.ContainsKey(selectedRecipe) ? selectedRecipe : first);
    }
    public void SelectRecipe(RecipeData recipe)
    {
        if (!IsOpen || (recipe != null && !buttons.ContainsKey(recipe))) return;
        selectedRecipe = recipe;
        foreach (var entry in buttons) entry.Value.SetSelected(entry.Key == selectedRecipe);
        foreach (Image icon in ingredientIcons)
            if (icon != null) { icon.gameObject.SetActive(false); Destroy(icon.gameObject); }
        ingredientIcons.Clear();

        recipeTitleText.text = recipe == null ? "No recipes available" : recipe.RecipeName;
        finishedRecipeImage.sprite = recipe == null ? null : recipe.FinishedRecipeSprite;
        finishedRecipeImage.enabled = finishedRecipeImage.sprite != null;
        finishedRecipeImage.preserveAspect = true;
        finishedRecipeImage.raycastTarget = false;
        instructionsText.text = recipe == null ? "Add recipes to the station in the Inspector." : recipe.GetInstructions();
        if (recipe != null)
        {
            IReadOnlyList<string> ingredients = recipe.GetIngredientNames();
            if (ingredients != null)
                foreach (string ingredient in ingredients)
                {
                    Image icon = Instantiate(ingredientIconPrefab, ingredientIconsRoot, false);
                    string name = ingredient == null ? "" : ingredient.Trim();
                    spriteLookup.TryGetValue(name, out Sprite sprite);
                    icon.sprite = sprite;
                    icon.enabled = sprite != null;
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                    icon.gameObject.name = "Ingredient - " + name;
                    ingredientIcons.Add(icon);
                    if (sprite == null) Debug.LogWarning("RecipeViewer: Missing book sprite for " + name, this);
                }
        }
        Canvas.ForceUpdateCanvases();
        if (instructionsScrollRect != null) instructionsScrollRect.verticalNormalizedPosition = 1f;
    }
    public void Close()
    {
        IsOpen = false;
        if (uiPanel != null && !transform.IsChildOf(uiPanel.transform)) uiPanel.SetActive(false);
        if (player != null) player.ReleaseUI(this);
    }
    private void OnDisable()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        Close();
    }
    private void OnDestroy()
    {
        foreach (RecipeBookButton button in buttons.Values) if (button != null) Destroy(button.gameObject);
        foreach (Image icon in ingredientIcons) if (icon != null) Destroy(icon.gameObject);
    }
}
