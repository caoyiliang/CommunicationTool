using CommunicationTool.ViewModel;
using Config;
using Config.Model;
using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace CommunicationTool.Behaviors
{
    internal class OpenNewTestBehavior : Behavior<Button>
    {
        public Connection Connection
        {
            get { return (Connection)GetValue(ConnectionProperty); }
            set { SetValue(ConnectionProperty, value); }
        }

        public static readonly DependencyProperty ConnectionProperty =
            DependencyProperty.Register("Connection", typeof(Connection), typeof(OpenNewTestBehavior), default);

        public TestType TestType
        {
            get { return (TestType)GetValue(TestTypeProperty); }
            set { SetValue(TestTypeProperty, value); }
        }

        public static readonly DependencyProperty TestTypeProperty =
            DependencyProperty.Register("TestType", typeof(TestType), typeof(OpenNewTestBehavior), new FrameworkPropertyMetadata(TestType.SerialPort, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));


        protected override void OnAttached()
        {
            AssociatedObject.Click += AssociatedObject_Click;
            base.OnAttached();
        }

        private async void AssociatedObject_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            switch (TestType)
            {
                case TestType.SerialPort:
                    {
                        var config = new Config.Model.TestConfig(new SerialPortConnection()) { Id = Guid.NewGuid() };
                        Connection.TestConfigs.Add(config);
                        var test = new View.TopPortTest { DataContext = new TopPortViewModel(Connection, config) };
                        test.Show();
                    }
                    break;
                case TestType.TcpClient:
                    break;
                case TestType.TcpServer:
                    break;
                case TestType.ClassicBluetoothClient:
                    break;
                case TestType.ClassicBluetoothServer:
                    break;
                default:
                    break;
            }
            await Connection.TrySaveChangeAsync();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Click -= AssociatedObject_Click;
            base.OnDetaching();
        }
    }
}
