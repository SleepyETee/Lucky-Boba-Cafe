// ============================================================
// FILE: FontSetter.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Applies JazzCreateBubble font to ALL TextMeshPro
//              text elements in the scene at runtime.
// ============================================================
using UnityEngine;
using TMPro;

public class FontSetter : MonoBehaviour
{
    [Header("Font")]
    [SerializeField] private TMP_FontAsset jazzFont;
    
    [Header("Options")]
    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private bool applyToWorldText = true;
    [SerializeField] private bool applyToUIText = true;
    
    void Start()
    {
        if (applyOnStart)
            ApplyFontToAll();
    }
    
    /// <summary>
    /// Finds ALL TextMeshPro components in the scene and sets
    /// their font to the assigned JazzCreateBubble font.
    /// </summary>
    public void ApplyFontToAll()
    {
        if (jazzFont == null)
        {
            Debug.LogWarning("[FontSetter] No font assigned! Drag JazzCreateBubble SDF into the Font slot.");
            return;
        }
        
        int count = 0;
        
        // World-space TextMeshPro (3D text, order bubbles, prompts)
        if (applyToWorldText)
        {
            TextMeshPro[] worldTexts = FindObjectsByType<TextMeshPro>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in worldTexts)
            {
                t.font = jazzFont;
                count++;
            }
        }
        
        // UI TextMeshProUGUI (HUD, menus, minigame)
        if (applyToUIText)
        {
            TextMeshProUGUI[] uiTexts = FindObjectsByType<TextMeshProUGUI>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in uiTexts)
            {
                t.font = jazzFont;
                count++;
            }
        }
        
        Debug.Log($"[FontSetter] Applied JazzCreateBubble to {count} text elements");
    }
}
