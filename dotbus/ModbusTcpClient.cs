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

    public async Task ReadCoilsAsync(
        Memory<bool> destination,
        int startingAddress,
        int amount, 
        CancellationToken cancellationToken = default)
    {
        using var owner = MemoryOwner<byte>.Allocate(260);
        var memBuffer = owner.Memory;

        var written = WriteHeader(
            owner.Span,
            ReadCoilsRequest.RequestLength);
        written += ReadCoilsRequest.Serialize(
            owner.Span[written..],
            (ushort)startingAddress,
            (ushort)amount);

        await _stream.WriteAsync(memBuffer[..written], cancellationToken);
        await _stream.FlushAsync(cancellationToken);

        var readBytes = await _stream.ReadAsync(memBuffer, cancellationToken);
        
        // TODO: Verify transaction id
        var (readOffset, transactionId, length) = ModbusTcpHeader.ReadModbusTcpHeader(
            owner.Span[..readBytes]);

        ReadCoilsRequest.Deserialize(
            destination.Span,
            owner.Span.Slice(readOffset, length));
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