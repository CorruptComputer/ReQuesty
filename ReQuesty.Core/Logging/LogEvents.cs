namespace ReQuesty.Core.Logging;

/// <summary>
///   Defines log event identifiers for various error conditions.
/// </summary>
public enum LogEvents
{
    /// <summary>
    ///   Indicates an error occurred during client generation.
    /// </summary>
    ClientGenerationError = 1,

    /// <summary>
    ///   Indicates an error occurred while loading the OpenAPI document.
    /// </summary>
    OpenApiDocumentLoadError = 2,

    /// <summary>
    ///   Indicates an error occurred while parsing the OpenAPI document.
    /// </summary>
    OpenApiParsingError = 3,

    /// <summary>
    ///   Indicates an error occurred due to a duplicate client name.
    /// </summary>
    DuplicateClientNameError = 4,

    /// <summary>
    ///   Indicates that the client failed to migrate due to a missing OpenAPI document.
    /// </summary>
    ClientFailedToMigrateDueToMissingOpenApiDoc = 5,
}

/// <summary>
///   Provides extension methods for the LogEvents enum.
/// </summary>
public static class LogEventExtensions
{
    /// <summary>
    ///   Converts the LogEvents enum value to its integer representation.
    /// </summary>
    /// <param name="logEvent"></param>
    /// <returns></returns>
    public static int AsInt(this LogEvents logEvent)
    {
        return (int)logEvent;
    }
}