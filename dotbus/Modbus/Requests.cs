using System.Buffers.Binary;
using dotbus.Exceptions;
using dotbus.Utils;

namespace dotbus.Modbus;

public static class Requests
{
    public const int MinCoilAmount = 1;
    public const int MaxCoilAmount = 2000;
    public const int RequestLength = 5;
    
    public static int Serialize(
        Span<byte> destination, 
        EFunctionCode functionCode,
        ushort startAddress, 
        ushort amountOrValue)
    {
        destination[0] = (byte)functionCode;
        BinaryPrimitives.WriteUInt16BigEndian(destination[1..], startAddress);
        BinaryPrimitives.WriteUInt16BigEndian(destination[3..], amountOrValue);

        return RequestLength;
    }

    public static int Serialize(
        Span<byte> destination,
        EFunctionCode functionCode,
        ushort startAddress,
        ReadOnlySpan<bool> bitValues)
    {
        destination[0] = (byte)functionCode;
        BinaryPrimitives.WriteUInt16BigEndian(destination[1..], startAddress);
        BinaryPrimitives.WriteUInt16BigEndian(destination[3..], (ushort)bitValues.Length);

        var byteCount = bitValues.Length % 8 == 0 ? bitValues.Length / 8 : bitValues.Length / 8 + 1;
        destination[5] = (byte)(byteCount);
        BitUtils.SmashBits(destination[6..], bitValues);

        return 6 + byteCount;
    }
    
    public static int Serialize(
        Span<byte> destination,
        EFunctionCode functionCode,
        ushort startAddress,
        ReadOnlySpan<ushort> words)
    {
        destination[0] = (byte)functionCode;
        BinaryPrimitives.WriteUInt16BigEndian(destination[1..], startAddress);
        BinaryPrimitives.WriteUInt16BigEndian(destination[3..], (ushort)words.Length);
        destination[5] = (byte)(words.Length * 2);

        for (var i = 0; i < words.Length; ++i)
        {
            BinaryPrimitives.WriteUInt16BigEndian(destination[(6 + i * 2)..], words[i]);
        }

        return 6 + words.Length * 2;
    }

    public static void DeserializeBits(Span<bool> destination, ReadOnlySpan<byte> source)
    {
        // source[0] is the function code
        var byteCount = source[1];
        var coilCount = destination.Length;
        BitUtils.ExpandBits(destination, source.Slice(2, byteCount), coilCount);
    }

    public static void DeserializeWords(Span<ushort> destination, ReadOnlySpan<byte> source)
    {
        // source[0] is the function code   
        var wordCount = source[1] / 2;

        // function code (1 byte) + wordCount (1 byte)
        const int dataOffset = 2;
        
        // I'd love to use MemoryMarshal.Cast<TF, TT> but Modbus is serializing in BigEndian :(
        for (var i = 0; i < wordCount; ++i)
        {
            destination[i] = BinaryPrimitives.ReadUInt16BigEndian(source[(dataOffset + i * 2)..]);
        }
    }
    
    public static void DeserializeBytes(Span<byte> destination, ReadOnlySpan<byte> source)
    {
        // source[0] is the function code   
        var wordCount = source[1] / 2;

        // function code (1 byte) + wordCount (1 byte)
        const int dataOffset = 2;
        
        source
            .Slice(dataOffset, wordCount * 2)
            .CopyTo(destination);
    }
    
    public static void ThrowIfException(ReadOnlySpan<byte> source)
    {
        if (IsException(source[0]))
        {
            throw new ModbusException((EExceptionCode)source[1]);
        }
    }

    /// <summary>
    /// Checks if the response is an exception message
    /// </summary>
    /// <param name="functionCodeByte">The returned function code</param>
    /// <returns>True if the MSB is set to high</returns>
    private static bool IsException(byte functionCodeByte) => 
        (functionCodeByte & 0b1000_0000) != 0;
}