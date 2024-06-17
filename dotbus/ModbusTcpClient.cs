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
        CancellationToken cancellationToken = default
    ) => await ReadBitsAsync(destination, EFunctionCode.ReadCoils, startingAddress, amount, cancellationToken);
    
    public async Task ReadDiscreteInputsAsync(
        Memory<bool> destination,
        int startingAddress,
        int amount,
        CancellationToken cancellationToken = default
    ) => await ReadBitsAsync(destination, EFunctionCode.ReadDiscreteInputs, startingAddress, amount, cancellationToken);
    
    public async Task ReadHoldingRegistersAsync(
        Memory<ushort> destination,
        int startingAddress,
        int amount,
        CancellationToken cancellationToken = default
    ) => await ReadWordsAsync(destination, EFunctionCode.ReadHoldingRegisters, startingAddress, amount, cancellationToken);

    private async Task ReadBitsAsync(
        Memory<bool> destination,
        EFunctionCode functionCode,
        int startingAddress,
        int amount, 
        CancellationToken cancellationToken = default)
    {
        var buffer = MemoryOwner<byte>.Allocate(PacketBufferSize);
        var (readOffset, length) = await DoRequestAsync(
            buffer, 
            functionCode, 
            startingAddress, 
            amount, 
            cancellationToken
        );

        Requests.DeserializeBits(
            destination.Span,
            buffer.Span.Slice(readOffset, length));
    }
    
    private async Task ReadWordsAsync(
        Memory<ushort> destination,
        EFunctionCode functionCode,
        int startingAddress,
        int amount, 
        CancellationToken cancellationToken = default)
    {
        var buffer = MemoryOwner<byte>.Allocate(PacketBufferSize);
        var (readOffset, length) = await DoRequestAsync(
            buffer, 
            functionCode, 
            startingAddress, 
            amount, 
            cancellationToken
        );

        Requests.DeserializeWords(
            destination.Span,
        buffer.Span.Slice(readOffset, length));
    }

    private async Task<(int readOffset, int length)> DoRequestAsync(
        MemoryOwner<byte> destination,
        EFunctionCode functionCode,
        int startingAddress,
        int amount, 
        CancellationToken cancellationToken = default)
    {
        var memBuffer = destination.Memory;
        
        var (written, sentTransactionId) = WriteHeader(
            destination.Span,
            Requests.RequestLength
        );
        written += Requests.Serialize(
            destination.Span[written..],
            functionCode,
            (ushort)startingAddress,
            (ushort)amount
        );

        await _stream.WriteAsync(memBuffer[..written], cancellationToken);
        await _stream.FlushAsync(cancellationToken);

        var readBytes = await _stream.ReadAsync(memBuffer, cancellationToken);
        var (readOffset, receivedTransactionId, length) = ModbusTcpHeader.ReadModbusTcpHeader(
            destination.Span[..readBytes]);

        if (sentTransactionId != receivedTransactionId)
        {
            throw new TransactionIdMismatchException(
                sentTransactionId, 
                receivedTransactionId);
        }

        return (readOffset, length);
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