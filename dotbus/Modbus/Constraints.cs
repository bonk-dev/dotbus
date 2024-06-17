namespace dotbus.Modbus;

public static class Constraints
{
    public const int MinimumAddress = ushort.MinValue;
    public const int MaximumAddress = ushort.MaxValue;
    
    public const int MinimumReadCoilQuantity = 0x0001;
    public const int MaximumReadCoilQuantity = 0x07D0;
    
    public const int MinimumReadRegisterQuantity = 0x0001;
    public const int MaximumReadRegisterQuantity = 0x007D;
    
    public const int MinimumWriteCoilQuantity = 0x0001;
    public const int MaximumWriteCoilQuantity = 0x07B0;
    
    public const int MinimumWriteRegisterQuantity = 0x0001;
    public const int MaximumWriteRegisterQuantity = 0x007B;
}