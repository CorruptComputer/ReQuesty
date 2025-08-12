using ReQuesty.Builder.Writers.CSharp;

using Xunit;

namespace ReQuesty.Builder.Tests.Writers.CSharp;
public class CSharpWriterTests
{
    [Fact]
    public void Instantiates()
    {
        CSharpWriter writer = new("./", "graph");
        Assert.NotNull(writer);
        Assert.NotNull(writer.PathSegmenter);
        Assert.Throws<ArgumentNullException>(() => new CSharpWriter(null!, "graph"));
        Assert.Throws<ArgumentNullException>(() => new CSharpWriter("./", null!));
    }
}
