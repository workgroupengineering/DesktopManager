using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopManager;

/// <summary>
/// Native process-related platform invocations.
/// </summary>
public static partial class MonitorNativeMethods {
    /// <summary>
    /// Opens a handle to an existing process.
    /// </summary>
    /// <param name="dwDesiredAccess">Requested access rights.</param>
    /// <param name="bInheritHandle">Whether the handle is inheritable.</param>
    /// <param name="dwProcessId">Process identifier.</param>
    /// <returns>Handle to the process, or IntPtr.Zero on failure.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    /// <summary>
    /// Closes an open object handle.
    /// </summary>
    /// <param name="hObject">Handle to close.</param>
    /// <returns>True on success.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    /// <summary>
    /// Retrieves the full path of the executable image for the specified process.
    /// </summary>
    /// <param name="hProcess">Handle to the process.</param>
    /// <param name="dwFlags">Query flags.</param>
    /// <param name="lpExeName">Buffer that receives the path.</param>
    /// <param name="lpdwSize">Size of the buffer, in characters.</param>
    /// <returns>True on success.</returns>
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);

    /// <summary>
    /// Opens the access token associated with a process.
    /// </summary>
    /// <param name="processHandle">Handle to the process.</param>
    /// <param name="desiredAccess">Requested access rights.</param>
    /// <param name="tokenHandle">Receives the token handle.</param>
    /// <returns>True on success.</returns>
    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    /// <summary>
    /// Retrieves a specified type of information about an access token.
    /// </summary>
    /// <param name="tokenHandle">Handle to the token.</param>
    /// <param name="tokenInformationClass">Information class to retrieve.</param>
    /// <param name="tokenInformation">Buffer to receive the data.</param>
    /// <param name="tokenInformationLength">Size of the buffer.</param>
    /// <param name="returnLength">Required buffer size.</param>
    /// <returns>True on success.</returns>
    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool GetTokenInformation(
        IntPtr tokenHandle,
        TOKEN_INFORMATION_CLASS tokenInformationClass,
        IntPtr tokenInformation,
        int tokenInformationLength,
        out int returnLength);

    /// <summary>
    /// Process access right for querying limited information.
    /// </summary>
    public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    /// <summary>
    /// Process access right for querying information.
    /// </summary>
    public const uint PROCESS_QUERY_INFORMATION = 0x0400;

    /// <summary>
    /// Token access right for querying information.
    /// </summary>
    public const uint TOKEN_QUERY = 0x0008;

    /// <summary>
    /// Token information class identifiers.
    /// </summary>
    public enum TOKEN_INFORMATION_CLASS {
        /// <summary>
        /// Retrieves elevation information for the token.
        /// </summary>
        TokenElevation = 20
    }

    /// <summary>
    /// Token elevation information.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_ELEVATION {
        /// <summary>Non-zero if the token is elevated.</summary>
        public int TokenIsElevated;
    }
}
