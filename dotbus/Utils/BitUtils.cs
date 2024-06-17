namespace dotbus.Utils;

public static class BitUtils
{
    /// <summary>
    /// Expands bits from bytes into individual booleans
    /// </summary>
    /// <param name="destination">The resulting boolean span</param>
    /// <param name="source">The received coil buffer from a Modbus slave</param>
    /// <param name="coilCount">Amount of coils (bits) to decode</param>
    /// <exception cref="ArgumentException">Thrown when the <see cref="destination"/> is smaller than <see cref="coilCount"/></exception>
    public static void ExpandBits(Span<bool> destination, ReadOnlySpan<byte> source, int coilCount)
    {
        if (destination.Length < coilCount)
        {
            throw new ArgumentException(
                $"Destination (length: {destination.Length}) was smaller than coilCount ({coilCount}",
                nameof(destination));
        }

        for (var i = 0; i < coilCount; ++i)
        {
            var bitIndex = 8 - i % 8;
            var byteIndex = i / 8;
            
            destination[i] = (source[byteIndex] & (0b1 << bitIndex)) != 0;
        }
    }
}