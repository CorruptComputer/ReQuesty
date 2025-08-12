using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Writers.CSharp;

using Xunit;

namespace ReQuesty.Builder.Tests.Writers.CSharp;

public sealed class TypeDefinitionExtensionsTests
{
    [Fact]
    public void ReturnsFullNameForTypeWithoutNamespace()
    {
        CodeNamespace rootNamespace = CodeNamespace.InitRootNamespace();
        CodeClass myClass = new()
        {
            Name = "myClass"
        };
        rootNamespace.AddClass(myClass);

        string fullName = TypeDefinitionExtensions.GetFullName(myClass);

        Assert.Equal("MyClass", fullName);
    }

    [Fact]
    public void ReturnsFullNameForTypeInNamespace()
    {
        CodeNamespace rootNamespace = CodeNamespace.InitRootNamespace();

        CodeNamespace myNamespace = rootNamespace.AddNamespace("MyNamespace");
        CodeClass myClass = new()
        {
            Name = "myClass",
        };
        myNamespace.AddClass(myClass);

        string fullName = TypeDefinitionExtensions.GetFullName(myClass);

        Assert.Equal("global::MyNamespace.MyClass", fullName);
    }

    [Fact]
    public void ReturnsFullNameForNestedTypes()
    {
        CodeNamespace rootNamespace = CodeNamespace.InitRootNamespace();

        CodeNamespace myNamespace = rootNamespace.AddNamespace("MyNamespace");

        CodeClass myParentClass = new()
        {
            Name = "myParentClass"
        };
        myNamespace.AddClass(myParentClass);

        CodeClass myNestedClass = new()
        {
            Name = "myNestedClass",
        };
        myParentClass.AddInnerClass(myNestedClass);

        string parentClassFullName = TypeDefinitionExtensions.GetFullName(myParentClass);
        string nestedClassFullName = TypeDefinitionExtensions.GetFullName(myNestedClass);

        Assert.Equal("global::MyNamespace.MyParentClass", parentClassFullName);
        Assert.Equal("global::MyNamespace.MyParentClass.MyNestedClass", nestedClassFullName);
    }

    [Fact]
    public void ThrowsIfTypeIsNull()
    {
        Assert.Throws<ArgumentNullException>("typeDefinition", () => TypeDefinitionExtensions.GetFullName(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ThrowsIfTypeDoesNotHaveAName(string? typeName)
    {
        CodeClass myClass = new()
        {
            Name = typeName!
        };

        Assert.Throws<ArgumentException>("typeDefinition", () => TypeDefinitionExtensions.GetFullName(myClass));
    }

    [Fact]
    public void ThrowsIfTypesParentIsInvalid()
    {
        CodeClass myClass = new()
        {
            Name = "myClass",
            Parent = new CodeConstant()
        };

        Assert.Throws<InvalidOperationException>(() => TypeDefinitionExtensions.GetFullName(myClass));
    }

    [Fact]
    public void CapitalizesTypeNamesInTypeHierarchyButNotTheNamespace()
    {
        CodeNamespace rootNamespace = CodeNamespace.InitRootNamespace();
        CodeNamespace myNamespace = rootNamespace.AddNamespace("myNamespace");

        CodeClass myParentClass = new()
        {
            Name = "myParentClass"
        };
        myNamespace.AddClass(myParentClass);

        CodeClass myNestedClass = new()
        {
            Name = "myNestedClass",
        };
        myParentClass.AddInnerClass(myNestedClass);

        string nestedClassFullName = TypeDefinitionExtensions.GetFullName(myNestedClass);

        Assert.Equal("global::myNamespace.MyParentClass.MyNestedClass", nestedClassFullName);
    }

    [Fact]
    public void DoesNotAppendNamespaceSegmentIfNamespaceNameIsEmpty()
    {
        CodeNamespace rootNamespace = CodeNamespace.InitRootNamespace();
        CodeNamespace myNamespace = rootNamespace.AddNamespace("ThisWillBeEmpty");
        myNamespace.Name = "";

        CodeClass myClass = new()
        {
            Name = "myClass"
        };
        myNamespace.AddClass(myClass);

        string fullName = TypeDefinitionExtensions.GetFullName(myClass);

        Assert.Equal("MyClass", fullName);
    }

    [Fact]
    public void PrependsGlobalNamespaceAliasToNamespaces()
    {
        CodeNamespace rootNamespace = CodeNamespace.InitRootNamespace();
        CodeNamespace myNamespace = rootNamespace.AddNamespace("MyNamespace");
        CodeClass myClass = new()
        {
            Name = "MyClass"
        };
        myNamespace.AddClass(myClass);

        string fullName = TypeDefinitionExtensions.GetFullName(myClass);

        Assert.StartsWith("global::", fullName, StringComparison.Ordinal);
    }
}
