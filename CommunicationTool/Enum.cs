namespace CommunicationTool;

public enum TestType
{
    SerialPort,
    TcpClient,
    TcpServer,
    ClassicBluetoothClient,
    ClassicBluetoothServer
}

public enum DataType
{
    Hex,
    Ascii,
    Utf8,
    Gb2312
}

public enum TransferDirection
{
    Request,
    Response
}
