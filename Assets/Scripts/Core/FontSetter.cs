// ============================================================
// FILE: FontSetter.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Applies JazzCreateBubble font to ALL TextMeshPro
//              text elements in the scene at runtime.
// ============================================================
using System.Collections.Generic;
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
        EnsureFallback();
        if (applyOnStart)
            ApplyFontToAll();
    }

    void EnsureFallback()
    {
        if (jazzFont == null) return;

        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont == null || defaultFont == jazzFont) return;

        if (jazzFont.fallbackFontAssetTable == null)
            jazzFont.fallbackFontAssetTable = new List<TMP_FontAsset>();

        if (!jazzFont.fallbackFontAssetTable.Contains(defaultFont))
            jazzFont.fallbackFontAssetTable.Add(defaultFont);
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
                FindObjectsInactive.Include);
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
                FindObjectsInactive.Include);
            foreach (var t in uiTexts)
            {
                t.font = jazzFont;
                count++;
            }
        }
        
        Debug.Log($"[FontSetter] Applied JazzCreateBubble to {count} text elements");
    }
}
