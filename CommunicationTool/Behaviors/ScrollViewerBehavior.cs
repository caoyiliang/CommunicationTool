using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace CommunicationTool.Behaviors
{
    public class ScrollViewerBehavior : Behavior<ScrollViewer>
    {
        public bool AutoScroll
        {
            get { return (bool)GetValue(AutoScrollProperty); }
            set { SetValue(AutoScrollProperty, value); }
        }

        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.Register("AutoScroll", typeof(bool), typeof(ScrollViewerBehavior), new PropertyMetadata(true));

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
