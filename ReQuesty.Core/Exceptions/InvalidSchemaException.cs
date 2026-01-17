namespace ReQuesty.Core.Exceptions;

/// <summary>
///   An exception that is thrown when the schema is invalid.
/// </summary>
public class InvalidSchemaException : InvalidOperationException
{
    /// <summary>
    ///   ctor, with a reason given
    /// </summary>
    public InvalidSchemaException(string message)
        : base(message) { }

    /// <summary>
    ///   ctor, with a reason and another exception given
    /// </summary>
    public InvalidSchemaException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    ///   ctor
    /// </summary>
    public InvalidSchemaException() { }
}
