namespace Adm.AI.Application.Exceptions;

public class AiProcessingException : Exception
{
    public string ErrorCode { get; }

    public AiProcessingException(string errorCode, string message)
        : base(message) => ErrorCode = errorCode;

    public AiProcessingException(string errorCode, string message, Exception inner)
        : base(message, inner) => ErrorCode = errorCode;
}
