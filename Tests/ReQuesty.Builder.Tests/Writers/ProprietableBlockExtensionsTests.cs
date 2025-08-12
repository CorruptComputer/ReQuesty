using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Extensions;
using ReQuesty.Builder.Writers;
using Xunit;

namespace ReQuesty.Builder.Tests.Writers;

public class ProprietableBlockExtensions
{
    [Fact]
    public void GetsTheCodePathForFirstLevelProperty()
    {
        // Given
        CodeClass block = new()
        {
            Name = "testClass",
        };
        block.AddProperty(
            new CodeProperty
            {
                Name = "prop1",
                Kind = CodePropertyKind.Custom,
                IsPrimaryErrorMessage = true,
                Type = new CodeType
                {
                    Name = "string",
                }
            },
            new CodeProperty
            {
                Name = "prop2",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = "string",
                }
            }
        );

        // When
        string result = block.GetPrimaryMessageCodePath(
            static x => x.Name.ToFirstCharacterUpperCase(),
            static x => x.Name.ToFirstCharacterUpperCase(),
            "?."
        );

        // Then
        Assert.Equal("Prop1", result);
    }
    [Fact]
    public void GetsNothingOnNoPrimaryMessage()
    {
        // Given
        CodeClass block = new()
        {
            Name = "testClass",
        };
        block.AddProperty(
            new CodeProperty
            {
                Name = "prop1",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = "string",
                }
            },
            new CodeProperty
            {
                Name = "prop2",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = "string",
                }
            }
        );

        // When
        string result = block.GetPrimaryMessageCodePath(
            static x => x.Name.ToFirstCharacterUpperCase(),
            static x => x.Name.ToFirstCharacterUpperCase(),
            "?."
        );

        // Then
        Assert.Empty(result);
    }
    [Fact]
    public void GetsTheCodePathForANestedProperty()
    {
        // Given
        CodeClass block = new()
        {
            Name = "testClass",
        };
        CodeClass nestedBlockLevel1 = new()
        {
            Name = "nestedClassLevel1",
        };
        CodeClass nestedBlockLevel2 = new()
        {
            Name = "nestedClassLevel2",
        };
        nestedBlockLevel2.AddProperty(
            new CodeProperty
            {
                Name = "prop1",
                Kind = CodePropertyKind.Custom,
                IsPrimaryErrorMessage = true,
                Type = new CodeType
                {
                    Name = "string",
                }
            },
            new CodeProperty
            {
                Name = "prop2",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = "string",
                }
            }
        );
        nestedBlockLevel1.AddProperty(
            new CodeProperty
            {
                Name = "prop1",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = nestedBlockLevel2.Name,
                    TypeDefinition = nestedBlockLevel2,
                }
            },
            new CodeProperty
            {
                Name = "prop2",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = "string",
                }
            }
        );
        block.AddProperty(
            new CodeProperty
            {
                Name = "prop1",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = nestedBlockLevel1.Name,
                    TypeDefinition = nestedBlockLevel1,
                }
            },
            new CodeProperty
            {
                Name = "prop2",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = "string",
                }
            }
        );

        // When
        string result = block.GetPrimaryMessageCodePath(
            static x => x.Name.ToFirstCharacterUpperCase(),
            static x => x.Name.ToFirstCharacterUpperCase(),
            "?."
        );

        // Then
        Assert.Equal("Prop1?.Prop1?.Prop1", result);
    }
    [Fact]
    public void GetsTheShortestCodePathForMultiplePrimaryMessages()
    {
        // Given
        CodeClass block = new()
        {
            Name = "testClass",
        };
        CodeClass nestedBlockLevel1 = new()
        {
            Name = "nestedClassLevel1",
        };
        CodeClass nestedBlockLevel2 = new()
        {
            Name = "nestedClassLevel2",
        };
        nestedBlockLevel2.AddProperty(
            new CodeProperty
            {
                Name = "prop1",
                Kind = CodePropertyKind.Custom,
                IsPrimaryErrorMessage = true,
                Type = new CodeType
                {
                    Name = "string",
                }
            },
            new CodeProperty
            {
                Name = "prop2",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = "string",
                }
            }
        );
        nestedBlockLevel1.AddProperty(
            new CodeProperty
            {
                Name = "prop1",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = nestedBlockLevel2.Name,
                    TypeDefinition = nestedBlockLevel2,
                }
            },
            new CodeProperty
            {
                Name = "prop2",
                Kind = CodePropertyKind.Custom,
                IsPrimaryErrorMessage = true,
                Type = new CodeType
                {
                    Name = "string",
                }
            }
        );
        block.AddProperty(
            new CodeProperty
            {
                Name = "prop1",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = nestedBlockLevel1.Name,
                    TypeDefinition = nestedBlockLevel1,
                }
            },
            new CodeProperty
            {
                Name = "prop2",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = "string",
                }
            }
        );

        // When
        string result = block.GetPrimaryMessageCodePath(
            static x => x.Name.ToFirstCharacterUpperCase(),
            static x => x.Name.ToFirstCharacterUpperCase(),
            "?."
        );

        // Then
        Assert.Equal("Prop1?.Prop2", result);
    }
}
