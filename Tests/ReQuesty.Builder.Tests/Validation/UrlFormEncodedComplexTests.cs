using System.Text;
using ReQuesty.Builder.Validation;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using Xunit;

namespace ReQuesty.Builder.Tests.Validation;

public class UrlFormEncodedComplexTests
{
    [Fact]
    public async Task AddsAWarningWhenUrlEncodedNotObjectRequestBody()
    {
        string documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
paths:
  /enumeration:
    get:
      requestBody:
        content:
          application/x-www-form-urlencoded:
            schema:
              type: string
              format: int32
      responses:
        '200':
          description: some description
          content:
            application/json:
              schema:
                type: string
                format: int32";
        OpenApiDiagnostic? diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Single(diagnostic!.Warnings);
    }
    [Fact]
    public async Task AddsAWarningWhenUrlEncodedNotObjectResponse()
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
            application/x-www-form-urlencoded:
              schema:
                type: string
                format: int32";
        OpenApiDiagnostic? diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Single(diagnostic!.Warnings);
    }
    [Fact]
    public async Task AddsAWarningWhenUrlEncodedComplexPropertyOnRequestBody()
    {
        string documentTxt = @"openapi: 3.0.1
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
paths:
  /enumeration:
    get:
      requestBody:
        content:
          application/x-www-form-urlencoded:
            schema:
              type: object
              properties:
                complex:
                  type: object
                  properties:
                    prop:
                      type: string
      responses:
        '200':
          description: some description
          content:
            application/json:
              schema:
                type: string
                format: int32";
        OpenApiDiagnostic? diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Single(diagnostic!.Warnings);
    }
    [Fact]
    public async Task AddsAWarningWhenUrlEncodedComplexPropertyOnResponse()
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
            application/x-www-form-urlencoded:
              schema:
                  type: object
                  properties:
                    complex:
                      type: object
                      properties:
                        prop:
                          type: string";
        OpenApiDiagnostic? diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Single(diagnostic!.Warnings);
    }
    [Fact]
    public async Task DoesntAddAWarningWhenUrlEncoded()
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
            application/x-www-form-urlencoded:
              schema:
                type: object
                properties:
                  prop:
                    type: string";
        OpenApiDiagnostic? diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Empty(diagnostic!.Warnings);
    }
    [Fact]
    public async Task DoesntAddAWarningOnArrayProperty()
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
        content:
          application/x-www-form-urlencoded:
            schema:
              type: object
              properties:
                filters:
                  type: array
                  items:
                    type: integer
                    format: int32
      responses:
        '200':
          description: some description
          content:
            application/x-www-form-urlencoded:
              schema:
                type: object
                properties:
                  prop:
                    type: string";
        OpenApiDiagnostic? diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Empty(diagnostic!.Warnings);
    }
    [Fact]
    public async Task DoesntAddAWarningWhenNotUrlEncoded()
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
                type: enum
                format: string";
        OpenApiDiagnostic? diagnostic = await GetDiagnosticFromDocumentAsync(documentTxt);
        Assert.Empty(diagnostic!.Warnings);
    }
    private static async Task<OpenApiDiagnostic?> GetDiagnosticFromDocumentAsync(string document)
    {
        UrlFormEncodedComplex rule = new();
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(document));
        OpenApiReaderSettings settings = new();
        settings.RuleSet.Add(typeof(OpenApiOperation), [rule]);
        settings.AddYamlReader();
        ReadResult result = await OpenApiDocument.LoadAsync(stream, "yaml", settings);
        return result.Diagnostic;
    }
}
