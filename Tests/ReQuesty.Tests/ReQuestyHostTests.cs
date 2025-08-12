using System.CommandLine;
using System.CommandLine.IO;
using Moq;
using Xunit;

namespace ReQuesty.Tests;
public sealed class ReQuestyHostTests : IDisposable
{
    private readonly IConsole _console;
    private readonly List<IDisposable> _disposables = [];
    public ReQuestyHostTests()
    {
        Mock<IConsole> consoleMock = new();
        Mock<IStandardStreamWriter> mockStandardStreamWriter = new();
        StringWriter mockWriter = new();
        _disposables.Add(mockWriter);
        mockStandardStreamWriter.Setup(w => w.Write(It.IsAny<string>())).Callback<string>(mockWriter.Write);
        consoleMock.Setup(c => c.Out).Returns(mockStandardStreamWriter.Object);
        consoleMock.Setup(c => c.Error).Returns(mockStandardStreamWriter.Object);
        consoleMock.Setup(c => c.IsInputRedirected).Returns(true);
        consoleMock.Setup(c => c.IsOutputRedirected).Returns(true);
        consoleMock.Setup(c => c.IsErrorRedirected).Returns(true);
        _console = consoleMock.Object;
    }
    [Fact]
    public async Task ThrowsOnInvalidOutputPathAsync()
    {
        Assert.Equal(1, await ReQuestyHost.GetRootCommand().InvokeAsync(["generate", "-o", "A:\\doesnotexist"], _console));
    }
    [Fact]
    public async Task ThrowsOnInvalidInputPathAsync()
    {
        Assert.Equal(1, await ReQuestyHost.GetRootCommand().InvokeAsync(["generate", "-d", "A:\\doesnotexist"], _console));
    }
    [Fact]
    public async Task ThrowsOnInvalidInputUrlAsync()
    {
        Assert.Equal(1, await ReQuestyHost.GetRootCommand().InvokeAsync(["generate", "-d", "https://nonexistentdomain56a535ba-bda6-405e-b5e2-ef5f11bf1003.net/doesnotexist"], _console));
    }
    [Fact]
    public async Task ThrowsOnInvalidLanguageAsync()
    {
        Assert.Equal(1, await ReQuestyHost.GetRootCommand().InvokeAsync(["generate", "-l", "Pascal"], _console));
    }
    [Fact]
    public async Task ThrowsOnInvalidLogLevelAsync()
    {
        Assert.Equal(1, await ReQuestyHost.GetRootCommand().InvokeAsync(["generate", "--ll", "Dangerous"], _console));
    }
    [Fact]
    public async Task ThrowsOnInvalidClassNameAsync()
    {
        Assert.Equal(1, await ReQuestyHost.GetRootCommand().InvokeAsync(["generate", "-c", ".Graph"], _console));
        Assert.Equal(1, await ReQuestyHost.GetRootCommand().InvokeAsync(["generate", "-c", "Graph-api"], _console));
        Assert.Equal(1, await ReQuestyHost.GetRootCommand().InvokeAsync(["generate", "-c", "1Graph"], _console));
        Assert.Equal(1, await ReQuestyHost.GetRootCommand().InvokeAsync(["generate", "-c", "Gr@ph"], _console));
    }
    [Fact]
    public async Task AcceptsDeserializersAsync()
    {
        Assert.Equal(1, await ReQuestyHost.GetRootCommand().InvokeAsync(["generate", "--ds", "ReQuesty.Tests.TestData.TestDeserializer"], _console));
    }
    [Fact]
    public async Task AcceptsSerializersAsync()
    {
        Assert.Equal(1, await ReQuestyHost.GetRootCommand().InvokeAsync(["generate", "-s", "ReQuesty.Tests.TestData.TestSerializer"], _console));
    }
    [Fact]
    public async Task ThrowsOnInvalidSearchTermAsync()
    {
        Assert.Equal(1, await ReQuestyHost.GetRootCommand().InvokeAsync(["search"], _console));
    }

    public void Dispose()
    {
        foreach (IDisposable disposable in _disposables)
        {
            disposable.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
