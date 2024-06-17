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

    public async Task WriteSingleCoilAsync(
        int address, 
        bool value, 
        CancellationToken cancellationToken = default)
    {
        const int coilLow = 0x0000;
        const int coilHigh = 0xFF00;
        var coilValue = value
            ? coilHigh
            : coilLow;

        await DoRequestDiscardAsync(
            EFunctionCode.WriteSingleCoil,
            address, 
            coilValue, 
            cancellationToken
        );
    }
    
    public async Task WriteSingleRegisterAsync(
        int address, 
        int value, 
        CancellationToken cancellationToken = default)
    {
        await DoRequestDiscardAsync(
            EFunctionCode.WriteSingleRegister,
            address, 
            value, 
            cancellationToken
        );
    }

    public async Task WriteMultipleCoilsAsync(
        int startingAddress,
        ReadOnlyMemory<bool> values,
        CancellationToken cancellationToken = default)
    {
        await DoMultiRequestDiscardAsync(
            EFunctionCode.WriteMultipleCoils, 
            startingAddress, 
            values, 
            cancellationToken
        );
    }
    
    public async Task WriteMultipleRegistersAsync(
        int startingAddress,
        ReadOnlyMemory<ushort> values,
        CancellationToken cancellationToken = default)
    {
        await DoMultiRequestDiscardAsync(
            EFunctionCode.WriteMultipleRegisters, 
            startingAddress, 
            values, 
            cancellationToken
        );
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
    
    public async Task ReadInputRegistersAsync(
        Memory<ushort> destination,
        int startingAddress,
        int amount,
        CancellationToken cancellationToken = default
    ) => await ReadWordsAsync(destination, EFunctionCode.ReadInputRegisters, startingAddress, amount, cancellationToken);

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

    private async Task DoRequestDiscardAsync(
        EFunctionCode functionCode,
        int startingAddress,
        int amountOrValue,
        CancellationToken cancellationToken = default)
    {
        // TODO: This should handle exceptions
        using var owner = MemoryOwner<byte>.Allocate(PacketBufferSize);
        _ = await DoRequestAsync(owner, functionCode, startingAddress, amountOrValue, cancellationToken);
    }
    
    private async Task DoMultiRequestDiscardAsync(
        EFunctionCode functionCode,
        int startingAddress,
        ReadOnlyMemory<bool> bits,
        CancellationToken cancellationToken = default)
    {
        // TODO: This should handle exceptions
        using var owner = MemoryOwner<byte>.Allocate(PacketBufferSize);
        _ = await DoMultiRequestAsync(owner, functionCode, startingAddress, bits, cancellationToken);
    }
    
    private async Task DoMultiRequestDiscardAsync(
        EFunctionCode functionCode,
        int startingAddress,
        ReadOnlyMemory<ushort> words,
        CancellationToken cancellationToken = default)
    {
        // TODO: This should handle exceptions
        using var owner = MemoryOwner<byte>.Allocate(PacketBufferSize);
        _ = await DoMultiRequestAsync(owner, functionCode, startingAddress, words, cancellationToken);
    }

    private async Task<(int readOffset, int length)> DoRequestAsync(
        MemoryOwner<byte> destination,
        EFunctionCode functionCode,
        int startingAddress,
        int amountOrValue, 
        CancellationToken cancellationToken = default)
    {
        var memBuffer = destination.Memory;
        var written = Requests.Serialize(
            destination.Span,
            functionCode,
            (ushort)startingAddress,
            (ushort)amountOrValue
        );

        return await DoRequestRawDataAsync(destination, memBuffer[..written], cancellationToken);
    }
    
    private async Task<(int readOffset, int length)> DoMultiRequestAsync(
        MemoryOwner<byte> destination,
        EFunctionCode functionCode,
        int startingAddress,
        ReadOnlyMemory<bool> bits, 
        CancellationToken cancellationToken = default)
    {
        var memBuffer = destination.Memory;
        var written = Requests.Serialize(
            destination.Span,
            functionCode,
            (ushort)startingAddress,
            bits.Span
        );

        return await DoRequestRawDataAsync(destination, memBuffer[..written], cancellationToken);
    }
    
    private async Task<(int readOffset, int length)> DoMultiRequestAsync(
        MemoryOwner<byte> destination,
        EFunctionCode functionCode,
        int startingAddress,
        ReadOnlyMemory<ushort> words, 
        CancellationToken cancellationToken = default)
    {
        var memBuffer = destination.Memory;
        var written = Requests.Serialize(
            destination.Span,
            functionCode,
            (ushort)startingAddress,
            words.Span
        );

        return await DoRequestRawDataAsync(destination, memBuffer[..written], cancellationToken);
    }
    
    private async Task<(int readOffset, int length)> DoRequestRawDataAsync(
        MemoryOwner<byte> destination,
        ReadOnlyMemory<byte> requestData,
        CancellationToken cancellationToken = default)
    {
        var memBuffer = destination.Memory;
        requestData.CopyTo(memBuffer[ModbusTcpHeader.ModbusTcpHeaderLength..]);
        
        var (written, sentTransactionId) = WriteHeader(
            destination.Span,
            requestData.Length
        );

        written += requestData.Length;

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
        
        Requests.ThrowIfException(destination.Span[readOffset..]);

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