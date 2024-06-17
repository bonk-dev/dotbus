using System.Buffers;
using System.Net.Sockets;
using CommunityToolkit.HighPerformance.Buffers;
using dotbus.Modbus.Requests;
using dotbus.Transport;

namespace dotbus;

public class ModbusTcpClient : IDisposable, IAsyncDisposable
{
    private readonly Stream _stream;
    private readonly byte _unitId;

    private ushort _nextTransactionId = 0x01; 

    public ModbusTcpClient(Stream stream, byte unitId)
    {
        _stream = stream;
        _unitId = unitId;
    }

    public async Task ReadCoilsAsync(int startingAddress, int amount, CancellationToken cancellationToken = default)
    {
        using var owner = MemoryOwner<byte>.Allocate(260);
        var buffer = owner.Memory;

        var written = WriteHeader(
            owner.Span,
            ReadCoilsRequest.RequestLength);
        written += ReadCoilsRequest.Serialize(
            owner.Span[written..],
            (ushort)startingAddress,
            (ushort)amount);

        await _stream.WriteAsync(buffer[..written], cancellationToken);
        await _stream.FlushAsync(cancellationToken);

        var read = await _stream.ReadAsync(buffer, cancellationToken);
    }

    public void ReadCoils(int startingAddress, int amount)
    {
        Span<byte> buffer = stackalloc byte[260];

        var written = WriteHeader(
            buffer,
            ReadCoilsRequest.RequestLength);
        written += ReadCoilsRequest.Serialize(
            buffer[written..],
            (ushort)startingAddress,
            (ushort)amount);

        _stream.Write(buffer[..written]);
        var read = _stream.Read(buffer);
    }

    private int WriteHeader(Span<byte> destination, int requestLength) =>
        ModbusTcpHeader.WriteModbusTcpHeader(
            destination, 
            _nextTransactionId++,
            requestLength,
            _unitId);

    public void Dispose() => 
        _stream.Dispose();

    public async ValueTask DisposeAsync() =>
        await _stream.DisposeAsync();
}