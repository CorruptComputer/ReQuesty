using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.OrderComparers;

using Moq;

using Xunit;

namespace ReQuesty.Builder.Tests.OrderComparers;

public class CodeParameterOrderComparerTests
{
    [Fact]
    public void DefensiveProgramming()
    {
        BaseCodeParameterOrderComparer comparer = new();
        Assert.NotNull(comparer);
        CodeParameter mockParameter = new Mock<CodeParameter>().Object;
        Assert.Equal(0, comparer.Compare(null, null));
        Assert.Equal(-1, comparer.Compare(null, mockParameter));
        Assert.Equal(1, comparer.Compare(mockParameter, null));
    }

    [Fact]
    public void CancellationParameterIsAfterRequestConfigurationByDefault()
    {
        BaseCodeParameterOrderComparer comparer = new();
        Assert.NotNull(comparer);
        CodeParameter param1 = new()
        {
            Name = "param1",
            Kind = CodeParameterKind.RequestConfiguration,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        CodeParameter param2 = new()
        {
            Name = "param2",
            Kind = CodeParameterKind.Cancellation,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        List<CodeParameter> parameters = [param1, param2];
        Assert.Equal("param1", parameters.OrderBy(x => x, comparer).First().Name);
        Assert.Equal(110, comparer.Compare(param2, param1));
        Assert.Equal(-110, comparer.Compare(param1, param2));
        Assert.Equal(0, comparer.Compare(param2, param2));
    }
    [Fact]
    public void CancellationParameterIsAfterRequestConfigurationByDefaultWithNamesInReverseOrder()
    {
        BaseCodeParameterOrderComparer comparer = new();
        Assert.NotNull(comparer);
        CodeParameter param1 = new()
        {
            Name = "requestConfiguration",
            Kind = CodeParameterKind.RequestConfiguration,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        CodeParameter param2 = new()
        {
            Name = "cancellationToken",
            Kind = CodeParameterKind.Cancellation,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        List<CodeParameter> parameters = [param1, param2];
        Assert.Equal("requestConfiguration", parameters.OrderBy(x => x, comparer).First().Name);
        Assert.Equal(90, comparer.Compare(param2, param1));
        Assert.Equal(-90, comparer.Compare(param1, param2));
        Assert.Equal(0, comparer.Compare(param2, param2));
    }
    [Fact]
    public void CancellationParameterIsAfterRequestConfigurationByDefaultIfBothOptional()
    {
        BaseCodeParameterOrderComparer comparer = new();
        Assert.NotNull(comparer);
        CodeParameter param1 = new()
        {
            Name = "param1",
            Kind = CodeParameterKind.RequestConfiguration,
            Optional = true,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        CodeParameter param2 = new()
        {
            Name = "param2",
            Kind = CodeParameterKind.Cancellation,
            Optional = true,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        List<CodeParameter> parameters = [param1, param2];
        Assert.Equal("param1", parameters.OrderBy(x => x, comparer).First().Name);
        Assert.Equal(110, comparer.Compare(param2, param1));
        Assert.Equal(-110, comparer.Compare(param1, param2));
        Assert.Equal(0, comparer.Compare(param2, param2));
    }
}
