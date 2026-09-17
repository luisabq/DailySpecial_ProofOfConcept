using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Separate recipe station UI. Never supplies hints to the oven mini-game.
public class RecipeViewer : MonoBehaviour
{
    [SerializeField] private FirstPersonPlayerController player;
    [SerializeField] private RecipeData[] recipes;
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private TMP_Text recipesText;
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
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }
    private void Update()
    {
        if (IsOpen && FirstPersonPlayerController.EscapePressed()) Close();
    }

    public bool Open()
    {
        if (!isActiveAndEnabled || player == null || !player.isActiveAndEnabled ||
            uiPanel == null || recipesText == null || closeButton == null ||
            transform.IsChildOf(uiPanel.transform))
        {
            Debug.LogError("RecipeViewer: Assign Player, panel, text and Close Button. Keep manager outside panel.", this);
            return false;
        }
        if (player.IsUIOpen && !IsOpen) return false;
        var text = new StringBuilder();
        if (recipes != null)
            foreach (RecipeData recipe in recipes)
            {
                if (recipe == null) continue;
                text.Append(recipe.RecipeName).Append('\n').Append(recipe.GetInstructions()).Append("\n\n");
            }
        recipesText.text = text.Length == 0 ? "No recipes available." : text.ToString();
        IsOpen = true;
        uiPanel.SetActive(true);
        player.AcquireUI(this);
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
        return true;
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
}
