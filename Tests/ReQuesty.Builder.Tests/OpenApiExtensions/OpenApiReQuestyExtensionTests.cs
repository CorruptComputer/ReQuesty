using System.Text;
using System.Text.Json.Nodes;
using ReQuesty.Builder.OpenApiExtensions;
using Microsoft.OpenApi;
using Xunit;

namespace ReQuesty.Builder.Tests.OpenApiExtensions;

public class OpenApiReQuestyExtensionTests
{
    [Fact]
    public void Serializes()
    {
        OpenApiReQuestyExtension value = new()
        {
            LanguagesInformation = new() {
                {
                    "CSharp",
                    new LanguageInformation {
                        Dependencies = [
                            new LanguageDependency {
                                Name = "Microsoft.Graph.Core",
                                Version = "1.0.0",
                            }
                        ],
                        DependencyInstallCommand = "dotnet add package",
                        ClientClassName = "GraphServiceClient",
                        ClientNamespaceName = "Microsoft.Graph",
                        StructuredMimeTypes = [
                            "application/json",
                            "application/xml",
                        ]
                    }
                }
            },
        };
        using StringWriter sWriter = new();
        OpenApiJsonWriter writer = new(sWriter, new OpenApiJsonWriterSettings { Terse = true });


        value.Write(writer, OpenApiSpecVersion.OpenApi3_0);
        string result = sWriter.ToString();
        Assert.Equal("{\"languagesInformation\":{\"CSharp\":{\"maturityLevel\":\"Preview\",\"supportExperience\":\"Microsoft\",\"dependencyInstallCommand\":\"dotnet add package\",\"dependencies\":[{\"name\":\"Microsoft.Graph.Core\",\"version\":\"1.0.0\"}],\"clientClassName\":\"GraphServiceClient\",\"clientNamespaceName\":\"Microsoft.Graph\",\"structuredMimeTypes\":[\"application/json\",\"application/xml\"]}}}", result);
    }
    [Fact]
    public void Parses()
    {
        string oaiValueRepresentation =
        """
        {
            "languagesInformation": {
                "CSharp": {
                    "dependencies": [
                        {
                            "name": "Microsoft.Graph.Core",
                            "version": "1.0.0"
                        }
                    ],
                    "dependencyInstallCommand": "dotnet add package",
                    "maturityLevel": "Preview",
                    "supportExperience": "Microsoft",
                    "clientClassName": "GraphServiceClient",
                    "clientNamespaceName": "Microsoft.Graph",
                    "structuredMimeTypes": [
                        "application/json",
                        "application/xml"
                    ]
                }
            }
        }
        """;
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(oaiValueRepresentation));
        JsonNode? oaiValue = JsonNode.Parse(stream);
        OpenApiReQuestyExtension value = OpenApiReQuestyExtension.Parse(oaiValue!);
        Assert.NotNull(value);
        Assert.True(value.LanguagesInformation.TryGetValue("CSharp", out LanguageInformation? CSEntry));
        Assert.Equal("dotnet add package", CSEntry.DependencyInstallCommand);
        Assert.Equal("GraphServiceClient", CSEntry.ClientClassName);
        Assert.Equal("Microsoft.Graph", CSEntry.ClientNamespaceName);
        Assert.Single(CSEntry.Dependencies);
        Assert.Equal("Microsoft.Graph.Core", CSEntry.Dependencies[0].Name);
        Assert.Equal(2, CSEntry.StructuredMimeTypes.Count);
        Assert.Contains("application/json", CSEntry.StructuredMimeTypes);
        Assert.Contains("application/xml", CSEntry.StructuredMimeTypes);
        Assert.Equal("1.0.0", CSEntry.Dependencies[0].Version);
    }
}
