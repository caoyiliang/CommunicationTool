using Microsoft.Xaml.Behaviors;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace CommunicationTool.Behaviors
{
    internal class CloseBehavior : Behavior<Window>
    {
        public ICommand? Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }
        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register("Command", typeof(ICommand), typeof(CloseBehavior), default);

        private bool _isUserClosing = false;
        protected override void OnAttached()
        {
            AssociatedObject.SourceInitialized += AssociatedObject_SourceInitialized;
            AssociatedObject.Closing += AssociatedObject_Closing;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.SourceInitialized -= AssociatedObject_SourceInitialized;
            AssociatedObject.Closing -= AssociatedObject_Closing;
            base.OnDetaching();
        }

        private void AssociatedObject_SourceInitialized(object? sender, EventArgs e)
        {
            var hwndSource = PresentationSource.FromVisual(AssociatedObject) as HwndSource;
            hwndSource?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_CLOSE = 0xF060;

            if (msg == WM_SYSCOMMAND && wParam.ToInt32() == SC_CLOSE)
            {
                _isUserClosing = true;
            }

            return IntPtr.Zero;
        }

        private void AssociatedObject_Closing(object? sender, CancelEventArgs e)
        {
            if (_isUserClosing)
            {
                _isUserClosing = false;
                var result = MessageBox.Show("你确定要结束该测试？", "确认关闭", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    Command?.Execute(null);
                }
            }
        }
    }
}
