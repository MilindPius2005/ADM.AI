namespace Adm.AI.Application.Exceptions;

public class FileValidationException : Exception
{
    public string ErrorCode { get; }

    public FileValidationException(string errorCode, string message)
        : base(message) => ErrorCode = errorCode;
}
