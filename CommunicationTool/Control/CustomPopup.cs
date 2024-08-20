using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;

namespace CommunicationTool.Control
{
    public class CustomPopup : Popup
    {
        public CustomPopup()
        {
            this.Loaded += CustomPopup_Loaded;
        }

        private void CustomPopup_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.PlacementTarget is Window window)
            {
                window.LocationChanged += Window_LocationChanged;
            }
        }

        private void Window_LocationChanged(object? sender, EventArgs e)
        {
            if (IsOpen)
            {
                var mi = typeof(Popup).GetMethod("UpdatePosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                mi?.Invoke(this, null);
            }
        }

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
