using ReQuesty.Builder.CodeDOM;

using Xunit;

namespace ReQuesty.Builder.Tests.CodeDOM;
public class CodeNamespaceTests
{
    public const string ChildName = "one.two.three";
    [Fact]
    public void DoesntThrowOnRootInitialization()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        Assert.NotNull(root);
        Assert.Null(root.Parent);
        Assert.NotNull(root.StartBlock);
        Assert.NotNull(root.EndBlock);
    }
    [Fact]
    public void SegmentsNamespaceNames()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(ChildName);
        Assert.NotNull(child);
        Assert.Equal(ChildName, child.Name);
        CodeNamespace? two = child.Parent as CodeNamespace;
        Assert.NotNull(two);
        Assert.Equal("one.two", two.Name);
        CodeNamespace? one = two.Parent as CodeNamespace;
        Assert.NotNull(one);
        Assert.Equal("one", one.Name);
        Assert.Equal(root, one.Parent);
    }
    [Fact]
    public void AddsASingleItemNamespace()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(ChildName);
        CodeNamespace item = child.EnsureItemNamespace();
        Assert.NotNull(item);
        Assert.True(item.IsItemNamespace);
        Assert.Contains(".item", item.Name);
        Assert.Equal(child, item.Parent);
        CodeNamespace subitem = item.EnsureItemNamespace();
        Assert.Equal(item, subitem);
    }
    [Fact]
    public void ThrowsWhenAddingItemToRoot()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            CodeNamespace root = CodeNamespace.InitRootNamespace();
            CodeNamespace item = root.EnsureItemNamespace();
        });
    }
    [Fact]
    public void ThrowsWhenAddingANamespaceWithEmptyName()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        Assert.Throws<ArgumentNullException>(() =>
        {
            root.AddNamespace(null!);
        });
        Assert.Throws<ArgumentException>(() =>
        {
            root.AddNamespace(string.Empty);
        });
    }
    [Fact]
    public void FindsNamespaceByName()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(ChildName);
        CodeNamespace? result = root.FindNamespaceByName(ChildName);
        Assert.Equal(child, result);
    }
    [Fact]
    public void ThrowsOnAddingEmptyCollections()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(ChildName);
        Assert.Throws<ArgumentNullException>(() =>
        {
            child.AddClass(null!);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            child.AddClass([]);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            child.AddClass([null!]);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            child.AddEnum(null!);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            child.AddEnum([]);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            child.AddEnum([null!]);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            child.AddUsing(null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            child.AddUsing([null!]);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            child.AddFunction(null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            child.AddFunction([null!]);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            child.AddInterface(null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            child.AddInterface([null!]);
        });
    }
    [Fact]
    public void IsParentOf()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeNamespace child = root.AddNamespace(ChildName);
        CodeNamespace grandchild = child.AddNamespace(ChildName + ".four");
        Assert.True(child.IsParentOf(grandchild));
        Assert.False(grandchild.IsParentOf(child));
    }

}
