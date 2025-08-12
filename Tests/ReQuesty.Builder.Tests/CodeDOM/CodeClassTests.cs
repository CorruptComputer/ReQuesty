using ReQuesty.Builder.CodeDOM;

using Xunit;

namespace ReQuesty.Builder.Tests.CodeDOM;
public class CodeClassTests
{
    [Fact]
    public void Defensive()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeClass codeClass = new()
        {
            Name = "class",
        };
        root.AddClass(codeClass);
        Assert.False(codeClass.IsOfKind(null!));
        Assert.False(codeClass.IsOfKind([]));
        Assert.Throws<ArgumentNullException>(() => codeClass.DiscriminatorInformation.AddDiscriminatorMapping(null!, new CodeType { Name = "class" }));
        Assert.Throws<ArgumentNullException>(() => codeClass.DiscriminatorInformation.AddDiscriminatorMapping("oin", null!));
        Assert.Throws<ArgumentNullException>(() => codeClass.DiscriminatorInformation.GetDiscriminatorMappingValue(null!));
        Assert.Null(codeClass.DiscriminatorInformation.GetDiscriminatorMappingValue("oin"));

        Assert.Null(codeClass.BaseClass);
    }
    [Fact]
    public void IsOfKind()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeClass codeClass = new()
        {
            Name = "class",
        };
        root.AddClass(codeClass);
        Assert.False(codeClass.IsOfKind(CodeClassKind.Model));
        codeClass.Kind = CodeClassKind.RequestBuilder;
        Assert.True(codeClass.IsOfKind(CodeClassKind.RequestBuilder));
        Assert.True(codeClass.IsOfKind(CodeClassKind.RequestBuilder, CodeClassKind.QueryParameters));
        Assert.False(codeClass.IsOfKind(CodeClassKind.QueryParameters));
    }
    [Fact]
    public void SetsIndexer()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(CodeNamespaceTests.ChildName);
        CodeClass codeClass = child.AddClass(new CodeClass
        {
            Name = "class1"
        }).First();
        codeClass.AddIndexer(new CodeIndexer
        {
            Name = "idx",
            ReturnType = new CodeType
            {
                Name = "string",
                IsExternal = true,
                IsNullable = true,
            },
            IndexParameter = new()
            {
                Name = "idxSmth",
                SerializationName = "idx_smth",
                Type = new CodeType
                {
                    Name = "string",
                    IsExternal = true,
                    IsNullable = true,
                },
            }
        });
        Assert.Single(codeClass.GetChildElements(true).OfType<CodeIndexer>());
        Assert.Throws<ArgumentNullException>(() =>
        {
            codeClass.AddIndexer(null!);
        });
        codeClass.AddIndexer(new CodeIndexer
        {
            Name = "idx2",
            ReturnType = new CodeType
            {
                Name = "string",
                IsExternal = true,
                IsNullable = true,
            },
            IndexParameter = new()
            {
                Name = "idx2",
                SerializationName = "idx-2",
                Type = new CodeType
                {
                    Name = "string",
                    IsExternal = true,
                    IsNullable = true,
                },
            }
        });
        Assert.Empty(codeClass.GetChildElements(true).OfType<CodeIndexer>());
        CodeMethod[] methods = codeClass.GetChildElements(true).OfType<CodeMethod>().Where(static x => x.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility)).ToArray();
        Assert.Equal(2, methods.Length);
        Assert.Equal("WithIdxSmth", methods.FirstOrDefault(static x => x.OriginalIndexer!.Name.Equals("idx"))?.Name);
        Assert.Equal("WithIdx2", methods.FirstOrDefault(static x => x.OriginalIndexer!.Name.Equals("idx2"))?.Name);
    }
    [Fact]
    public void ThrowsOnAddingEmptyCollections()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(CodeNamespaceTests.ChildName);
        CodeClass codeClass = child.AddClass(new CodeClass
        {
            Name = "class1"
        }).First();
        Assert.Throws<ArgumentNullException>(() =>
        {
            codeClass.AddMethod(null!);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            codeClass.AddMethod([]);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            codeClass.AddMethod([null!]);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            codeClass.AddProperty(null!);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            codeClass.AddProperty([]);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            codeClass.AddProperty([null!]);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            codeClass.AddInnerClass(null!);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            codeClass.AddInnerClass([]);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            codeClass.AddInnerClass([null!]);
        });
    }
    [Fact]
    public void AddsInnerElements()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(CodeNamespaceTests.ChildName);
        CodeClass codeClass = child.AddClass(new CodeClass
        {
            Name = "class1"
        }).First();
        codeClass.AddInnerClass(new CodeClass
        {
            Name = "subclass"
        });
        Assert.Single(codeClass.GetChildElements(true));
        codeClass.AddMethod(new CodeMethod
        {
            Name = "submethod",
            ReturnType = new CodeType
            {
                Name = "string"
            }
        });
        Assert.Equal(2, codeClass.GetChildElements(true).Count());
        codeClass.AddProperty(new CodeProperty
        {
            Name = "subprop",
            Type = new CodeType
            {
                Name = "string"
            }
        });
        Assert.Equal(3, codeClass.GetChildElements(true).Count());
    }
    [Fact]
    public void AddsInnerInterface()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(CodeNamespaceTests.ChildName);
        CodeClass codeClass = child.AddClass(new CodeClass
        {
            Name = "class1"
        }).First();
        codeClass.AddInnerInterface(new CodeInterface
        {
            Name = "subinterface",
            OriginalClass = new CodeClass() { Name = "originalSubInterface" }
        });
        Assert.Single(codeClass.GetChildElements(true).OfType<CodeInterface>());
        Assert.Throws<ArgumentNullException>(() =>
        {
            codeClass.AddInnerInterface(null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            codeClass.AddInnerInterface([null!]);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            codeClass.AddInnerInterface();
        });
    }
    [Fact]
    public void GetsParentAndGrandParent()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(CodeNamespaceTests.ChildName);
        CodeClass grandParent = child.AddClass(new CodeClass
        {
            Name = "class1"
        }).First();
        CodeClass parent = child.AddClass(new CodeClass
        {
            Name = "parent"
        }).First();
        CodeClass childClass = child.AddClass(new CodeClass
        {
            Name = "child"
        }).First();
        childClass.StartBlock.Inherits = new CodeType
        {
            TypeDefinition = parent,
        };
        parent.StartBlock.Inherits = new CodeType
        {
            TypeDefinition = grandParent,
        };
        Assert.Equal(grandParent, parent.BaseClass);
        Assert.Equal(parent, childClass.BaseClass);
        Assert.Equal(grandParent, childClass.GetGreatestGrandparent());
    }
    [Fact]
    public void ContainsMember()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(CodeNamespaceTests.ChildName);
        CodeClass codeClass = child.AddClass(new CodeClass
        {
            Name = "class1"
        }).First();
        codeClass.AddInnerClass(new CodeClass
        {
            Name = "subclass"
        });
        Assert.True(codeClass.ContainsMember("subclass"));
    }
    [Fact]
    public void InheritsFrom()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(CodeNamespaceTests.ChildName);
        CodeClass baseClass = child.AddClass(new CodeClass
        {
            Name = "baseClass"
        }).First();
        CodeClass baseClass2 = child.AddClass(new CodeClass
        {
            Name = "baseClass2"
        }).First();
        baseClass2.StartBlock.Inherits = new CodeType
        {
            TypeDefinition = baseClass,
        };
        CodeClass subClass = child.AddClass(new CodeClass
        {
            Name = "subclass"
        }).First();
        CodeClass unrelatedClass = child.AddClass(new CodeClass
        {
            Name = "unrelatedClass"
        }).First();
        subClass.StartBlock.Inherits = new CodeType
        {
            TypeDefinition = baseClass2,
        };
        Assert.True(baseClass2.StartBlock.InheritsFrom(baseClass));
        Assert.True(subClass.StartBlock.InheritsFrom(baseClass));
        Assert.True(subClass.StartBlock.InheritsFrom(baseClass2));
        Assert.False(subClass.StartBlock.InheritsFrom(unrelatedClass));
        Assert.False(baseClass.StartBlock.InheritsFrom(baseClass2));
    }
}
