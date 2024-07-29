using Microsoft.Xaml.Behaviors;
using System.Windows.Controls;
using System.Windows.Input;

namespace CommunicationTool.Behaviors
{
    internal class TitleNameEditBehavior : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            AssociatedObject.GotFocus += AssociatedObject_GotFocus;
            AssociatedObject.LostFocus += AssociatedObject_LostFocus;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.GotFocus -= AssociatedObject_GotFocus;
            AssociatedObject.LostFocus -= AssociatedObject_LostFocus;
            base.OnDetaching();
        }

        private void AssociatedObject_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            AssociatedObject.IsReadOnly = false;
            AssociatedObject.BorderThickness = new System.Windows.Thickness(1);
            AssociatedObject.Background = System.Windows.Media.Brushes.White;
        }

        private void AssociatedObject_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            AssociatedObject.IsReadOnly = true;
            AssociatedObject.BorderThickness = new System.Windows.Thickness(0);
            AssociatedObject.Background = System.Windows.Media.Brushes.Transparent;
        }
    }
}
