using ReQuesty.Builder.EqualityComparers;
using Microsoft.OpenApi;
using Xunit;

namespace ReQuesty.Builder.Tests.EqualityComparers;

public class OpenApiServerComparerTests
{
    [Fact]
    public void ProtocolAgnostic()
    {
        OpenApiServerComparer comparer = new();
        OpenApiServer s1 = new() { Url = "http://localhost" };
        OpenApiServer s2 = new() { Url = "https://localhost" };

        Assert.Equal(s1, s2, comparer);

        s1 = new OpenApiServer { Url = "hTtPs://localhost" };
        s2 = new OpenApiServer { Url = "http://localhost" };
        Assert.Equal(s1, s2, comparer);

        s1 = new OpenApiServer { Url = "hTtPs://" };
        s2 = new OpenApiServer { Url = "http://" };
        Assert.Equal(s1, s2, comparer);

        s1 = new OpenApiServer { Url = "hTtPs:/" };
        s2 = new OpenApiServer { Url = "http:/" };
        Assert.NotEqual(s1, s2, comparer);
    }
}
