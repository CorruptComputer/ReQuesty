using ReQuesty.Rpc;
using ReQuesty.Builder.Configuration;
using Xunit;

namespace ReQuesty.Builder.Tests.OpenApiExtensions;
public sealed class OpenApiDocumentDownloadServiceTests : IDisposable
{
    private readonly HttpClient _httpClient = new();
    private const string DocumentContentWithNoServer = @"openapi: 3.0.0
info:
  title: Graph Users
  version: 0.0.0
tags: []
paths:
  /users:
    get:
      operationId: getUsers
      parameters: []
      responses:
        '200':
          description: The request has succeeded.
          content:
            application/json:
              schema:
                type: object";


    public void Dispose()
    {
        _httpClient.Dispose();
    }

    [Fact]
    public async Task GetDocumentFromStreamAsyncTest_IncludeReQuestyValidationRulesInConfig()
    {
        GenerationConfiguration generationConfig = new()
        {
            PluginTypes = [PluginType.APIPlugin],
            IncludeReQuestyValidationRules = true
        };
        FakeLogger<OpenApiDocumentDownloadService> fakeLogger = new();

        using Stream inputDocumentStream = CreateMemoryStreamFromString(DocumentContentWithNoServer);
        OpenApiDocumentDownloadService documentDownloadService = new(_httpClient, fakeLogger);
        Microsoft.OpenApi.OpenApiDocument? document = await documentDownloadService.GetDocumentFromStreamAsync(inputDocumentStream, generationConfig);

        Assert.NotNull(document);
        //There should be a log entry for the no server rule
        IEnumerable<FakeLogEntry> logEntryForNoServerRule = fakeLogger.LogEntries
            .Where(l => l.message.StartsWith("OpenAPI warning: #/ - A servers entry (v3) or host + basePath + schemes properties (v2) was not present in the OpenAPI description"));
        Assert.Single(logEntryForNoServerRule);
    }

    [Fact]
    public async Task GetDocumentFromStreamAsyncTest_No_IncludeReQuestyValidationRulesInConfig()
    {
        GenerationConfiguration generationConfig = new()
        {
            PluginTypes = [PluginType.APIPlugin],
            IncludeReQuestyValidationRules = false
        };
        FakeLogger<OpenApiDocumentDownloadService> fakeLogger = new();

        using Stream inputDocumentStream = CreateMemoryStreamFromString(DocumentContentWithNoServer);
        OpenApiDocumentDownloadService documentDownloadService = new(_httpClient, fakeLogger);
        Microsoft.OpenApi.OpenApiDocument? document = await documentDownloadService.GetDocumentFromStreamAsync(inputDocumentStream, generationConfig);

        Assert.NotNull(document);
        //There should be no log entry for the no server rule
        IEnumerable<FakeLogEntry> logEntryForNoServerRule = fakeLogger.LogEntries
            .Where(l => l.message.StartsWith("OpenAPI warning: #/ - A servers entry (v3) or host + basePath + schemes properties (v2) was not present in the OpenAPI description"));
        Assert.Empty(logEntryForNoServerRule);
    }

    [Fact]
    public async Task GetDocumentFromStreamAsyncTest_Default_IncludeReQuestyValidationRulesInConfig()
    {
        GenerationConfiguration generationConfig = new()
        {
            PluginTypes = [PluginType.APIPlugin],
        };
        FakeLogger<OpenApiDocumentDownloadService> fakeLogger = new();

        using Stream inputDocumentStream = CreateMemoryStreamFromString(DocumentContentWithNoServer);
        OpenApiDocumentDownloadService documentDownloadService = new(_httpClient, fakeLogger);
        Microsoft.OpenApi.OpenApiDocument? document = await documentDownloadService.GetDocumentFromStreamAsync(inputDocumentStream, generationConfig);

        Assert.NotNull(document);
        //There should be no log entry for the no server rule
        IEnumerable<FakeLogEntry> logEntryForNoServerRule = fakeLogger.LogEntries
            .Where(l => l.message.StartsWith("OpenAPI warning: #/ - A servers entry (v3) or host + basePath + schemes properties (v2) was not present in the OpenAPI description"));
        Assert.Empty(logEntryForNoServerRule);
    }

    private static Stream CreateMemoryStreamFromString(string s)
    {
        MemoryStream stream = new();
        StreamWriter writer = new(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }
}
