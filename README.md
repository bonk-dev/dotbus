# .bus
dotbus is a lightweight and efficient Modbus/TCP library for C#. 
Designed with a focus on minimal memory allocation.

## Usage
```csharp
var client = new TcpClient()
{
    NoDelay = true
};

// Connect the TcpClient
await client.ConnectAsync("192.168.0.10", 502);
var stream = client.GetStream();

// Create the ModbusTcpClient (slave id: 0x01)
await using var mClient = new ModbusTcpClient(stream, 0x01);

// Allocate buffer for coils
var coils = new Memory<bool>(new bool[2000]);

// Read coils
await mClient.ReadCoilsAsync(coils, 0, Requests.MaxCoilAmount);
```

See full usage in the ```dotbus.cli``` project.