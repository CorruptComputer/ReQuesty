using ReQuesty.Builder.Writers;
using ReQuesty.Builder.Writers.CSharp;

using Xunit;

namespace ReQuesty.Builder.Tests.Writers;

public class LanguageWriterTests
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    [Fact]
    public void GetCorrectWriterForLanguage()
    {
        Assert.Equal(typeof(CSharpWriter),
                    LanguageWriter.GetLanguageWriter(GenerationLanguage.CSharp, DefaultPath, DefaultName).GetType());
    }
}
