namespace ReQuesty.Core.Logging;

public enum LogEvents
{
    ClientGenerationError = 1,
    OpenApiDocumentLoadError = 2,

    OpenApiParsingError = 3,
    DuplicateClientNameError = 4,
    ClientFailedToMigrateDueToMissingOpenApiDoc = 5,
}

public static class LogEventExtensions
{
    public static int AsInt(this LogEvents logEvent)
    {
        return (int)logEvent;
    }
}