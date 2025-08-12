namespace ReQuesty.Builder.Logging;

internal sealed class AggregateScope(params IDisposable[] scopes) : IDisposable
{
    public void Dispose()
    {
        foreach (IDisposable scope in scopes)
        {
            scope.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
