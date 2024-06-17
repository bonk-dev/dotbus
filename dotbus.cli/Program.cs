using System.Net.Sockets;
using dotbus;
using dotbus.Exceptions;
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

var write1 = new bool[] { true, false, true, false, true, false, true, false, true };
var write2 = new ushort[] { 0xDEAD, 0xDEAD, 0xDEAD, 0xDEAD, 0xDEAD, 0xDEAD, 0xDEAD };

for (var j = 0; j < 1000; ++j)
{
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
    await mClient.WriteMultipleCoilsAsync(300, write1);
    await mClient.ReadCoilsAsync(coils, 300, 9);

    await mClient.ReadHoldingRegistersAsync(holdingRegs, 300, 7);
    await mClient.WriteMultipleRegistersAsync(300, write2);
    await mClient.ReadHoldingRegistersAsync(holdingRegs, 300, 7);
}

try
{
    // This should throw an exception
    await mClient.ReadHoldingRegistersAsync(holdingRegs, 0, 300);
}
catch (ModbusException ex)
{
    Console.WriteLine("ModbusException handled: " + ex.Message);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine("AOR Exception on write handled: " + ex.Message);
}

try
{
    // This should also throw an exception
    await mClient.WriteMultipleRegistersAsync(1000, new ushort[1]);
}
catch (ModbusException ex)
{
    Console.WriteLine("ModbusException on write handled: " + ex.Message);
}

Console.WriteLine("Done");