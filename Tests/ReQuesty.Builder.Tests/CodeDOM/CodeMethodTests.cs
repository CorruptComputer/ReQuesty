using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Extensions;

using Xunit;

namespace ReQuesty.Builder.Tests.CodeDOM;
public class CodeMethodTests
{
    [Fact]
    public void Defensive()
    {
        CodeMethod method = new()
        {
            Name = "class",
            ReturnType = new CodeType
            {
                Name = "string"
            }
        };
        Assert.False(method.IsOfKind(null!));
        Assert.False(method.IsOfKind([]));
        Assert.Throws<ArgumentNullException>(() => method.AddErrorMapping(null!, new CodeType { Name = "class" }));
        Assert.Throws<ArgumentNullException>(() => method.AddErrorMapping("oin", null!));
        Assert.Throws<ArgumentNullException>(() => method.ReturnType = null!);
    }
    [Fact]
    public void IsOfKind()
    {
        CodeMethod method = new()
        {
            Name = "class",
            ReturnType = new CodeType
            {
                Name = "string"
            }
        };
        Assert.False(method.IsOfKind(CodeMethodKind.Constructor));
        method.Kind = CodeMethodKind.Deserializer;
        Assert.True(method.IsOfKind(CodeMethodKind.Deserializer));
        Assert.True(method.IsOfKind(CodeMethodKind.Deserializer, CodeMethodKind.Getter));
        Assert.False(method.IsOfKind(CodeMethodKind.Getter));
    }
    [Fact]
    public void AddsParameter()
    {
        CodeMethod method = new()
        {
            Name = "method1",
            ReturnType = new CodeType
            {
                Name = "string"
            }
        };
        Assert.Throws<ArgumentNullException>(() =>
        {
            method.AddParameter((CodeParameter)null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            method.AddParameter(null!);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            method.AddParameter([]);
        });
    }
    [Fact]
    public void ClonesParameters()
    {
        CodeMethod method = new()
        {
            Name = "method1",
            ReturnType = new CodeType
            {
                Name = "string"
            }
        };
        method.AddParameter(new CodeParameter
        {
            Name = "param1",
            Type = new CodeType
            {
                Name = "string"
            }
        });
        CodeMethod? clone = method.Clone() as CodeMethod;
        Assert.Equal(method.Name, clone!.Name);
        Assert.Single(method.Parameters);
        Assert.Equal(method.Parameters.First().Name, clone.Parameters.First().Name);
    }
    [Fact]
    public void ParametersExtensionsReturnsValue()
    {
        CodeMethod method = new()
        {
            Name = "method1",
            ReturnType = new CodeType
            {
                Name = "string"
            }
        };
        method.AddParameter(new CodeParameter
        {
            Name = "param1",
            Kind = CodeParameterKind.Custom,
            Type = new CodeType
            {
                Name = "string"
            }
        });
        Assert.NotNull(method.Parameters.OfKind(CodeParameterKind.Custom));
        Assert.Null(method.Parameters.OfKind(CodeParameterKind.RequestBody));
    }
    [Fact]
    public void DeduplicatesErrorMappings()
    {
        CodeMethod method = new()
        {
            Name = "method1",
            ReturnType = new CodeType
            {
                Name = "string"
            }
        };
        CodeType commonType = new() { Name = "string" };
        method.AddErrorMapping("4XX", commonType);
        method.AddErrorMapping("5XX", commonType);
        method.DeduplicateErrorMappings();
        Assert.Single(method.ErrorMappings);
    }
    [Fact]
    public void DeduplicatesErrorMappingsCommonDefinition()
    {
        CodeMethod method = new()
        {
            Name = "method1",
            ReturnType = new CodeType
            {
                Name = "string"
            }
        };
        CodeClass codeClass = new()
        {
            Name = "class1"
        };
        CodeType commonType = new() { TypeDefinition = codeClass };
        CodeType commonType2 = new() { TypeDefinition = codeClass };
        method.AddErrorMapping("4XX", commonType);
        method.AddErrorMapping("5XX", commonType2);
        method.DeduplicateErrorMappings();
        Assert.Single(method.ErrorMappings);
    }
    [Fact]
    public void DoesNotDeduplicateErrorMappingsOnDifferentTypes()
    {
        CodeMethod method = new()
        {
            Name = "method1",
            ReturnType = new CodeType
            {
                Name = "string"
            }
        };
        method.AddErrorMapping("4XX", new CodeType { Name = "string" });
        method.AddErrorMapping("5XX", new CodeType { Name = "string" });
        method.DeduplicateErrorMappings();
        Assert.Equal(2, method.ErrorMappings.Count());
    }
    [Fact]
    public void DoesNotDeduplicatesErrorMappingsWithSpecificCodes()
    {
        CodeMethod method = new()
        {
            Name = "method1",
            ReturnType = new CodeType
            {
                Name = "string"
            }
        };
        CodeType commonType = new() { Name = "string" };
        method.AddErrorMapping("404", commonType);
        method.AddErrorMapping("5XX", commonType);
        method.DeduplicateErrorMappings();
        Assert.Equal(2, method.ErrorMappings.Count());
    }
}
