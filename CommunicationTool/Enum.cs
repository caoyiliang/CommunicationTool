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
    HEX,
    ASCII,
    UTF8,
    GB2312
}

public enum TransferDirection
{
    Request,
    Response
}

public enum CrcType
{
    None,
    Modbus,
    Modbus_R,
    GB,
    GB_string,
    GB_protocol,
    HB,
    HB_string,
    HB_protocol,
    UCRC
}
