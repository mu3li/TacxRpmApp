namespace TacxRpmApp.Core.Protocol;

public enum CommandStatus
{
    Unknown = -1,
    Success = 0x00,
    Fail = 0x01,
    NotSupported = 0x02,
    Rejected = 0x03,
    Uninitialized = 0xFF
}
