using Microsoft.Xaml.Behaviors;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Utils;

namespace CommunicationTool.Behaviors
{
    internal class DynamicContextMenuBehavior : Behavior<TextBox>
    {
        private bool _isHighByteBefore = false; // 默认低字节在前
        protected override void OnAttached()
        {
            AssociatedObject.ContextMenuOpening += OnContextMenuOpening;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.ContextMenuOpening -= OnContextMenuOpening;
            base.OnDetaching();
        }

        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (AssociatedObject.ContextMenu == null) return;

            var selectedText = AssociatedObject.SelectedText;
            AssociatedObject.ContextMenu.Items.Clear();

            // 添加低字节在前的菜单项
            var lowByteMenuItem = new MenuItem
            {
                Header = "低字节在前",
                IsCheckable = true,
                IsChecked = !_isHighByteBefore
            };
            lowByteMenuItem.Click += (s, _) => _isHighByteBefore = !lowByteMenuItem.IsChecked;
            AssociatedObject.ContextMenu.Items.Add(lowByteMenuItem);

            string[] SelectData = selectedText.TrimEnd().TrimStart().Split(' ');
            foreach (string data in SelectData)
            {
                if (!Regex.IsMatch(data, "^[0-9A-Fa-f]+$"))
                {
                    return;
                }
            }
            if (SelectData.Length == 2)
            {
                AssociatedObject.ContextMenu.Items.Add(CreateMenuItem("转换为 Int", () => ConvertToInt(selectedText)));
                AssociatedObject.ContextMenu.Items.Add(CreateMenuItem("转换为 UInt", () => ConvertToUInt(selectedText)));
            }
            else if (SelectData.Length == 4)
            {
                AssociatedObject.ContextMenu.Items.Add(CreateMenuItem("转换为 Int", () => ConvertToInt(selectedText)));
                AssociatedObject.ContextMenu.Items.Add(CreateMenuItem("转换为 UInt", () => ConvertToUInt(selectedText)));
                AssociatedObject.ContextMenu.Items.Add(CreateMenuItem("转换为 Float", () => ConvertToFloat(selectedText)));
            }
            else if (SelectData.Length == 8)
            {
                AssociatedObject.ContextMenu.Items.Add(CreateMenuItem("转换为 Int", () => ConvertToInt(selectedText)));
                AssociatedObject.ContextMenu.Items.Add(CreateMenuItem("转换为 UInt", () => ConvertToUInt(selectedText)));
                AssociatedObject.ContextMenu.Items.Add(CreateMenuItem("转换为 Double", () => ConvertToDouble(selectedText)));
            }
            AssociatedObject.ContextMenu.Items.Add(CreateMenuItem("转换为 Ascii", () => ConvertToAscii(selectedText)));
            AssociatedObject.ContextMenu.Items.Add(CreateMenuItem("转换为 Gb2312", () => ConvertToGb2312(selectedText)));
        }

        private void ConvertToGb2312(string selectedText)
        {
            try
            {
                var bytes = StringByteUtils.StringToBytes(selectedText);
                var result = Encoding.GetEncoding("gb2312").GetString(bytes);
                MessageBox.Show($"转换结果: {result}", "转换为 Gb2312");
            }
            catch
            {
                MessageBox.Show("转换失败，请检查数据格式。", "错误");
            }
        }

        private void ConvertToAscii(string selectedText)
        {
            try
            {
                var bytes = StringByteUtils.StringToBytes(selectedText);
                var result = Encoding.ASCII.GetString(bytes);
                MessageBox.Show($"转换结果: {result}", "转换为 Ascii");
            }
            catch
            {
                MessageBox.Show("转换失败，请检查数据格式。", "错误");
            }
        }

        private MenuItem CreateMenuItem(string header, Action action)
        {
            var menuItem = new MenuItem { Header = header };
            menuItem.Click += (s, e) => action();
            return menuItem;
        }

        private void ConvertToInt(string selectedText)
        {
            try
            {
                var bytes = StringByteUtils.StringToBytes(selectedText);
                dynamic result = bytes.Length switch
                {
                    2 => StringByteUtils.ToInt16(bytes, 0, _isHighByteBefore),
                    4 => StringByteUtils.ToInt32(bytes, 0, _isHighByteBefore),
                    _ => (dynamic)StringByteUtils.ToInt64(bytes, 0, _isHighByteBefore),
                };
                MessageBox.Show($"转换结果: {result}", "转换为 Int");
            }
            catch
            {
                MessageBox.Show("转换失败，请检查数据格式。", "错误");
            }
        }

        private void ConvertToUInt(string selectedText)
        {
            try
            {
                var bytes = StringByteUtils.StringToBytes(selectedText);
                dynamic result = bytes.Length switch
                {
                    2 => StringByteUtils.ToUInt16(bytes, 0, _isHighByteBefore),
                    4 => StringByteUtils.ToUInt32(bytes, 0, _isHighByteBefore),
                    _ => (dynamic)StringByteUtils.ToUInt64(bytes, 0, _isHighByteBefore),
                };
                MessageBox.Show($"转换结果: {result}", "转换为 UInt");
            }
            catch
            {
                MessageBox.Show("转换失败，请检查数据格式。", "错误");
            }
        }

        private void ConvertToFloat(string selectedText)
        {
            try
            {
                var bytes = StringByteUtils.StringToBytes(selectedText);
                float result = StringByteUtils.ToSingle(bytes, 0, _isHighByteBefore);
                MessageBox.Show($"转换结果: {result}", "转换为 Float");
            }
            catch
            {
                MessageBox.Show("转换失败，请检查数据格式。", "错误");
            }
        }

        private void ConvertToDouble(string selectedText)
        {
            try
            {
                var bytes = StringByteUtils.StringToBytes(selectedText);
                double result = StringByteUtils.ToDouble(bytes, 0, _isHighByteBefore);
                MessageBox.Show($"转换结果: {result}", "转换为 Double");
            }
            catch
            {
                MessageBox.Show("转换失败，请检查数据格式。", "错误");
            }
        }
    }
}
