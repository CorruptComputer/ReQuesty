using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Writers;

using Xunit;

namespace ReQuesty.Builder.Tests.Writers;

public sealed class CodeTypeWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeType currentType;
    private const string TypeName = "SomeType";
    public CodeTypeWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.CSharp, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        currentType = new()
        {
            Name = TypeName
        };
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeClass parentClass = root.AddClass(new CodeClass
        {
            Name = "ParentClass"
        }).First();
        currentType.Parent = parentClass;
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public void WritesCodeType()
    {
        writer.Write(currentType);
        string result = tw.ToString();
        Assert.Contains(TypeName, result);
    }
}
