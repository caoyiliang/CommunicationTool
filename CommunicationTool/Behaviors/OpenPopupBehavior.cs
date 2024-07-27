using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace CommunicationTool.Behaviors
{
    internal class OpenPopupBehavior : Behavior<Button>
    {
        public bool Show
        {
            get { return (bool)GetValue(ShowProperty); }
            set { SetValue(ShowProperty, value); }
        }

        public static readonly DependencyProperty ShowProperty =
            DependencyProperty.Register("Show", typeof(bool), typeof(OpenPopupBehavior), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));


        protected override void OnAttached()
        {
            AssociatedObject.Click += AssociatedObject_Click;
            base.OnAttached();
        }

        private void AssociatedObject_Click(object sender, RoutedEventArgs e)
        {
            Show = !Show;
        }
    }
}
