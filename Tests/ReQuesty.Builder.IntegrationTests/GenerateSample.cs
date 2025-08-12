using ReQuesty.Builder.Configuration;
using Microsoft.Extensions.Logging;

using Xunit;

namespace ReQuesty.Builder.IntegrationTests;

public sealed class GenerateSample : IDisposable
{
    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
    private readonly HttpClient _httpClient = new();
    [InlineData(false)]
    [InlineData(true)]
    [Theory]
    public async Task GeneratesTodoAsync(bool backingStore)
    {
        ILogger<ReQuestyBuilder> logger = LoggerFactory.Create(builder =>
        {
        }).CreateLogger<ReQuestyBuilder>();

        string backingStoreSuffix = backingStore ? string.Empty : "BackingStore";
        GenerationConfiguration configuration = new()
        {
            Language = GenerationLanguage.CSharp,
            OpenAPIFilePath = GetAbsolutePath("ToDoApi.yaml"),
            OutputPath = $".\\Generated\\Todo\\{GenerationLanguage.CSharp}{backingStoreSuffix}",
            UsesBackingStore = backingStore,
        };
        await new ReQuestyBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());
    }
    [InlineData(false)]
    [InlineData(true)]
    [Theory]
    public async Task GeneratesModelWithDictionaryAsync(bool backingStore)
    {
        ILogger<ReQuestyBuilder> logger = LoggerFactory.Create(builder =>
        {
        }).CreateLogger<ReQuestyBuilder>();

        string backingStoreSuffix = backingStore ? "BackingStore" : string.Empty;
        GenerationConfiguration configuration = new()
        {
            Language = GenerationLanguage.CSharp,
            OpenAPIFilePath = GetAbsolutePath("ModelWithDictionary.yaml"),
            OutputPath = $".\\Generated\\ModelWithDictionary\\{GenerationLanguage.CSharp}{backingStoreSuffix}",
            UsesBackingStore = backingStore,
        };
        await new ReQuestyBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());
    }
    [InlineData(false)]
    [InlineData(true)]
    [Theory]
    public async Task GeneratesResponseWithMultipleReturnFormatsAsync(bool backingStore)
    {
        ILogger<ReQuestyBuilder> logger = LoggerFactory.Create(builder =>
        {
        }).CreateLogger<ReQuestyBuilder>();

        string backingStoreSuffix = backingStore ? "BackingStore" : string.Empty;
        GenerationConfiguration configuration = new()
        {
            Language = GenerationLanguage.CSharp,
            OpenAPIFilePath = GetAbsolutePath("ResponseWithMultipleReturnFormats.yaml"),
            OutputPath = $".\\Generated\\ResponseWithMultipleReturnFormats\\{GenerationLanguage.CSharp}{backingStoreSuffix}",
            UsesBackingStore = backingStore,
        };
        await new ReQuestyBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());
    }

    [Fact]
    public async Task GeneratesErrorsInliningParentsAsync()
    {
        ILogger<ReQuestyBuilder> logger = LoggerFactory.Create(builder =>
        {
        }).CreateLogger<ReQuestyBuilder>();

        GenerationConfiguration configuration = new()
        {
            Language = GenerationLanguage.CSharp,
            OpenAPIFilePath = GetAbsolutePath("InheritingErrors.yaml"),
            OutputPath = $".\\Generated\\ErrorInlineParents\\{GenerationLanguage.CSharp}",
        };
        await new ReQuestyBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());
    }

    [Fact]
    public async Task GeneratesCorrectEnumsAsync()
    {
        ILogger<ReQuestyBuilder> logger = LoggerFactory.Create(builder =>
        {
        }).CreateLogger<ReQuestyBuilder>();

        GenerationConfiguration configuration = new()
        {
            Language = GenerationLanguage.CSharp,
            OpenAPIFilePath = GetAbsolutePath("EnumHandling.yaml"),
            OutputPath = $".\\Generated\\EnumHandling\\{GenerationLanguage.CSharp}",
        };
        await new ReQuestyBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());
    }

    [Fact]
    public async Task GeneratesUritemplateHintsAsync()
    {
        ILogger<ReQuestyBuilder> logger = LoggerFactory.Create(builder =>
        {
        }).CreateLogger<ReQuestyBuilder>();

        string OutputPath = $".\\Generated\\GeneratesUritemplateHints\\{GenerationLanguage.CSharp}";
        GenerationConfiguration configuration = new()
        {
            Language = GenerationLanguage.CSharp,
            OpenAPIFilePath = GetAbsolutePath("GeneratesUritemplateHints.yaml"),
            OutputPath = OutputPath,
            CleanOutput = true,
        };
        await new ReQuestyBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());

        string fullText = "";
        foreach (string file in Directory.GetFiles(OutputPath, "*.*", SearchOption.AllDirectories))
        {
            fullText += File.ReadAllText(file);
        }

        Assert.Contains("[QueryParameter(\"startDateTime\")]", fullText);
    }
    private static string GetAbsolutePath(string relativePath) => Path.Combine(Directory.GetCurrentDirectory(), relativePath);
}
