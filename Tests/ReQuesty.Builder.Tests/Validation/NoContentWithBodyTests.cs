using System.Text;
using ReQuesty.Builder.Validation;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using Xunit;

namespace ReQuesty.Builder.Tests.Validation;

public class NoContentWithBodyTests
{
    [Fact]
    public async Task AddsAWarningWhen204WithBody()
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
        '204':
          description: some description
          content:
            application/json:";
        OpenApiDiagnostic? diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Single(diagnostic!.Warnings);
    }
    [Fact]
    public async Task DoesntAddAWarningWhen204WithNoBody()
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
        '204':
          description: some description";
        OpenApiDiagnostic? diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Empty(diagnostic!.Warnings);
    }
    [Fact]
    public async Task DoesntAddAWarningWhen200WithBody()
    {
        string documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
paths:
  /enumeration:
    post:
      requestBody:
        description: New navigation property
        content:
          application/json:
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
        NoContentWithBody rule = new();
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(document));
        OpenApiReaderSettings settings = new();
        settings.RuleSet.Add(typeof(OpenApiOperation), [rule]);
        settings.AddYamlReader();
        ReadResult result = await OpenApiDocument.LoadAsync(stream, "yaml", settings);
        return result.Diagnostic;
    }
}
