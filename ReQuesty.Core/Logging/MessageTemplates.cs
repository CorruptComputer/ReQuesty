using Microsoft.Extensions.Logging;

namespace ReQuesty.Core.Logging;

/// <summary>
///   Predefined message templates for logging.
///   See: https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging
/// </summary>
public static class MessageTemplates
{
    #region Critical
    private static readonly Action<ILogger, Exception> failedToGenerateClient = LoggerMessage.Define(
        LogLevel.Critical,
        new EventId(LogEvents.ClientGenerationError.AsInt(), nameof(FailedToGenerateClient)),
        "Error generating the client");

    /// <summary>
    ///   Logs a critical error when the client generation fails.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="exception"></param>
    public static void FailedToGenerateClient(this ILogger logger, Exception exception)
        => failedToGenerateClient(logger, exception);

    private static readonly Action<ILogger, Exception> failedToLoadOpenApiDocument = LoggerMessage.Define(
        LogLevel.Critical,
        new EventId(LogEvents.OpenApiDocumentLoadError.AsInt(), nameof(FailedToLoadOpenApiDocument)),
        "Failed to load OpenAPI document");

    /// <summary>
    ///   Logs a critical error when the OpenAPI document fails to load.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="exception"></param>
    public static void FailedToLoadOpenApiDocument(this ILogger logger, Exception exception)
        => failedToLoadOpenApiDocument(logger, exception);
    #endregion

    #region Error
    private static readonly Action<ILogger, string, string, Exception?> openApiParsingError = LoggerMessage.Define<string, string>(
        LogLevel.Error,
        new EventId(LogEvents.OpenApiParsingError.AsInt(), nameof(OpenApiParsingError)),
        "Error while parsing OpenAPI document: {Pointer} - {Message}");

    /// <summary>
    ///   Logs an error when there is a problem parsing the OpenAPI document.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="pointer"></param>
    /// <param name="message"></param>
    public static void OpenApiParsingError(this ILogger logger, string? pointer, string message)
        => openApiParsingError(logger, pointer ?? "(null)", message, null);

    private static readonly Action<ILogger, string, Exception?> duplicateClientNameError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(LogEvents.DuplicateClientNameError.AsInt(), nameof(DuplicateClientNameError)),
        "The client {ClientName} is already present in the configuration");

    /// <summary>
    ///   Logs an error when a duplicate client name is detected in the configuration.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="name"></param>
    public static void DuplicateClientNameError(this ILogger logger, string name)
        => duplicateClientNameError(logger, name, null);

    private static readonly Action<ILogger, string, Exception?> clientFailedToMigrateDueToMissingOpenApiDoc = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(LogEvents.ClientFailedToMigrateDueToMissingOpenApiDoc.AsInt(), nameof(DuplicateClientNameError)),
        "The client {ClientName} could not be migrated because the OpenAPI document could not be loaded");

    /// <summary>
    ///   Logs an error when a client fails to migrate due to a missing OpenAPI document.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="name"></param>
    public static void ClientFailedToMigrateDueToMissingOpenApiDoc(this ILogger logger, string name)
        => clientFailedToMigrateDueToMissingOpenApiDoc(logger, name, null);
    #endregion

    #region Warning
    #endregion

    #region Information
    #endregion

    #region Debug
    #endregion

    #region Trace
    #endregion
}
