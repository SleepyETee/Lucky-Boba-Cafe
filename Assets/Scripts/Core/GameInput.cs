// ============================================================
// FILE: GameInput.cs
// DESCRIPTION: Static utility for reading input via the new
//              Input System (Keyboard.current / Mouse.current).
//              Drop-in replacement for legacy UnityEngine.Input.
//              No MonoBehaviour — no wiring needed.
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;

public static class GameInput
{
    static Keyboard KB
    {
        get
        {
            var kb = Keyboard.current;
            if (kb == null && !_kbWarned)
            {
                _kbWarned = true;
                Debug.LogWarning("[GameInput] Keyboard.current is null — no keyboard device detected.");
            }
            return kb;
        }
    }
    static Mouse MS => Mouse.current;
    private static bool _kbWarned;

    // ==================== MOVEMENT (WASD + Arrows) ====================

    public static Vector2 MoveInput
    {
        get
        {
            if (KB == null) return Vector2.zero;
            float x = 0f, y = 0f;
            if (KB.dKey.isPressed || KB.rightArrowKey.isPressed) x += 1f;
            if (KB.aKey.isPressed || KB.leftArrowKey.isPressed)  x -= 1f;
            if (KB.wKey.isPressed || KB.upArrowKey.isPressed)    y += 1f;
            if (KB.sKey.isPressed || KB.downArrowKey.isPressed)  y -= 1f;
            return new Vector2(x, y);
        }
    }

    // ==================== COMMON ACTIONS ====================

    public static bool SprintHeld       => KB != null && KB.leftShiftKey.isPressed;
    public static bool InteractPressed  => KB != null && KB.eKey.wasPressedThisFrame;
    public static bool ConfirmPressed   => KB != null && KB.spaceKey.wasPressedThisFrame;
    public static bool ConfirmHeld      => KB != null && KB.spaceKey.isPressed;
    public static bool ConfirmReleased  => KB != null && KB.spaceKey.wasReleasedThisFrame;
    public static bool PausePressed     => KB != null && KB.escapeKey.wasPressedThisFrame;
    public static bool EnterPressed     => KB != null && KB.enterKey.wasPressedThisFrame;
    public static bool ClickPressed     => MS != null && MS.leftButton.wasPressedThisFrame;

    // ==================== NUMBER KEYS (crafting options) ====================

    public static bool Option1Pressed => KB != null && (KB.digit1Key.wasPressedThisFrame || KB.numpad1Key.wasPressedThisFrame);
    public static bool Option2Pressed => KB != null && (KB.digit2Key.wasPressedThisFrame || KB.numpad2Key.wasPressedThisFrame);
    public static bool Option3Pressed => KB != null && (KB.digit3Key.wasPressedThisFrame || KB.numpad3Key.wasPressedThisFrame);

    // ==================== CONFIGURABLE KEY HELPER ====================

    public static bool WasKeyPressedThisFrame(Key key)
    {
        return KB != null && KB[key].wasPressedThisFrame;
    }
}
