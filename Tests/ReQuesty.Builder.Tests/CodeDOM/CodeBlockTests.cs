using ReQuesty.Builder.CodeDOM;

using Xunit;

namespace ReQuesty.Builder.Tests.CodeDOM;
public class CodeBlockTests
{
    [Fact]
    public void Defensive()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        NeverBlock child = new()
        {
            Parent = root,
        };
        child.AddRange();
        Assert.Empty(child.GetChildElements(true));
    }
    class NeverBlock : CodeBlock<BlockDeclaration, BlockEnd>
    {
        public void AddRange()
        {
            AddRange((CodeClass[])null!);
        }

        public override string Name
        {
            get => base.Name;
            set => base.Name = value;
        }

        public override bool Equals(object? obj)
        {
            return base.Equals(obj);
        }

        public override IEnumerable<CodeElement> GetChildElements(bool innerOnly = false)
        {
            return base.GetChildElements(innerOnly);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string? ToString()
        {
            return base.ToString();
        }
    }
    [Fact]
    public void FindInChildElements()
    {
        string grandChildName = "child1.grandchild1";
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace("child1");
        child.AddNamespace(grandChildName);
        Assert.NotNull(root.FindChildByName<CodeNamespace>(grandChildName));
        Assert.Null(root.FindChildByName<CodeNamespace>("child2"));
    }
    [Fact]
    public void RemovesElements()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(CodeNamespaceTests.ChildName);
        IEnumerable<CodeClass> elements = child.AddClass(new CodeClass { Name = "class1" },
                        new CodeClass { Name = "class2" });
        child.RemoveChildElement(elements.First());
        Assert.Single(child.GetChildElements(true));

        child.RemoveChildElement<CodeClass>(null!); // doesn't fail when passing null collection
    }
    [Fact]
    public void AddsUsing()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(CodeNamespaceTests.ChildName);
        child.AddUsing(new CodeUsing
        {
            Name = "someNS"
        });
        Assert.Single(child.StartBlock.Usings);
    }
    [Fact]
    public void RemoveUsing()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(CodeNamespaceTests.ChildName);
        CodeUsing usng = new()
        {
            Name = "someNS"
        };
        child.AddUsing(usng);
        child.StartBlock.RemoveUsings(usng);
        Assert.Empty(child.StartBlock.Usings);
    }
    [Fact]
    public void RemoveUsingByDeclarationName()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(CodeNamespaceTests.ChildName);
        CodeUsing usng = new()
        {
            Name = "someNS",
            Declaration = new CodeType
            {
                Name = "someClass"
            }
        };
        child.AddUsing(usng);
        child.StartBlock.RemoveUsingsByDeclarationName("someClass");
        Assert.Empty(child.StartBlock.Usings);
    }
    [Fact]
    public void ThrowsWhenInsertingDuplicatedElements()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(CodeNamespaceTests.ChildName);
        Assert.Throws<InvalidOperationException>(() =>
        {
            child.AddClass(new CodeClass
            {
                Name = "class1"
            });
            child.AddEnum(new CodeEnum
            {
                Name = "class1"
            });
        });
    }
    [Fact]
    public void DoesntThrowWhenAddingOverloads()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(CodeNamespaceTests.ChildName);
        CodeClass codeClass = child.AddClass(new CodeClass
        {
            Name = "class1"
        }).First();
        CodeMethod method = new()
        {
            Name = "method",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            }
        };
        CodeMethod? overload = method.Clone() as CodeMethod;
        overload!.AddParameter(new CodeParameter
        {
            Name = "param1",
            Type = new CodeType
            {
                Name = "string"
            }
        });
        codeClass.AddMethod(method, overload);
    }
    [Fact]
    public void DoesntThrowWhenAddingIndexersWithPropName()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(CodeNamespaceTests.ChildName);
        CodeClass codeClass = child.AddClass(new CodeClass
        {
            Name = "class1"
        }).First();
        CodeProperty property = new()
        {
            Name = "property",
            Kind = CodePropertyKind.RequestBuilder,
            Type = new CodeType
            {
                Name = "string"
            }
        };
        CodeMethod indexer = new()
        {
            Name = "method",
            Kind = CodeMethodKind.IndexerBackwardCompatibility,
            ReturnType = new CodeType
            {
                Name = "string"
            }
        };
        codeClass.AddProperty(property);
        codeClass.AddMethod(indexer);
    }
    [Fact]
    public void FindChildByNameThrowsOnEmptyNames()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(CodeNamespaceTests.ChildName);
        Assert.Throws<ArgumentException>(() =>
        {
            child.FindChildByName<CodeClass>(string.Empty);
        });
        Assert.Throws<ArgumentException>(() =>
        {
            child.FindChildrenByName<CodeClass>(string.Empty);
        });
    }
    [Fact]
    public void FindsChildByNameInSubnamespace()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(CodeNamespaceTests.ChildName);
        string className = "class1";
        CodeClass class1 = child.AddClass(new CodeClass
        {
            Name = className
        }).First();
        Assert.Equal(class1, child.FindChildByName<CodeClass>(className));
        Assert.Null(child.FindChildByName<CodeClass>("class2"));
        Assert.Null(child.FindChildByName<CodeEnum>(className));
    }
    [Fact]
    public void FindsChildrenByName()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(CodeNamespaceTests.ChildName);
        string className = "class1";
        child.AddClass(new CodeClass
        {
            Name = className
        });
        CodeNamespace subchild = child.AddNamespace($"{child.Name}.four");
        subchild.AddClass(new CodeClass
        {
            Name = className
        });
        Assert.Equal(2, root.FindChildrenByName<CodeClass>(className).Count());
    }
    [Fact]
    public void ReplacesImplementsByName()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(CodeNamespaceTests.ChildName);
        string className = "class1";
        CodeClass model = child.AddClass(new CodeClass
        {
            Name = className
        }).First();
        model.StartBlock.AddImplements(new CodeType
        {
            Name = "IParsable",
            IsExternal = true
        });
        model.StartBlock.ReplaceImplementByName("IParsable", "Parsable");
        Assert.DoesNotContain(model.StartBlock.Implements, x => x.Name == "IParsable");
        Assert.Single(model.StartBlock.Implements, x => x.Name == "Parsable");
    }
}
