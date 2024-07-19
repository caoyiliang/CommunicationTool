using Microsoft.Xaml.Behaviors;
using System.Windows;

namespace CommunicationTool.Behaviors
{
    internal class ClosePopupBehavior : Behavior<Window>
    {
        public bool PopupVisible
        {
            get { return (bool)GetValue(PopupVisibleProperty); }
            set { SetValue(PopupVisibleProperty, value); }
        }

        public static readonly DependencyProperty PopupVisibleProperty =
            DependencyProperty.Register("PopupVisible", typeof(bool), typeof(ClosePopupBehavior), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        protected override void OnAttached()
        {
            AssociatedObject.LocationChanged += AssociatedObject_LocationChanged;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.LocationChanged -= AssociatedObject_LocationChanged;
            base.OnDetaching();
        }

        private void AssociatedObject_LocationChanged(object? sender, EventArgs e)
        {
            PopupVisible = false;
        }
    }
}
