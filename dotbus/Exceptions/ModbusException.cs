using dotbus.Modbus;

namespace dotbus.Exceptions;

public class ModbusException : Exception
{
    public EExceptionCode ExceptionCode { get; }

    public override string Message => $"The Modbus slave device returned an exception: {Enum.GetName(ExceptionCode)}. " +
                                      $"Lookup the Modbus Application Protocol V1.1 for more details.";

    public ModbusException(EExceptionCode exceptionCode)
    {
        ExceptionCode = exceptionCode;
    }
}