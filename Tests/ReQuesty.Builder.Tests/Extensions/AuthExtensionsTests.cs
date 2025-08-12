using ReQuesty.Builder.Extensions;
using Microsoft.DeclarativeAgents.Manifest;
using Xunit;

namespace ReQuesty.Builder.Tests.Extensions;
public class AuthExtensionsTests
{
    private class UnmanagedAuth : Auth
    {
        public string? ReferenceId
        {
            get; set;
        }
    }

    [Fact]
    public void GetReferenceId_FromNull()
    {
        Auth? auth = null;
        string? actual = auth.GetReferenceId();
        Assert.Null(actual);
    }

    [Fact]
    public void GetReferenceId_FromOAuthPluginVault_WithValidReferenceId()
    {
        OAuthPluginVault auth = new()
        {
            ReferenceId = "test_refid"
        };
        string? actual = auth.GetReferenceId();
        Assert.Equal("test_refid", actual);
    }

    [Fact]
    public void GetReferenceId_FromOAuthPluginVault_WithNullReferenceId()
    {
        OAuthPluginVault auth = new()
        {
            ReferenceId = null
        };
        string? actual = auth.GetReferenceId();
        Assert.Null(actual);
    }

    [Fact]
    public void GetReferenceId_FromApiKeyPluginVault_WithValidReferenceId()
    {
        OAuthPluginVault auth = new()
        {
            ReferenceId = "test_refid"
        };
        string? actual = auth.GetReferenceId();
        Assert.Equal("test_refid", actual);
    }

    [Fact]
    public void GetReferenceId_FromApiKeyPluginVault_WithNullReferenceId()
    {
        ApiKeyPluginVault auth = new()
        {
            ReferenceId = null
        };
        string? actual = auth.GetReferenceId();
        Assert.Null(actual);
    }

    [Fact]
    public void GetReferenceId_FromUnmanagedAuth_WithValidReferenceId()
    {
        UnmanagedAuth auth = new()
        {
            ReferenceId = "test_refid"
        };
        string? actual = auth.GetReferenceId();
        Assert.Null(actual);
    }

    [Fact]
    public void GetReferenceId_FromUnmanagedAuth_WithNullReferenceId()
    {
        UnmanagedAuth auth = new()
        {
            ReferenceId = null
        };
        string? actual = auth.GetReferenceId();
        Assert.Null(actual);
    }
}
