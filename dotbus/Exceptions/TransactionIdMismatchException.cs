namespace dotbus.Exceptions;

public class TransactionIdMismatchException : Exception
{
    public ushort ExpectedId { get; }
    public ushort ReceivedId { get; }
    public override string Message { get; }

    public TransactionIdMismatchException(ushort expectedId, ushort receivedId)
    {
        ExpectedId = expectedId;
        ReceivedId = receivedId;
        Message = $"The Modbus TCP client has received a response with an invalid transaction id " +
                  $"(expected: {expectedId}, received: {receivedId}).";
    }
}