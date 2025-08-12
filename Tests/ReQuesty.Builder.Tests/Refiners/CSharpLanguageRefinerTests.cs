using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.Extensions;
using ReQuesty.Builder.Refiners;

using Xunit;

namespace ReQuesty.Builder.Tests.Refiners;
public class CSharpLanguageRefinerTests
{
    private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();
    #region CommonLanguageRefinerTests
    [Fact]
    public async Task EnumHasEscapedOption_UsesEnumMemberAttributeAsync()
    {
        CodeEnum model = root.AddEnum(new CodeEnum
        {
            Name = "someenum"
        }).First();
        CodeEnumOption option = new() { Name = "requestyCsharpName", SerializationName = "ReQuesty:CSharp:Enum" };
        model.AddOption(option);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        BlockDeclaration declaration = model.StartBlock;

        Assert.Contains("EnumMemberAttribute", declaration.Usings.Select(x => x.Name));
    }
    [Theory]
    [InlineData("operator")]
    [InlineData("string")]
    public async Task EnumWithReservedName_IsNotRenamedAsync(string input)
    {
        CodeEnum model = root.AddEnum(new CodeEnum
        {
            Name = "someenum"
        }).First();
        CodeEnumOption option = new() { Name = input, SerializationName = input };
        model.AddOption(option);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        Assert.Equal(input, model.Options.First().Name);
    }
    [Fact]
    public async Task EnumDoesntHaveEscapedOption_DoesntUseEnumMemberAttributeAsync()
    {
        CodeEnum model = root.AddEnum(new CodeEnum
        {
            Name = "someenum"
        }).First();
        CodeEnumOption option = new() { Name = "item1" };
        model.AddOption(option);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        BlockDeclaration declaration = model.StartBlock;

        Assert.DoesNotContain("EnumMemberAttribute", declaration.Usings.Select(x => x.Name));
    }
    [Fact]
    public async Task AddsExceptionInheritanceOnErrorClassesAsync()
    {
        CodeClass model = root.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        ClassDeclaration declaration = model.StartBlock;

        Assert.Contains("ApiException", declaration.Usings.Select(x => x.Name));
        Assert.Equal("ApiException", declaration.Inherits!.Name);
    }
    [Fact]
    public async Task InlineParentOnErrorClassesWhichAlreadyInheritAsync()
    {
        CodeClass model = root.AddClass(new CodeClass
        {
            Name = "somemodel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();

        CodeClass otherModel = root.AddClass(new CodeClass
        {
            Name = "otherModel",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = false,
        }).First();
        otherModel.AddProperty(
        new CodeProperty
        {
            Name = "otherProp",
            Type = new CodeType
            {
                Name = "string"
            }
        });
        otherModel.AddMethod(
        new CodeMethod
        {
            Name = "otherMethod",
            Kind = CodeMethodKind.RequestGenerator,
            IsAsync = false,
            ReturnType = new CodeType
            {
                Name = "string"
            }
        });
        otherModel.AddUsing(
        new CodeUsing
        {
            Name = "otherNs",
        });
        otherModel.StartBlock.AddImplements(new CodeType
        {
            Name = "IAdditionalDataHolder",
            IsExternal = true
        });
        ClassDeclaration declaration = model.StartBlock;
        declaration.Inherits = new CodeType
        {
            TypeDefinition = otherModel
        };
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        Assert.Contains(model.Properties, x => x.Name.Equals("OtherProp"));
        Assert.Contains(model.Methods, x => x.Name.Equals("otherMethod"));
        Assert.Contains(model.Usings, x => x.Name.Equals("otherNs"));
        Assert.Contains(model.StartBlock.Implements, x => x.Name.Equals("IAdditionalDataHolder"));
    }
    [Fact]
    public async Task AddsUsingsForErrorTypesForRequestExecutorAsync()
    {
        CodeClass requestBuilder = root.AddClass(new CodeClass
        {
            Name = "somerequestbuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();
        CodeNamespace subNS = root.AddNamespace($"{root.Name}.subns"); // otherwise the import gets trimmed
        CodeClass errorClass = subNS.AddClass(new CodeClass
        {
            Name = "Error4XX",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        CodeMethod requestExecutor = requestBuilder.AddMethod(new CodeMethod
        {
            Name = "get",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            },
        }).First();
        requestExecutor.AddErrorMapping("4XX", new CodeType
        {
            Name = "Error4XX",
            TypeDefinition = errorClass,
        });
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        ClassDeclaration declaration = requestBuilder.StartBlock;

        Assert.Contains("Error4XX", declaration.Usings.Select(x => x.Declaration?.Name));
    }
    [Fact]
    public async Task DoesNotEscapesReservedKeywordsForClassOrPropertyKindAsync()
    {
        // Arrange
        CodeClass model = root.AddClass(new CodeClass
        {
            Name = "break", // this a keyword
            Kind = CodeClassKind.Model,
        }).First();
        CodeProperty propertyWithCsharpReservedName = model.AddProperty(new CodeProperty
        {
            Name = "alias",// this a keyword
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        CodeProperty propertyWithReservedTypeName = model.AddProperty(new CodeProperty
        {
            Name = "task",// this a type name reserved in C#
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        // Act
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        // Assert
        Assert.Equal("break", model.Name);
        Assert.DoesNotContain("@", model.Name); // classname will be capitalized
        Assert.Equal("Alias", propertyWithCsharpReservedName.Name);
        Assert.DoesNotContain("@", propertyWithCsharpReservedName.Name); // classname will be capitalized
        Assert.Equal("Task", propertyWithReservedTypeName.Name);
        Assert.DoesNotContain("@", propertyWithReservedTypeName.Name); // classname will be capitalized
        Assert.DoesNotContain("Escaped", propertyWithReservedTypeName.Name); // classname will be capitalized
    }

    [Fact]
    public async Task DoesNotEscapesReservedKeywordsForClassOrPropertyKindEnhancedAsync()
    {
        // Arrange
        CodeClass reservedModel = root.AddClass(new CodeClass
        {
            Name = "file", // this a keyword
            Kind = CodeClassKind.Model,
        }).First();
        CodeClass reservedObjectModel = root.AddClass(new CodeClass
        {
            Name = "fileObject", // this a what the renaming of the keyword would cause
            Kind = CodeClassKind.Model,
        }).First();
        CodeProperty property = reservedModel.AddProperty(new CodeProperty
        {
            Name = "alias",// this a keyword
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        CodeProperty secondProperty = reservedModel.AddProperty(new CodeProperty
        {
            Name = "file",// this a keyword
            Type = new CodeType
            {
                TypeDefinition = reservedModel
            }
        }).First();
        CodeProperty thirdProperty = reservedModel.AddProperty(new CodeProperty
        {
            Name = "fileObject",// this a keyword
            Type = new CodeType
            {
                TypeDefinition = reservedObjectModel
            }
        }).First();
        // Act
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        // Assert
        Assert.Equal("fileObject1", reservedModel.Name);// classes/models will be renamed if reserved without conflicts
        Assert.Equal("fileObject", reservedObjectModel.Name);// original stays the same
        Assert.Equal("Alias", property.Name);// property names don't bring issue in dotnet
        Assert.Equal("File", secondProperty.Name);// property names don't bring issue in dotnet
        Assert.Equal("fileObject1", secondProperty.Type.Name);// property type was renamed
        Assert.Equal("FileObject", thirdProperty.Name);// property names don't bring issue in dotnet
        Assert.Equal("fileObject", thirdProperty.Type.Name);// property type was renamed

    }
    [Theory]
    [InlineData("integer")]
    [InlineData("boolean")]
    [InlineData("tuple")]
    [InlineData("single")]
    [InlineData("random")]
    [InlineData("buffer")]
    [InlineData("convert")]
    [InlineData("action")]
    [InlineData("valueType")]
    public async Task EscapesReservedTypeNamesAsync(string typeName)
    {
        // Arrange
        CodeClass model = root.AddClass(new CodeClass
        {
            Name = typeName,
            Kind = CodeClassKind.Model,
        }).First();
        CodeProperty property = model.AddProperty(new CodeProperty
        {
            Name = typeName,// this a keyword
            Type = new CodeType
            {
                Name = typeName,
                IsExternal = true
            }
        }).First();
        // Act
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        // Assert
        Assert.NotEqual(typeName, model.Name);
        Assert.Equal($"{typeName}Object", model.Name);//our defined model is renamed
        Assert.Equal(typeName, property.Type.Name);//external type is unchanged
        Assert.Equal(typeName.ToPascalCase(['_']), property.Name.ToFirstCharacterUpperCase());//external type property name is in pascal-case
    }

    [Fact]
    public async Task EscapesReservedKeywordsForReservedNamespaceNameSegmentsAsync()
    {
        CodeNamespace subNS = root.AddNamespace($"{root.Name}.task"); // otherwise the import gets trimmed
        CodeClass requestBuilder = subNS.AddClass(new CodeClass
        {
            Name = "tasksRequestBuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();

        CodeType indexerCodeType = new() { Name = "taskItemRequestBuilder" };
        CodeIndexer indexer = new()
        {
            Name = "idx",
            ReturnType = indexerCodeType,
            IndexParameter = new()
            {
                Name = "id",
                SerializationName = "id",
                Type = new CodeType
                {
                    Name = "string",
                },
            }
        };
        requestBuilder.AddIndexer(indexer);


        CodeNamespace itemSubNamespace = root.AddNamespace($"{subNS.Name}.item"); // otherwise the import gets trimmed
        CodeClass itemRequestBuilder = itemSubNamespace.AddClass(new CodeClass
        {
            Name = "taskItemRequestBuilder",
            Kind = CodeClassKind.RequestBuilder,
        }).First();

        itemRequestBuilder.AddMethod(new CodeMethod
        {
            Name = "get",
            Kind = CodeMethodKind.IndexerBackwardCompatibility,
            ReturnType = new CodeType
            {
                Name = "String"
            },
        });

        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        Assert.Contains("TaskNamespace", subNS.Name);
        Assert.Contains("TaskNamespace", itemSubNamespace.Name);
    }
    [Fact]
    public async Task ConvertsUnionTypesToWrapperAsync()
    {
        CodeClass model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        CodeUnionType union = new()
        {
            Name = "union",
        };
        union.AddType(new()
        {
            Name = "type1",
        }, new()
        {
            Name = "type2"
        });
        CodeProperty property = model.AddProperty(new CodeProperty
        {
            Name = "deserialize",
            Kind = CodePropertyKind.Custom,
            Type = (union.Clone() as CodeTypeBase)!,
        }).First();
        CodeMethod method = model.AddMethod(new CodeMethod
        {
            Name = "method",
            ReturnType = (union.Clone() as CodeTypeBase)!
        }).First();
        CodeParameter parameter = new()
        {
            Name = "param1",
            Type = (union.Clone() as CodeTypeBase)!
        };
        CodeIndexer indexer = new()
        {
            Name = "idx",
            ReturnType = (union.Clone() as CodeTypeBase)!,
            IndexParameter = new()
            {
                Name = "id",
                Type = new CodeType
                {
                    Name = "string"
                },
            }
        };
        model.AddIndexer(indexer);
        method.AddParameter(parameter);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root); //using CSharp so the indexer doesn't get removed
        Assert.True(property.Type is CodeType);
        Assert.True(parameter.Type is CodeType);
        Assert.True(method.ReturnType is CodeType);
        Assert.True(indexer.ReturnType is CodeType);
        CodeClass? resultingWrapper = root.FindChildByName<CodeClass>("union");
        Assert.NotNull(resultingWrapper);
        Assert.NotNull(resultingWrapper.OriginalComposedType);
        Assert.Contains("IComposedTypeWrapper", resultingWrapper.StartBlock.Implements.Select(static x => x.Name));
        Assert.Null(resultingWrapper.Methods.SingleOrDefault(static x => x.IsOfKind(CodeMethodKind.ComposedTypeMarker)));
    }
    [Fact]
    public async Task MovesClassesWithNamespaceNamesUnderNamespaceAsync()
    {
        CodeNamespace graphNS = root.AddNamespace("graph");
        CodeNamespace modelNS = graphNS.AddNamespace("graph.model");
        CodeClass model = graphNS.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.Single(root.GetChildElements(true));
        Assert.Single(graphNS.GetChildElements(true));
        Assert.Single(modelNS.GetChildElements(true));
        Assert.Equal(modelNS, model.Parent);
    }
    [Fact]
    public async Task KeepsCancellationParametersInRequestExecutorsAsync()
    {
        CodeClass model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.RequestBuilder
        }).First();
        CodeMethod method = model.AddMethod(new CodeMethod
        {
            Name = "getMethod",
            Kind = CodeMethodKind.RequestExecutor,
            ReturnType = new CodeType
            {
                Name = "string"
            }
        }).First();
        CodeParameter cancellationParam = new()
        {
            Name = "cancellationToken",
            Optional = true,
            Kind = CodeParameterKind.Cancellation,
            Documentation = new()
            {
                DescriptionTemplate = "Cancellation token to use when cancelling requests",
            },
            Type = new CodeType { Name = "CancellationToken", IsExternal = true },
        };
        method.AddParameter(cancellationParam);
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root); //using CSharp so the cancellationToken doesn't get removed
        Assert.True(method.Parameters.Any());
        Assert.Contains(cancellationParam, method.Parameters);
    }
    [Fact]
    public async Task ReplacesExceptionPropertiesNamesAsync()
    {
        CodeClass exception = root.AddClass(new CodeClass
        {
            Name = "error403",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        CodeProperty propToAdd = exception.AddProperty(new CodeProperty
        {
            Name = "stacktrace",
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.Equal("stacktraceEscaped", propToAdd.Name, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("stacktrace", propToAdd.SerializationName, StringComparer.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task RenamesMatchAndAddsPrimaryErrorMessageIfMatchAlreadyExistsAsync()
    {
        CodeClass exception = root.AddClass(new CodeClass
        {
            Name = "error403",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        CodeProperty propToAdd = exception.AddProperty(new CodeProperty
        {
            Name = "message",
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        Assert.False(exception.Properties.First().IsOfKind(CodePropertyKind.ErrorMessageOverride));// property is NOT message override
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        CodeProperty[] properties = exception.Properties.ToArray();
        Assert.Equal("messageEscaped", propToAdd.Name, StringComparer.OrdinalIgnoreCase);// property remains
        Assert.Equal(2, properties.Length); // no primary message property added
        Assert.Equal("message", properties[0].Name, StringComparer.OrdinalIgnoreCase); // name is expected.
        Assert.True(properties[0].IsOfKind(CodePropertyKind.ErrorMessageOverride));// property is now message override
        Assert.Equal("messageEscaped", properties[1].Name, StringComparer.OrdinalIgnoreCase); // renamed property is renamed as expected.
        Assert.True(properties[1].IsPrimaryErrorMessage);// property is IsPrimaryErrorMessage so that information deserialized into it shows up in the error information.
    }
    [Fact]
    public async Task RenamesExceptionClassWithReservedPropertyNameAsync()
    {
        CodeClass exception = root.AddClass(new CodeClass
        {
            Name = "message",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        CodeProperty propToAdd = exception.AddProperty(new CodeProperty
        {
            Name = "message",
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        CodeProperty[] properties = exception.Properties.ToArray();
        Assert.Equal("messageEscaped", exception.Name, StringComparer.OrdinalIgnoreCase);// class is renamed to avoid removing special overidden property
        Assert.Equal("messageEscapedProp", propToAdd.Name, StringComparer.OrdinalIgnoreCase); // property renamed to avoid conflicting with base
        Assert.Equal(2, properties.Length); // primary message is added
        Assert.Equal("message", properties[0].Name, StringComparer.OrdinalIgnoreCase); // we can still override exception message
        Assert.Equal("messageEscapedProp", properties[1].Name, StringComparer.OrdinalIgnoreCase); // collision with class name
    }
    [Fact]
    public async Task RenamesExceptionClassWithReservedPropertyNameWhenPropertyIsInitiallyAbsentAsync()
    {
        CodeClass exception = root.AddClass(new CodeClass
        {
            Name = "message",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = true,
        }).First();
        CodeProperty propToAdd = exception.AddProperty(new CodeProperty
        {
            Name = "something",
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.Equal("messageEscaped", exception.Name, StringComparer.OrdinalIgnoreCase);// class is renamed to avoid removing special overidden property
        Assert.Equal("something", propToAdd.Name, StringComparer.OrdinalIgnoreCase); // existing property remains
        Assert.Equal(2, exception.Properties.Count()); // initial property plus primary message
        Assert.Equal("message", exception.Properties.ToArray()[0].Name, StringComparer.OrdinalIgnoreCase); // primary error message is present
        Assert.Equal("something", exception.Properties.ToArray()[1].Name, StringComparer.OrdinalIgnoreCase);// existing property remains
    }
    [Fact]
    public async Task DoesNotReplaceNonExceptionPropertiesNamesAsync()
    {
        CodeClass exception = root.AddClass(new CodeClass
        {
            Name = "error403",
            Kind = CodeClassKind.Model,
            IsErrorDefinition = false,
        }).First();
        CodeProperty propToAdd = exception.AddProperty(new CodeProperty
        {
            Name = "message",
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.Equal("message", propToAdd.Name, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("message", propToAdd.SerializationName, StringComparer.OrdinalIgnoreCase);
    }
    #endregion
    #region CSharp
    [Fact]
    public async Task DisambiguatePropertiesWithClassNamesAsync()
    {
        CodeClass model = root.AddClass(new CodeClass
        {
            Name = "Model",
            Kind = CodeClassKind.Model
        }).First();
        CodeProperty propToAdd = model.AddProperty(new CodeProperty
        {
            Name = "model",
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.Equal("ModelProp", propToAdd.Name);
        Assert.Equal("model", propToAdd.SerializationName);
    }
    [Fact]
    public async Task AvoidsPropertyNameReplacementIfDuplicatedGeneratedAsync()
    {
        CodeClass model = root.AddClass(new CodeClass
        {
            Name = "Model",
            Kind = CodeClassKind.Model
        }).First();
        CodeProperty firstProperty = model.AddProperty(new CodeProperty
        {
            Name = "summary",
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        CodeProperty secondProperty = model.AddProperty(new CodeProperty
        {
            Name = "_summary",
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        CodeProperty thirdProperty = model.AddProperty(new CodeProperty
        {
            Name = "_replaced",
            Type = new CodeType
            {
                Name = "string"
            }
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.Equal("Summary", firstProperty.Name);// remains as is. No refinement needed
        Assert.Equal("_summary", secondProperty.Name);// No refinement as it will create a duplicate with firstProperty
        Assert.Equal("Replaced", thirdProperty.Name);// Base case. Proper refinements
    }
    [Fact]
    public async Task DisambiguatePropertiesWithClassNames_DoesntReplaceSerializationNameAsync()
    {
        string serializationName = "serializationName";
        CodeClass model = root.AddClass(new CodeClass
        {
            Name = "Model",
            Kind = CodeClassKind.Model
        }).First();
        CodeProperty propToAdd = model.AddProperty(new CodeProperty
        {
            Name = "model",
            Type = new CodeType
            {
                Name = "string"
            },
            SerializationName = serializationName,
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.Equal(serializationName, propToAdd.SerializationName);
    }
    [Fact]
    public async Task ReplacesDateOnlyByNativeTypeAsync()
    {
        CodeClass model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        CodeMethod method = model.AddMethod(new CodeMethod
        {
            Name = "method",
            ReturnType = new CodeType
            {
                Name = "DateOnly",
                IsExternal = true// this is external from the ReQuesty abstractions
            },
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Date", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesDateOnlyByNativeTypeInNestedClassAsync()
    {
        CodeClass model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        CodeClass nestedModel = model.AddInnerClass(new CodeClass
        {
            Name = "nestedModel",
            Kind = CodeClassKind.Model
        }).First();
        CodeProperty propertyInNestedModel = nestedModel.AddProperty(new CodeProperty
        {
            Name = "nestedModelProperty",
            Type = new CodeType
            {
                Name = "DateOnly",
                IsExternal = true// this is external from the ReQuesty abstractions
            },
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.NotEmpty(model.StartBlock.Usings); // using is added to outer class.
        Assert.Empty(nestedModel.StartBlock.Usings); // using is not added to nested model
        Assert.Equal("Date", propertyInNestedModel.Type.Name);
    }
    [Fact]
    public async Task ReplacesTimeOnlyByNativeTypeAsync()
    {
        CodeClass model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        CodeMethod method = model.AddMethod(new CodeMethod
        {
            Name = "method",
            ReturnType = new CodeType
            {
                Name = "TimeOnly",
                IsExternal = true // this is external from the ReQuesty abstractions
            },
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("Time", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesLocallyDefinedDateOnlyByNativeTypeAsync()
    {
        CodeClass model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        CodeClass dateOnlyModel = root.AddClass(new CodeClass
        {
            Name = "DateOnly",
            Kind = CodeClassKind.Model
        }).First();
        CodeMethod method = model.AddMethod(new CodeMethod
        {
            Name = "method",
            ReturnType = new CodeType
            {
                Name = "DateOnly",
                IsExternal = false,// this is internal from the description
                TypeDefinition = dateOnlyModel
            },
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("DateOnlyObject", method.ReturnType.Name);
    }
    [Fact]
    public async Task ReplacesLocallyDefinedTimeOnlyByNativeTypeAsync()
    {
        CodeClass model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        CodeClass timeOnlyModel = root.AddClass(new CodeClass
        {
            Name = "TimeOnly",
            Kind = CodeClassKind.Model
        }).First();
        CodeMethod method = model.AddMethod(new CodeMethod
        {
            Name = "method",
            ReturnType = new CodeType
            {
                Name = "TimeOnly",
                IsExternal = false, // this is internal from the description
                TypeDefinition = timeOnlyModel
            },
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.NotEmpty(model.StartBlock.Usings);
        Assert.Equal("TimeOnlyObject", method.ReturnType.Name);
    }

    [Fact]
    public async Task ReplacesIndexerDateOnlyTypeWithAbstractedDateTypeAsync()
    {
        // Arrange
        CodeClass requestBuilder = root.AddClass(new CodeClass
        {
            Name = "requestBuilder"
        }).First();

        requestBuilder.AddIndexer(new CodeIndexer
        {
            Name = "indexer",
            IndexParameter = new CodeParameter
            {
                Type = new CodeType
                {
                    Name = "DateOnly",
                    IsExternal = true
                }
            },
            ReturnType = new CodeType
            {
                Name = "SomeType"
            }
        });

        // Act
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        // Assert
        Assert.Equal("Date", requestBuilder.Indexer!.IndexParameter.Type.Name, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReplacesIndexerTimeOnlyTypeWithAbstractedTimeTypeAsync()
    {
        // Arrange
        CodeClass requestBuilder = root.AddClass(new CodeClass
        {
            Name = "requestBuilder"
        }).First();

        requestBuilder.AddIndexer(new CodeIndexer
        {
            Name = "indexer",
            IndexParameter = new CodeParameter
            {
                Type = new CodeType
                {
                    Name = "TimeOnly",
                    IsExternal = true
                }
            },
            ReturnType = new CodeType
            {
                Name = "SomeType"
            }
        });

        // Act
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        // Assert
        Assert.Equal("Time", requestBuilder.Indexer!.IndexParameter.Type.Name, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReplacesIndexerLocallyDefinedDateOnlyTypeWithAbstractedDateTypeAsync()
    {
        // Arrange
        CodeClass requestBuilder = root.AddClass(new CodeClass
        {
            Name = "requestBuilder"
        }).First();

        CodeClass dateOnlyModel = root.AddClass(new CodeClass
        {
            Name = "DateOnly",
            Kind = CodeClassKind.Model
        }).First();

        requestBuilder.AddIndexer(new CodeIndexer
        {
            Name = "indexer",
            IndexParameter = new CodeParameter
            {
                Type = new CodeType
                {
                    Name = "DateOnly",
                    IsExternal = false,
                    TypeDefinition = dateOnlyModel
                }
            },
            ReturnType = new CodeType
            {
                Name = "SomeType"
            }
        });

        // Act
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        // Assert
        Assert.Equal("DateOnlyObject", requestBuilder.Indexer!.IndexParameter.Type.Name, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReplacesIndexerLocallyDefinedTimeOnlyTypeWithAbstractedTimeTypeAsync()
    {
        // Arrange
        CodeClass requestBuilder = root.AddClass(new CodeClass
        {
            Name = "requestBuilder"
        }).First();

        CodeClass timeOnlyModel = root.AddClass(new CodeClass
        {
            Name = "TimeOnly",
            Kind = CodeClassKind.Model
        }).First();

        requestBuilder.AddIndexer(new CodeIndexer
        {
            Name = "indexer",
            IndexParameter = new CodeParameter
            {
                Type = new CodeType
                {
                    Name = "TimeOnly",
                    IsExternal = false,
                    TypeDefinition = timeOnlyModel
                }
            },
            ReturnType = new CodeType
            {
                Name = "SomeType"
            }
        });

        // Act
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);

        // Assert
        Assert.Equal("TimeOnlyObject", requestBuilder.Indexer!.IndexParameter.Type.Name, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddsUsingForUntypedNodeAsync()
    {
        CodeClass model = root.AddClass(new CodeClass
        {
            Name = "model",
            Kind = CodeClassKind.Model
        }).First();
        CodeProperty property = model.AddProperty(new CodeProperty
        {
            Name = "property",
            Type = new CodeType
            {
                Name = ReQuestyBuilder.UntypedNodeName,
                IsExternal = true
            },
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
        Assert.Equal(ReQuestyBuilder.UntypedNodeName, property.Type.Name);
        Assert.NotEmpty(model.StartBlock.Usings);
        CodeUsing[] nodeUsing = model.StartBlock.Usings.Where(static declaredUsing => declaredUsing.Name.Equals(ReQuestyBuilder.UntypedNodeName, StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Single(nodeUsing);
        Assert.Equal("ReQuesty.Runtime.Abstractions.Serialization", nodeUsing[0].Declaration!.Name);
    }

    [Theory]
    [InlineData(AccessModifier.Public)]
    [InlineData(AccessModifier.Internal)]
    public async Task SetTypeAccessModifierAsync(AccessModifier accessModifier)
    {
        CodeClass codeClass = root.AddClass(new CodeClass
        {
            Name = "Class1",
            Kind = CodeClassKind.Model
        }).First();
        CodeEnum codeEnum = root.AddEnum(new CodeEnum
        {
            Name = "Enum1",
        }).First();
        await ILanguageRefiner.RefineAsync(new GenerationConfiguration { Language = GenerationLanguage.CSharp, TypeAccessModifier = accessModifier }, root);
        Assert.Equal(codeClass.Access, accessModifier);
        Assert.Equal(codeEnum.Access, accessModifier);
    }
    #endregion
}
