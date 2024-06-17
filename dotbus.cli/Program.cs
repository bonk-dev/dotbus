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
var dInputs = new Memory<bool>(new bool[2000]);
var holdingRegs = new Memory<ushort>(new ushort[145]);
var inputRegs = new Memory<ushort>(new ushort[8]);

for (var i = 0; i < 100; ++i)
{
    await mClient.ReadCoilsAsync(coils, 0, Requests.MaxCoilAmount);
}
for (var i = 0; i < 100; ++i)
{
    await mClient.ReadDiscreteInputsAsync(dInputs, 0, 24);
}
for (var i = 0; i < 100; ++i)
{
    await mClient.ReadHoldingRegistersAsync(holdingRegs, 0, 125);
}
for (var i = 0; i < 100; ++i)
{
    await mClient.ReadInputRegistersAsync(inputRegs, 0, 8);
}

await mClient.WriteSingleCoilAsync(173, true);
await mClient.ReadCoilsAsync(coils, 173, 1);

await mClient.ReadHoldingRegistersAsync(holdingRegs, 200, 1);
await mClient.WriteSingleRegisterAsync(200, 0xDEAD);
await mClient.ReadHoldingRegistersAsync(holdingRegs, 200, 1);

await mClient.ReadCoilsAsync(coils, 300, 9);
await mClient.WriteMultipleCoilsAsync(300, 
    new bool[] { true, false, true, false, true, false, true, false, true }
);
await mClient.ReadCoilsAsync(coils, 300, 9);

await mClient.ReadHoldingRegistersAsync(holdingRegs, 300, 7);
await mClient.WriteMultipleRegistersAsync(300, 
    new ushort[] { 0xDEAD, 0xDEAD, 0xDEAD, 0xDEAD, 0xDEAD, 0xDEAD, 0xDEAD }
);
await mClient.ReadHoldingRegistersAsync(holdingRegs, 300, 7);

Console.WriteLine("Done");