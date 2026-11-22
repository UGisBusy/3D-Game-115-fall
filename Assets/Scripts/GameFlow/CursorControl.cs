using UnityEngine;

/// <summary>
/// The only place that changes the mouse cursor.
///  - Menu: shown (SequenceManager menu states).
///  - In a level: follows the Player input (PlayerInputReader). Locked and hidden while playing,
///    shown while the Player input is off (Pause, Fail, Complete).
/// </summary>
public static class CursorControl
{
    public static void SetLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
