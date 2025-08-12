using System.Text;
using ReQuesty.Builder.Validation;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using Xunit;

namespace ReQuesty.Builder.Tests.Validation;

public class DivergentResponseSchemaTests
{
    [Fact]
    public async Task DoesntAddAWarningWhenBodyIsSingle()
    {
        string documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
paths:
  /enumeration:
    get:
      responses:
        '200':
          description: some description
          content:
            application/json:
              schema:
                type: string
                format: int32";
        OpenApiDiagnostic? diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Empty(diagnostic!.Warnings);
    }
    [Fact]
    public async Task AddsAWarningWhenBodyIsDivergent()
    {
        string documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
paths:
  /enumeration:
    get:
      responses:
        '200':
          description: some description
          content:
            application/json:
              schema:
                type: string
                format: int32
        '201':
          description: some description
          content:
            application/json:
              schema:
                type: string
                format: int64";
        OpenApiDiagnostic? diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Single(diagnostic!.Warnings);
    }
    [Fact]
    public async Task DoesntAddAWarningWhenUsing2XX()
    {
        string documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
paths:
  /enumeration:
    get:
      responses:
        '200':
          description: some description
          content:
            application/json:
              schema:
                type: string
                format: int32
        '2XX':
          description: some description
          content:
            application/json:
              schema:
                type: string
                format: int64";
        OpenApiDiagnostic? diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Empty(diagnostic!.Warnings);
    }
    private static async Task<OpenApiDiagnostic?> GetDiagnosticFromDocumentAsync(string document)
    {
        DivergentResponseSchema rule = new(new());
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(document));
        OpenApiReaderSettings settings = new();
        settings.RuleSet.Add(typeof(OpenApiOperation), [rule]);
        settings.AddYamlReader();
        ReadResult result = await OpenApiDocument.LoadAsync(stream, "yaml", settings);
        return result.Diagnostic;
    }
}
