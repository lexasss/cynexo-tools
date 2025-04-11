namespace Cynexo.Communicator;

/// <summary>
/// A subset of Windows ERROR_XXX codes that are used to return from <see cref="CommPort"/> methods
/// </summary>
public enum Error
{
    /// <summary>
    /// ERROR_SUCCESS, no errors
    /// </summary>
    Success = 0,

    /// <summary>
    /// ERROR_NOT_READY, the port is not open yet/already
    /// </summary>
    NotReady = 0x15,

    /// <summary>
    /// ERROR_GEN_FAILURE, reading the port resulted in an exception
    /// </summary>
    AccessFailed = 0x1F,

    /// <summary>
    /// ERROR_OPEN_FAILED, not succeeded to open a port
    /// </summary>
    /// 
    OpenFailed = 0x6E,
}

/// <summary>
/// Operation reslt description used to return from <see cref="CommPort"/> public methods
/// </summary>
public class Result(string operation, Error error, string reason)
{
    public string Operation { get; init; } = operation;
    public Error Error { get; init; } = error;
    public string? Reason { get; init; } = reason;

    public static Result OK(string operation) => new(operation, Error.Success, "OK");

    public override string ToString() => $"{Operation} >> {Error} ({Reason})";
}
