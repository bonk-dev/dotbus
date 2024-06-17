using CommunityToolkit.HighPerformance.Buffers;
using dotbus.Exceptions;
using dotbus.Modbus;
using dotbus.Transport;

namespace dotbus;

public class ModbusTcpClient : IDisposable, IAsyncDisposable
{
    private const int PacketBufferSize = 260;
    
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
        using var owner = MemoryOwner<byte>.Allocate(PacketBufferSize);
        var memBuffer = owner.Memory;

        var (written, sentTransactionId) = WriteHeader(
            owner.Span,
            Requests.RequestLength
        );
        written += Requests.Serialize(
            owner.Span[written..],
            EFunctionCode.ReadCoils,
            (ushort)startingAddress,
            (ushort)amount
        );

        await _stream.WriteAsync(memBuffer[..written], cancellationToken);
        await _stream.FlushAsync(cancellationToken);

        var readBytes = await _stream.ReadAsync(memBuffer, cancellationToken);
        var (readOffset, receivedTransactionId, length) = ModbusTcpHeader.ReadModbusTcpHeader(
            owner.Span[..readBytes]);

        if (sentTransactionId != receivedTransactionId)
        {
            throw new TransactionIdMismatchException(
                sentTransactionId, 
                receivedTransactionId);
        }

        Requests.DeserializeReadBits(
            destination.Span,
            owner.Span.Slice(readOffset, length));
    }

    private (int writtenBytes, ushort transactionId) WriteHeader(Span<byte> destination, int requestLength)
    {
        var id = _nextTransactionId++;
        return (ModbusTcpHeader.WriteModbusTcpHeader(
            destination,
            id,
            requestLength,
            _unitId), id
        );
    }

    public void Dispose() => 
        _stream.Dispose();

    public async ValueTask DisposeAsync() =>
        await _stream.DisposeAsync();
}