namespace Config;

public enum TestType
{
    SerialPort,
    TcpClient,
    TcpServer,
    UdpClient,
    ClassicBluetoothClient,
    ClassicBluetoothServer
}

public enum ParserType
{
    TimeParser,
    HeadLengthParser,
    HeadFootParser,
    FootParser,
}

public enum LengthDataType
{
    Int16,
    UInt16,
    Int32,
    Float,
    Double,
    固定长度
}

public enum DataType
{
    HEX,
    ASCII,
    UTF8,
    GB2312
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
