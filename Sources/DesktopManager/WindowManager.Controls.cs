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
}
