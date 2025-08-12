using System.Text;
using ReQuesty.Builder.Validation;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using Xunit;

namespace ReQuesty.Builder.Tests.Validation;

public class MultipleServerEntriesTests
{
    [Fact]
    public async Task AddsAWarningWhenMultipleServersPresent()
    {
        string documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
  - url: https://graph.microsoft.com/beta
paths:
  /enumeration:
    get:
      responses:
        '200':
          description: some description
          content:
            application/json:";
        OpenApiDiagnostic? diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Single(diagnostic!.Warnings);
    }
    [Fact]
    public async Task DoesntAddAWarningWhenSingleServerPresent()
    {
        string documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
servers:
  - url: https://graph.microsoft.com/v1.0
paths:
  /enumeration:
    get:
      responses:
        '200':
          description: some description
          content:
            application/json:";
        OpenApiDiagnostic? diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Empty(diagnostic!.Warnings);
    }
    private static async Task<OpenApiDiagnostic?> GetDiagnosticFromDocumentAsync(string document)
    {
        MultipleServerEntries rule = new();
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(document));
        OpenApiReaderSettings settings = new();
        settings.RuleSet.Add(typeof(OpenApiDocument), [rule]);
        settings.AddYamlReader();
        ReadResult result = await OpenApiDocument.LoadAsync(stream, "yaml", settings);
        return result.Diagnostic;
    }
}
