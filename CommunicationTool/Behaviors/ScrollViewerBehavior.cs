using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace CommunicationTool.Behaviors
{
    internal class ScrollViewerBehavior : Behavior<ScrollViewer>
    {
        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.Register(
                nameof(AutoScroll),
                typeof(bool),
                typeof(ScrollViewerBehavior),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool AutoScroll
        {
            get { return (bool)GetValue(AutoScrollProperty); }
            set { SetValue(AutoScrollProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.ScrollChanged += OnScrollChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.ScrollChanged -= OnScrollChanged;
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (AutoScroll && e.ExtentHeightChange > 0)
            {
                AssociatedObject.ScrollToEnd();
            }
        }
    }
}
