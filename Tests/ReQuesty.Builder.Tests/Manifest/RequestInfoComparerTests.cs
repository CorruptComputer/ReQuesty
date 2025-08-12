using ReQuesty.Builder.Manifest;
using Microsoft.OpenApi.ApiManifest;
using Xunit;

namespace ReQuesty.Builder.Tests.Manifest;

public sealed class RequestInfoComparerTests
{
    private readonly RequestInfoComparer _comparer = new();
    [Fact]
    public void Defensive()
    {
        Assert.Equal(new HashCode().ToHashCode(), _comparer.GetHashCode(null!));
        Assert.True(_comparer.Equals(null, null));
        Assert.False(_comparer.Equals(new(), null));
        Assert.False(_comparer.Equals(null, new()));
    }
    [Fact]
    public void GetsHashCode()
    {
        HashCode hc = new();
        hc.Add<string>(null!);
        hc.Add<string>(null!);
        Assert.Equal(hc.ToHashCode(), _comparer.GetHashCode(new()));
    }
    [Fact]
    public void Compares()
    {
        RequestInfo requestInfo = new()
        {
            Method = "get",
            UriTemplate = "https://graph.microsoft.com/v1.0/users"
        };
        RequestInfo requestInfo2 = new()
        {
            Method = "get",
            UriTemplate = "https://graph.microsoft.com/v1.0/me"
        };
        Assert.False(_comparer.Equals(requestInfo, requestInfo2));
    }
}
