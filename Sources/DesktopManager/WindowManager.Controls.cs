namespace DesktopManager;

public partial class WindowManager {
    /// <summary>
    /// Clicks a window control using the specified mouse button.
    /// </summary>
    /// <param name="control">The control to click.</param>
    /// <param name="button">Mouse button to use.</param>
    public void ClickControl(WindowControlInfo control, MouseButton button) {
        WindowControlService.ControlClick(control, button);
    }

    /// <summary>
    /// Gets the check state of a button control.
    /// </summary>
    /// <param name="control">The control to query.</param>
    /// <returns><c>true</c> if checked; otherwise <c>false</c>.</returns>
    public bool GetControlCheckState(WindowControlInfo control) {
        return WindowControlService.GetCheckState(control);
    }

    /// <summary>
    /// Sets the check state of a button control.
    /// </summary>
    /// <param name="control">The control to modify.</param>
    /// <param name="check">Desired check state.</param>
    public void SetControlCheckState(WindowControlInfo control, bool check) {
        WindowControlService.SetCheckState(control, check);
    }
}
