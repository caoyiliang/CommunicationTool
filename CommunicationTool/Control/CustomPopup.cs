using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;

namespace CommunicationTool.Control
{
    public class CustomPopup : Popup
    {
        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            var hwndSource = (HwndSource)PresentationSource.FromVisual(this.Child);
            if (hwndSource != null)
            {
                var hwnd = hwndSource.Handle;
                SetWindowPos(hwnd, new IntPtr(-2), 0, 0, 0, 0, 0x0001 | 0x0002);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    }
}
