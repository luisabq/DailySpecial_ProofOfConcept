using System;
using System.Collections.Generic;
using UnityEngine;

// Creates shuffled ingredient tokens and ordered slots. It does not know which order is correct.
public class IngredientSlotBoard : MonoBehaviour
{
    [Serializable]
    private class IngredientVisual
    {
        public string ingredientName;
        public Sprite sprite;
    }

    [Header("Containers inside the baking prefab")]
    [SerializeField] private RectTransform trayRoot;
    [SerializeField] private RectTransform slotsRoot;
    [SerializeField] private RectTransform dragLayer;
    [Header("UI prefabs from the Project window")]
    [SerializeField] private DraggableIngredient ingredientPrefab;
    [SerializeField] private IngredientSlot slotPrefab;
    [Header("One sprite per unique ingredient name")]
    [SerializeField] private IngredientVisual[] ingredientVisuals;

    public event Action Changed;
    public RectTransform TrayRoot => trayRoot;
    public RectTransform DragLayer => dragLayer;
    private readonly List<IngredientSlot> slots = new List<IngredientSlot>();
    private DraggableIngredient activeDrag;
    private bool editable;
    private bool built;
    public bool IsComplete
    {
        get
        {
            if (slots.Count == 0 || activeDrag != null) return false;
            foreach (IngredientSlot slot in slots) if (slot.Occupant == null) return false;
            return true;
        }
    }

    public bool CanBuild(IReadOnlyList<string> ingredients, Transform owner, out string error)
    {
        error = null;
        if (trayRoot == null || slotsRoot == null || dragLayer == null || ingredientPrefab == null || slotPrefab == null)
            error = "Assign all IngredientSlotBoard containers and prefabs.";
        else if (trayRoot == slotsRoot || trayRoot == dragLayer || slotsRoot == dragLayer ||
            !trayRoot.IsChildOf(owner) || !slotsRoot.IsChildOf(owner) || !dragLayer.IsChildOf(owner))
            error = "Use three separate UI containers inside the baking prefab.";
        else if (trayRoot.childCount != 0 || slotsRoot.childCount != 0 || dragLayer.childCount != 0)
            error = "Tray Root, Slots Root and Drag Layer must start empty.";
        else if (ingredientPrefab.gameObject.scene.IsValid() || slotPrefab.gameObject.scene.IsValid() ||
            ingredientPrefab.transform.parent != null || slotPrefab.transform.parent != null ||
            !ingredientPrefab.gameObject.activeSelf || !slotPrefab.gameObject.activeSelf ||
            !ingredientPrefab.enabled || !slotPrefab.enabled)
            error = "Use enabled ingredient/slot prefab assets with their scripts on the roots.";
        if (error != null) return false;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (ingredientVisuals == null) { error = "Assign ingredient sprites."; return false; }
        foreach (IngredientVisual visual in ingredientVisuals)
        {
            if (visual == null || string.IsNullOrWhiteSpace(visual.ingredientName) || visual.sprite == null ||
                !names.Add(visual.ingredientName.Trim()))
            { error = "Each ingredient visual needs a unique name and a sprite."; return false; }
        }
        foreach (string ingredient in ingredients)
            if (!names.Contains(ingredient.Trim()))
            { error = "Missing ingredient sprite: " + ingredient; return false; }
        return true;
    }

    public void Build(IReadOnlyList<string> ingredients, Predicate<string> isAvailable = null)
    {
        if (built) throw new InvalidOperationException("Create a fresh board for each attempt.");
        built = true;
        var shuffled = new List<string>();
        for (int i = 0; i < ingredients.Count; i++)
        {
            IngredientSlot slot = Instantiate(slotPrefab, slotsRoot, false);
            slot.Initialize(this, i);
            slots.Add(slot);
            // Slots always exist; only collected ingredients get draggable tokens.
            string name = ingredients[i].Trim();
            if (isAvailable == null || isAvailable(name)) shuffled.Add(name);
        }
        // Shuffle independently of recipe order so the tray does not act as a checklist.
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            string temp = shuffled[i]; shuffled[i] = shuffled[j]; shuffled[j] = temp;
        }
        foreach (string ingredient in shuffled)
        {
            Sprite sprite = null;
            foreach (IngredientVisual visual in ingredientVisuals)
                if (string.Equals(ingredient, visual.ingredientName.Trim(), StringComparison.OrdinalIgnoreCase))
                { sprite = visual.sprite; break; }
            DraggableIngredient token = Instantiate(ingredientPrefab, trayRoot, false);
            token.Initialize(this, ingredient, sprite);
        }
        editable = true;
        Changed?.Invoke();
    }

    public bool BeginDrag(DraggableIngredient token)
    {
        if (!editable || !isActiveAndEnabled || activeDrag != null || token.Board != this) return false;
        activeDrag = token;
        Changed?.Invoke();
        return true;
    }

    public void TryDrop(DraggableIngredient token, IngredientSlot target)
    {
        if (!editable || token != activeDrag || token.Board != this || target.Board != this) return;
        IngredientSlot origin = token.CurrentSlot;
        if (origin == target) return;
        DraggableIngredient displaced = target.Occupant;
        if (origin != null) origin.Occupant = displaced;
        if (displaced != null)
        {
            displaced.CurrentSlot = origin; // Return to tray if the dragged token came from there.
            displaced.SnapToPlacement();
        }
        target.Occupant = token;
        token.CurrentSlot = target;
        // Dragged token snaps on EndDrag, after Unity finishes dispatching this drop.
        Changed?.Invoke();
    }

    public void EndDrag(DraggableIngredient token)
    {
        if (activeDrag == token) activeDrag = null;
        Changed?.Invoke();
    }

    public string[] GetOrder()
    {
        var result = new string[slots.Count];
        for (int i = 0; i < slots.Count; i++)
            result[i] = slots[i].Occupant == null ? null : slots[i].Occupant.IngredientName;
        return result;
    }

    public void SetEditable(bool value)
    {
        editable = value;
        if (!value && activeDrag != null) activeDrag.CancelDrag();
    }

    private void OnDisable() { editable = false; activeDrag = null; }
}
