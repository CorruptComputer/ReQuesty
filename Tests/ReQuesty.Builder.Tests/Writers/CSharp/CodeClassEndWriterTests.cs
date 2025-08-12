using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Writers;
using ReQuesty.Builder.Writers.CSharp;

using Xunit;

namespace ReQuesty.Builder.Tests.Writers.CSharp;
public sealed class CodeClassEndWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeBlockEndWriter codeElementWriter;
    private readonly CodeClass parentClass;
    public CodeClassEndWriterTests()
    {
        codeElementWriter = new CodeBlockEndWriter(new CSharpConventionService());
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.CSharp, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        parentClass = new CodeClass
        {
            Name = "parentClass"
        };
        root.AddClass(parentClass);
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public void ClosesNestedClasses()
    {
        CodeClass child = parentClass.AddInnerClass(new CodeClass
        {
            Name = "child"
        }).First();
        codeElementWriter.WriteCodeElement(child.EndBlock, writer);
        string result = tw.ToString();
        Assert.Equal(1, result.Count(x => x == '}'));
        Assert.DoesNotContain("#pragma warning restore CS0618", result);
    }
    [Fact]
    public void WritesWarningRestoreCs0618()
    {
        codeElementWriter.WriteCodeElement(parentClass.EndBlock, writer);
        string result = tw.ToString();
        Assert.Contains("#pragma warning restore CS0618", result);
    }
    [Fact]
    public void ClosesNonNestedClasses()
    {
        codeElementWriter.WriteCodeElement(parentClass.EndBlock, writer);
        string result = tw.ToString();
        Assert.Equal(2, result.Count(x => x == '}'));
    }
}
