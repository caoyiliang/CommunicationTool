﻿using CommunicationTool.ViewModel;
using Config.Model;
using Microsoft.Xaml.Behaviors;
using System.Windows;

namespace CommunicationTool.Behaviors
{
    internal class InitBehavior : Behavior<Window>
    {
        public Connection Connection
        {
            get { return (Connection)GetValue(ConnectionProperty); }
            set { SetValue(ConnectionProperty, value); }
        }

        public static readonly DependencyProperty ConnectionProperty =
            DependencyProperty.Register(
                "Connection",
                typeof(Connection),
                typeof(InitBehavior),
                default);

        protected override void OnAttached()
        {
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            base.OnAttached();
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var item in Connection.TestConfigs)
            {
                var test = new View.Test { DataContext = new TestViewModel(Connection, item) };
                test.Show();
            }
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            base.OnDetaching();
        }
    }
}
