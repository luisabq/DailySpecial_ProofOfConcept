using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// A reusable left-page recipe selection button.
[RequireComponent(typeof(Button))]
public class RecipeBookButton : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text label;
    [Tooltip("Optional border/highlight child, never the button root.")]
    [SerializeField] private GameObject selectedHighlight;
    private Button button;
    private UnityAction listener;
    public bool IsConfigured => icon != null && icon.transform != transform && icon.transform.IsChildOf(transform) &&
        (selectedHighlight == null || (selectedHighlight != gameObject && selectedHighlight.transform.IsChildOf(transform)));

    public void Initialize(RecipeData recipe, UnityAction onClick)
    {
        button = GetComponent<Button>();
        icon.sprite = recipe.FinishedRecipeSprite;
        icon.enabled = icon.sprite != null;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        if (label != null) { label.text = recipe.RecipeName; label.raycastTarget = false; }
        listener = onClick;
        button.onClick.AddListener(listener);
        SetSelected(false);
    }
    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null) selectedHighlight.SetActive(selected);
    }
    private void OnDestroy()
    {
        if (button != null && listener != null) button.onClick.RemoveListener(listener);
    }
}
