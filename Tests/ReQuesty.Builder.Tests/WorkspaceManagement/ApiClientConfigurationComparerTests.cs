using ReQuesty.Builder.Lock;
using ReQuesty.Builder.WorkspaceManagement;
using Xunit;

namespace ReQuesty.Builder.Tests.WorkspaceManagement;
public sealed class ApiClientConfigurationComparerTests
{
    private readonly ApiClientConfigurationComparer _comparer = new();
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
        StringIEnumerableDeepComparer iEnumComparer = new();
        HashCode hash = new();
        hash.Add(new HashSet<string>(StringComparer.OrdinalIgnoreCase), iEnumComparer);
        StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
        hash.Add(string.Empty, stringComparer);
        hash.Add(string.Empty, stringComparer);
        hash.Add("public", stringComparer);
        hash.Add(false);
        hash.Add(true);
        hash.Add(false);
        hash.Add([], iEnumComparer);
        HashCode hash2 = new();
        hash2.Add(string.Empty, stringComparer);
        hash2.Add(string.Empty, stringComparer);
        hash2.Add(new HashSet<string>(StringComparer.OrdinalIgnoreCase), iEnumComparer);
        hash2.Add(new HashSet<string>(StringComparer.OrdinalIgnoreCase), iEnumComparer);
        hash.Add(hash2.ToHashCode());
        Assert.Equal(hash.ToHashCode(), _comparer.GetHashCode(new() { UsesBackingStore = true }));
    }
}
