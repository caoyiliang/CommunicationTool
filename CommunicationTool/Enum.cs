﻿namespace CommunicationTool;

public enum TestType
{
    SerialPort,
    TcpClient,
    TcpServer,
    ClassicBluetoothClient,
    ClassicBluetoothServer
}

public enum TransferDirection
{
    Request,
    Response
}
