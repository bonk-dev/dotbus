using System.Buffers.Binary;
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

    public static void DeserializeBits(Span<bool> destination, ReadOnlySpan<byte> source)
    {
        // source[0] is the function code
        var coilCount = source[1];
        BitUtils.ExpandBits(destination, source[2..], coilCount);
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
}