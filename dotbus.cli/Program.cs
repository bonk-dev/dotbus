using System.Net.Sockets;
using dotbus;
using dotbus.Modbus;

var client = new TcpClient()
{
    NoDelay = true
};

await client.ConnectAsync(args[0], 502);
var stream = client.GetStream();

await using var mClient = new ModbusTcpClient(stream, 0x01);
var coils = new Memory<bool>(new bool[2000]);

for (var i = 0; i < 10000; ++i)
{
    await mClient.ReadCoilsAsync(coils, 0, ReadCoilsRequest.MaxCoilAmount);
}

Console.WriteLine("Done");