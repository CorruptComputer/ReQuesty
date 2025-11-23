using ReQuesty.Core.Exceptions;

using Xunit;

namespace ReQuesty.Builder.Tests.Exceptions;

public class InvalidSchemaExceptionTests
{
    [Fact]
    public void Instantiates()
    {
        Assert.NotNull(new InvalidSchemaException());
    }
}
