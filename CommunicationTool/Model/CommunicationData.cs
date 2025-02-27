﻿using CommunityToolkit.Mvvm.ComponentModel;
using Config;
using System.Text;

namespace CommunicationTool.Model
{
    public partial class CommunicationData(byte[] bytes, DataType showType, TransferDirection transferDirection = TransferDirection.Request) : ObservableObject
    {
        public DateTime DateTime { get; init; } = DateTime.Now;

        public string Message
        {
            get
            {
                try
                {
                    return ShowType switch
                    {
                        DataType.ASCII => Encoding.ASCII.GetString(bytes),
                        DataType.UTF8 => Encoding.UTF8.GetString(bytes),
                        DataType.GB2312 => Encoding.GetEncoding("GB2312").GetString(bytes),
                        _ => Utils.StringByteUtils.BytesToString(bytes),
                    };
                }
                catch
                {
                    return $"不适合该编码:{Utils.StringByteUtils.BytesToString(bytes)}";
                }
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Message))]
        private DataType _showType = showType;

        [ObservableProperty]
        private TransferDirection _transferDirection = transferDirection;
    }
}
