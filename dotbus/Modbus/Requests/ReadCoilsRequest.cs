using System.Buffers.Binary;
using dotbus.Utils;

namespace dotbus.Modbus.Requests;

public static class ReadCoilsRequest
{
    public const int MinCoilAmount = 1;
    public const int MaxCoilAmount = 2000;
    public const int RequestLength = 5;
    
    public static int Serialize(
        Span<byte> destination, 
        ushort startAddress, 
        ushort amount)
    {
        destination[0] = (byte)EFunctionCode.ReadCoils;
        BinaryPrimitives.WriteUInt16BigEndian(destination[1..], startAddress);
        BinaryPrimitives.WriteUInt16BigEndian(destination[3..], amount);

        return RequestLength;
    }

    public static void Deserialize(Span<bool> destination, ReadOnlySpan<byte> source)
    {
        // source[0] is the function code
        var coilCount = source[1];
        BitUtils.ExpandBits(destination, source, coilCount);
    }
}