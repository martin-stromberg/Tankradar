using System.Runtime.InteropServices;
using NativeWindow = Microsoft.UI.Xaml.Window;

namespace Tankradar.MAUI.Platforms.Windows;

/// <summary>
/// Hält das Hauptfenster im Testmodus (<c>TANKATLAS_TEST_WINDOW=offscreen</c>) außerhalb des sichtbaren Bildschirmbereichs
/// und verhindert, dass es sich in den Vordergrund holt oder einen Taskleisteneintrag erhält.
/// Die Oberflächenautomatisierung der E2E-Tests bleibt möglich, den Anwender stört das Fenster nicht.
/// </summary>
internal static class OffscreenWindow
{
    private const int OffscreenCoordinate = -32000;
    private const int GwlExStyle = -20;
    private const long WsExToolWindow = 0x00000080L;
    private const long WsExNoActivate = 0x08000000L;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;

    /// <summary>
    /// Verschiebt das Fenster aus dem sichtbaren Bereich und markiert es als nicht aktivierbares Werkzeugfenster.
    /// </summary>
    /// <param name="window">Das native WinUI-Fenster.</param>
    public static void Apply(NativeWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);

        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var style = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        SetWindowLongPtr(handle, GwlExStyle, new IntPtr(style | WsExToolWindow | WsExNoActivate));
        MoveOffscreen(handle);

        // Die Handler bleiben bewusst angemeldet (einmaliges Hauptfenster im Testmodus); Rückgabewerte der Win32-Aufrufe sind hier unkritisch.
        // Die Plattform setzt die Fensterposition beim Aktivieren und bei Größenänderungen ggf. neu; daher erneut wegschieben.
        window.Activated += (_, _) => MoveOffscreen(handle);
        window.SizeChanged += (_, _) => MoveOffscreen(handle);
    }

    private static void MoveOffscreen(IntPtr handle)
    {
        SetWindowPos(handle, IntPtr.Zero, OffscreenCoordinate, OffscreenCoordinate, 0, 0, SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
}
