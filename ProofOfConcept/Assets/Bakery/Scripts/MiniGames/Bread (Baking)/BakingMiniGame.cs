using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One specific mini-game implementation: ingredient order followed by oven timing.
// Attach to the ROOT of a UI prefab. The controller passes in the recipe at runtime.
public class BakingMiniGame : RecipeMiniGame
{
    [Header("Inventory")]
    [SerializeField] private bool requireShelfPickups = true;
    private IngredientInventory pickupInventory;

    [Header("Ingredient Slots")]
    [SerializeField] private IngredientSlotBoard ingredientBoard;
    [SerializeField] private Button confirmIngredientsButton;

    [Header("Text")]
    [SerializeField] private TMP_Text orderText;
    [SerializeField] private TMP_Text messageText;

    [Header("Actions - leave Inspector On Click lists empty")]
    [SerializeField] private Button bakeButton;
    [SerializeField] private Button removeButton;

    [Header("Mixing")]
    [SerializeField] private Button mixButton;
    [SerializeField] private Slider mixingProgress;
    private HoldButton mixHold;
    private float mixingDuration;
    private float mixProgress;

    [Header("Meter")]
    [Tooltip("An empty RectTransform inside this prefab. Graphics are created at runtime.")]
    [SerializeField] private RectTransform meterTrack;

    private enum Stage { Idle, Ingredients, Mixing, Ready, Baking, Result }
    private Stage stage = Stage.Idle;
    private string[] ingredients;
    private string foodName;
    private float bakeProgress;
    private float bakeDuration;
    private float perfectStart;
    private float perfectEnd;
    private RectTransform marker;

    // Called on the prefab before instantiation. Do not modify UI or recipe data here.
    public override bool CanPlay(RecipeData recipe, out string error)
    {
        BakingRecipeData bakingRecipe = recipe as BakingRecipeData;
        if (bakingRecipe == null)
            return Invalid("BakingMiniGame requires a Baking Recipe asset.", out error);
        if (!bakingRecipe.TryValidate(out error)) return false;
        if (requireShelfPickups && (IngredientInventory.Instance == null ||
            !IngredientInventory.Instance.isActiveAndEnabled))
            return Invalid("Add one enabled IngredientInventory to the scene for shelf pickups.", out error);

        if (orderText == null || messageText == null || bakeButton == null ||
            removeButton == null || meterTrack == null || mixButton == null || mixingProgress == null ||
            ingredientBoard == null || confirmIngredientsButton == null)
            return Invalid("Assign every BakingMiniGame UI reference.", out error);
        if (orderText == messageText || bakeButton == removeButton || mixButton == bakeButton || mixButton == removeButton || confirmIngredientsButton == mixButton ||
            confirmIngredientsButton == bakeButton || confirmIngredientsButton == removeButton)
            return Invalid("Use separate text objects and separate action buttons.", out error);
        if (!IsInsidePrefab(orderText.transform) || !IsInsidePrefab(messageText.transform) ||
            !IsInsidePrefab(bakeButton.transform) || !IsInsidePrefab(removeButton.transform) ||
            !IsInsidePrefab(meterTrack) || !IsInsidePrefab(mixButton.transform) || !IsInsidePrefab(mixingProgress.transform) || !IsInsidePrefab(ingredientBoard.transform) ||
            !IsInsidePrefab(confirmIngredientsButton.transform))
            return Invalid("All BakingMiniGame UI references must belong to its prefab.", out error);
        if (mixButton.GetComponent<HoldButton>() == null)
            return Invalid("Add HoldButton to the Mix Dough button.", out error);
        if (!ingredientBoard.CanBuild(bakingRecipe.Ingredients, transform, out error)) return false;

        error = null;
        return true;
    }

    protected override void OnBegin(RecipeData recipe)
    {
        BakingRecipeData bakingRecipe = (BakingRecipeData)recipe;

        // Snapshot settings so editing an asset cannot change an attempt halfway through.
        foodName = bakingRecipe.RecipeName.Trim();
        mixingDuration = bakingRecipe.MixingDuration;
        mixHold = mixButton.GetComponent<HoldButton>();
        mixHold.ResetHold();
        mixingProgress.minValue = 0f;
        mixingProgress.maxValue = 1f;
        mixingProgress.wholeNumbers = false;
        mixingProgress.interactable = false;
        mixingProgress.SetValueWithoutNotify(0f);
        mixProgress = 0f;
        bakeDuration = bakingRecipe.BakeDuration;
        perfectStart = bakingRecipe.PerfectStart;
        perfectEnd = bakingRecipe.PerfectEnd;
        ingredients = new string[bakingRecipe.Ingredients.Count];
        for (int i = 0; i < ingredients.Length; i++)
            ingredients[i] = bakingRecipe.Ingredients[i].Trim();

        confirmIngredientsButton.onClick.AddListener(ConfirmIngredients);
        ingredientBoard.Changed += RefreshButtons;
        bakeButton.onClick.AddListener(StartBaking);
        removeButton.onClick.AddListener(RemoveFood);

        CreateMeter();
        stage = Stage.Ingredients;
        pickupInventory = requireShelfPickups ? IngredientInventory.Instance : null;
        ingredientBoard.Build(ingredients, requireShelfPickups ? new Predicate<string>(pickupInventory.Has) : null);
        bakeProgress = 0f;
        PositionMarker();
        orderText.text = "Order: " + foodName;
        RefreshButtons();
        bool missing = false;
        if (requireShelfPickups)
            foreach (string ingredient in ingredients) if (!pickupInventory.Has(ingredient)) missing = true;
        messageText.text = missing
            ? "Some ingredients have not been collected. Close the oven, visit the shelves, then return."
            : "Drag ingredients into slots from left to right, then confirm.";
    }

    private void Update()
    {
        if (!IsRunning) return;
        if (stage == Stage.Mixing)
        {
            if (mixHold.IsHeld && Application.isFocused)
            {
                mixProgress = Mathf.Clamp01(mixProgress + Time.deltaTime / mixingDuration);
                mixingProgress.SetValueWithoutNotify(mixProgress);
                if (mixProgress >= 1f)
                {
                    mixHold.ResetHold();
                    stage = Stage.Ready;
                    messageText.text = "Dough mixed! Click Start Baking to put it in the oven.";
                    RefreshButtons();
                }
            }
            return;
        }
        if (stage != Stage.Baking) return;
        // Uses scaled time, so setting Time.timeScale to zero pauses the oven.
        bakeProgress = Mathf.Clamp01(bakeProgress + Time.deltaTime / bakeDuration);
        PositionMarker();
        if (bakeProgress >= 1f) FinishBaking();
    }

    private void ConfirmIngredients()
    {
        if (!IsRunning || stage != Stage.Ingredients || !ingredientBoard.IsComplete) return;
        // Confirm spends the ingredients for this attempt, even when arranged incorrectly.
        // Closing/switching before confirmation keeps pickups; after confirmation there is no refund.
        if (requireShelfPickups && (pickupInventory == null || !pickupInventory.TryConsume(ingredients)))
        {
            messageText.text = "Ingredients are no longer available. Close the oven and collect them again.";
            return;
        }
        string[] selected = ingredientBoard.GetOrder();
        bool correct = selected.Length == ingredients.Length;
        for (int i = 0; correct && i < ingredients.Length; i++)
            correct = SameIngredient(selected[i], ingredients[i]);

        // Lock the arrangement only after submission; placement itself gives no hints.
        ingredientBoard.SetEditable(false);
        if (!correct)
        {
            stage = Stage.Result;
            messageText.text = "Incorrect ingredient order! Check the recipe station and try again.";
            RefreshButtons();
            Complete(false, "Incorrect ingredient order");
            return;
        }
        stage = mixingDuration > 0f ? Stage.Mixing : Stage.Ready;
        messageText.text = stage == Stage.Mixing
            ? "Ingredients added. Hold Mix Dough until the mixing bar is full."
            : "Ingredients added. Click Start Baking.";
        RefreshButtons();
    }

    private void StartBaking()
    {
        if (!IsRunning || stage != Stage.Ready) return;
        stage = Stage.Baking;
        messageText.text = "Click Remove from Oven when the marker is in the green zone!";
        RefreshButtons();
    }

    private void RemoveFood()
    {
        if (!IsRunning || stage != Stage.Baking) return;
        FinishBaking();
    }

    private void FinishBaking()
    {
        if (!IsRunning || stage != Stage.Baking) return;
        stage = Stage.Result;
        string outcome;
        bool succeeded = false;
        if (bakeProgress < perfectStart)
        {
            outcome = "Undercooked";
            messageText.text = "Undercooked " + foodName + "! It needed more time.";
        }
        else if (bakeProgress <= perfectEnd)
        {
            outcome = "Perfect";
            succeeded = true;
            messageText.text = "Perfect " + foodName + "! Ready for the customer.";
        }
        else
        {
            outcome = "Burnt";
            messageText.text = "Burnt " + foodName + "! Try removing it earlier.";
        }
        RefreshButtons();
        // Last operation: listeners may close this UI or start another recipe.
        Complete(succeeded, outcome, true, ingredients);
    }

    protected override void OnCancelled()
    {
        stage = Stage.Idle;
        if (mixHold != null) mixHold.ResetHold();
        ingredientBoard.SetEditable(false);
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        confirmIngredientsButton.interactable = IsRunning && stage == Stage.Ingredients && ingredientBoard.IsComplete;
        mixButton.interactable = IsRunning && stage == Stage.Mixing;
        bakeButton.interactable = IsRunning && stage == Stage.Ready;
        removeButton.interactable = IsRunning && stage == Stage.Baking;
    }

    private void CreateMeter()
    {
        // Visible zones and result checks use the same recipe settings.
        CreateBand("Undercooked", new Color(1f, 0.7f, 0.25f), 0f, perfectStart);
        CreateBand("Perfect", new Color(0.25f, 0.8f, 0.35f), perfectStart, perfectEnd);
        CreateBand("Burnt", new Color(0.9f, 0.25f, 0.2f), perfectEnd, 1f);
        marker = CreateBand("Marker", Color.white, 0f, 0f);
        marker.sizeDelta = new Vector2(6f, 12f);
    }

    private RectTransform CreateBand(string bandName, Color color, float start, float end)
    {
        var band = new GameObject(bandName, typeof(RectTransform), typeof(Image));
        RectTransform rect = band.GetComponent<RectTransform>();
        rect.SetParent(meterTrack, false);
        rect.anchorMin = new Vector2(start, 0f);
        rect.anchorMax = new Vector2(end, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = band.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private void PositionMarker()
    {
        marker.anchorMin = new Vector2(bakeProgress, 0f);
        marker.anchorMax = new Vector2(bakeProgress, 1f);
        marker.anchoredPosition = Vector2.zero;
    }

    private bool IsInsidePrefab(Transform target)
    {
        return target.IsChildOf(transform);
    }

    private static bool SameIngredient(string first, string second)
    {
        return string.Equals(first.Trim(), second.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool Invalid(string message, out string error)
    {
        error = message;
        return false;
    }

    private void OnDestroy()
    {
        // Remove only listeners owned by this mini-game.
        if (ingredientBoard != null) ingredientBoard.Changed -= RefreshButtons;
        if (confirmIngredientsButton != null) confirmIngredientsButton.onClick.RemoveListener(ConfirmIngredients);
        if (bakeButton != null) bakeButton.onClick.RemoveListener(StartBaking);
        if (removeButton != null) removeButton.onClick.RemoveListener(RemoveFood);
    }
}
