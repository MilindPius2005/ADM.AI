namespace Adm.AI.Application.Exceptions;

public class PdfExtractionException : Exception
{
    public string ErrorCode { get; }

    public PdfExtractionException(string errorCode, string message)
        : base(message) => ErrorCode = errorCode;

    public PdfExtractionException(string errorCode, string message, Exception inner)
        : base(message, inner) => ErrorCode = errorCode;
}
