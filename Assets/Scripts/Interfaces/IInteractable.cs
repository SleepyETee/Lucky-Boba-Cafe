// ============================================================
// FILE: IInteractable.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Interface for all interactable objects in game
// ============================================================

public interface IInteractable
{
    /// <summary>
    /// Called when player presses interact key (E)
    /// </summary>
    void Interact();
    
    /// <summary>
    /// Called when player enters interaction range
    /// </summary>
    void OnPlayerEnterRange();
    
    /// <summary>
    /// Called when player exits interaction range
    /// </summary>
    void OnPlayerExitRange();
    
    /// <summary>
    /// Returns the prompt text to display
    /// </summary>
    string GetInteractionPrompt();
}
