using System.Buffers.Binary;

namespace dotbus.Transport;

public class ModbusTcpHeader
{
    private const ushort ProtocolId = 0x00;
    public const int ModbusTcpHeaderLength = 7;

    /// <summary>
    /// Writes the Modbus/TCP header
    /// </summary>
    /// <param name="destination">Destination buffer</param>
    /// <param name="transactionId">ID of the transaction</param>
    /// <param name="requestLength">Length of the Modbus payload</param>
    /// <param name="unitId">The slave Id</param>
    /// <returns>Amount of bytes written to destination</returns>
    public static int WriteModbusTcpHeader(
        Span<byte> destination,
        ushort transactionId,
        int requestLength,
        byte unitId) => WriteModbusTcpHeader(destination, transactionId, (ushort)requestLength, unitId);
    
    /// <summary>
    /// Writes the Modbus/TCP header
    /// </summary>
    /// <param name="destination">Destination buffer</param>
    /// <param name="transactionId">ID of the transaction</param>
    /// <param name="requestLength">Length of the Modbus payload</param>
    /// <param name="unitId">The slave Id</param>
    /// <returns>Amount of bytes written to destination</returns>
    public static int WriteModbusTcpHeader(
        Span<byte> destination, ushort transactionId, ushort requestLength, byte unitId)
    {
        BinaryPrimitives.WriteUInt16BigEndian(destination, transactionId);
        BinaryPrimitives.WriteUInt16BigEndian(destination[2..], ProtocolId);
        BinaryPrimitives.WriteUInt16BigEndian(destination[4..], (ushort)(requestLength + 1));
        destination[6] = unitId;

        return ModbusTcpHeaderLength;
    }
}