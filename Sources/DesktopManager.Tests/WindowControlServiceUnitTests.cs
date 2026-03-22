namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Unit tests for printable-key translation in direct control messaging.
/// </summary>
public class WindowControlServiceUnitTests {
    [TestMethod]
    /// <summary>
    /// Ensures letter keys translate to uppercase printable characters.
    /// </summary>
    public void TryGetPrintableCharacter_LetterKey_ReturnsUppercaseCharacter() {
        bool translated = WindowControlService.TryGetPrintableCharacter(VirtualKey.VK_H, noModifiersHeld: true, out char character);

        Assert.IsTrue(translated);
        Assert.AreEqual('H', character);
    }

    [TestMethod]
    /// <summary>
    /// Ensures digit keys translate to printable characters.
    /// </summary>
    public void TryGetPrintableCharacter_DigitKey_ReturnsDigitCharacter() {
        bool translated = WindowControlService.TryGetPrintableCharacter(VirtualKey.VK_7, noModifiersHeld: true, out char character);

        Assert.IsTrue(translated);
        Assert.AreEqual('7', character);
    }

    [TestMethod]
    /// <summary>
    /// Ensures the space key translates to a printable space.
    /// </summary>
    public void TryGetPrintableCharacter_SpaceKey_ReturnsSpaceCharacter() {
        bool translated = WindowControlService.TryGetPrintableCharacter(VirtualKey.VK_SPACE, noModifiersHeld: true, out char character);

        Assert.IsTrue(translated);
        Assert.AreEqual(' ', character);
    }

    [TestMethod]
    /// <summary>
    /// Ensures printable translation is blocked while modifiers are held.
    /// </summary>
    public void TryGetPrintableCharacter_WithModifiersHeld_ReturnsFalse() {
        bool translated = WindowControlService.TryGetPrintableCharacter(VirtualKey.VK_A, noModifiersHeld: false, out char character);

        Assert.IsFalse(translated);
        Assert.AreEqual('\0', character);
    }

    [TestMethod]
    /// <summary>
    /// Ensures non-printable keys are not translated to characters.
    /// </summary>
    public void TryGetPrintableCharacter_NonPrintableKey_ReturnsFalse() {
        bool translated = WindowControlService.TryGetPrintableCharacter(VirtualKey.VK_RETURN, noModifiersHeld: true, out char character);

        Assert.IsFalse(translated);
        Assert.AreEqual('\0', character);
    }
}
