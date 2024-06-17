using System.Diagnostics;
using System.Net.Sockets;
using dotbus.Modbus.Requests;
using dotbus.Transport;

var client = new TcpClient()
{
    NoDelay = true
};

await client.ConnectAsync("192.168.2.110", 502);
var stream = client.GetStream();

var request = new byte[ModbusTcpHeader.ModbusTcpHeaderLength + ReadCoilsRequest.RequestLength];

ushort addr = 0;
ushort amount = 2000;

ushort transactionId = 0x01;
byte unitId = 0x01;

var readBuffer = new byte[260];
for (var i = 0; i < 1000000; ++i) {
    ModbusTcpHeader.WriteModbusTcpHeader(
        request.AsSpan(), 
        transactionId++, 
        ReadCoilsRequest.RequestLength,
        unitId);
    ReadCoilsRequest.Serialize(
        request.AsSpan(ModbusTcpHeader.ModbusTcpHeaderLength), 
        addr,
        amount);
    
    await stream.WriteAsync(request);
    var read = await stream.ReadAsync(readBuffer);
}

Debugger.Break();

Console.WriteLine("Done");