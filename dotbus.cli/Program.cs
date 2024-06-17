using System.Diagnostics;
using System.Net.Sockets;
using dotbus;
using dotbus.Modbus.Requests;

var client = new TcpClient()
{
    NoDelay = true
};

await client.ConnectAsync(args[0], 502);
var stream = client.GetStream();

await using var mClient = new ModbusTcpClient(stream, 0x01);
for (var i = 0; i < 10000; ++i)
{
    await mClient.ReadCoilsAsync(0, ReadCoilsRequest.MaxCoilAmount);
    // mClient.ReadCoils(0, ReadCoilsRequest.MaxCoilAmount); 
}

Debugger.Break();

Console.WriteLine("Done");