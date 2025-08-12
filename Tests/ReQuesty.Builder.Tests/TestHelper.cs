using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Configuration;

namespace ReQuesty.Builder.Tests;
public static class TestHelper
{
    public static CodeClass CreateModelClassInModelsNamespace(GenerationConfiguration config, CodeNamespace codeSpace, string className = "model", bool withInheritance = false)
    {
        CodeNamespace modelsNamespace = codeSpace.FindNamespaceByName(config.ModelsNamespaceName) ?? codeSpace.AddNamespace(config.ModelsNamespaceName);
        return CreateModelClass(modelsNamespace, className, withInheritance);
    }
    public static CodeClass CreateModelClass(CodeNamespace codeSpace, string className = "model", bool withInheritance = false)
    {
        CodeClass? superClass = withInheritance ? CreateSuperClass(codeSpace) : default;
        CodeClass testClass = new()
        {
            Name = className,
            Kind = CodeClassKind.Model
        };
        if (withInheritance)
        {
            testClass.StartBlock.Inherits = new CodeType
            {
                Name = superClass!.Name,
                TypeDefinition = superClass
            };
        }
        codeSpace.AddClass(testClass);

        CodeMethod deserializer = new()
        {
            Name = "DeserializerMethod",
            ReturnType = new CodeType { },
            Kind = CodeMethodKind.Deserializer,
            IsAsync = false,
        };

        CodeMethod serializer = new()
        {
            Name = "SerializerMethod",
            ReturnType = new CodeType { },
            Kind = CodeMethodKind.Serializer,
            IsAsync = false,
        };
        testClass.AddMethod(deserializer);
        testClass.AddMethod(serializer);
        return testClass;
    }

    private static CodeClass CreateSuperClass(CodeNamespace codeSpace)
    {
        CodeClass parentClass = CreateModelClass(codeSpace, "SuperClass");
        parentClass.AddProperty(new CodeProperty
        {
            Name = "definedInParent",
            Type = new CodeType
            {
                Name = "string"
            },
            Kind = CodePropertyKind.Custom,
        });
        return parentClass;
    }

    public static void AddSerializationPropertiesToModelClass(CodeClass modelClass)
    {
        // Additional Data
        modelClass.AddProperty(new CodeProperty
        {
            Name = "additionalData",
            Kind = CodePropertyKind.AdditionalData,
            Type = new CodeType
            {
                Name = "string"
            }
        });
        modelClass.AddProperty(new CodeProperty
        {
            Name = "dummyProp",
            Type = new CodeType
            {
                Name = "string"
            }
        });

        // string array or primitive array
        modelClass.AddProperty(new CodeProperty
        {
            Name = "dummyColl",
            Type = new CodeType
            {
                Name = "string",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
            }
        });

        // CodeClass property

        CodeNamespace? parentNamespace = modelClass.Parent as CodeNamespace;
        CodeClass propertyClass = CreateModelClass(parentNamespace!, "SomeComplexType");
        modelClass.AddProperty(new CodeProperty
        {
            Name = "dummyComplexColl",
            Type = new CodeType
            {
                Name = "Complex",
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                TypeDefinition = propertyClass
            }
        });

        // enum collection
        CodeEnum propertyEnum = new()
        {
            Name = "EnumType"
        };
        CodeEnumOption enumOption = new() { Name = "SomeOption" };
        propertyEnum.AddOption(enumOption);
        parentNamespace!.AddEnum(propertyEnum);
        modelClass.AddProperty(new CodeProperty
        {
            Name = "dummyEnumCollection",
            Type = new CodeType
            {
                Name = "SomeEnum",
                TypeDefinition = propertyEnum
            }
        });

        modelClass.AddProperty(new CodeProperty
        {
            Name = "definedInParent",
            Type = new CodeType
            {
                Name = "string"
            },
            Kind = CodePropertyKind.Custom,
        });
    }

    public static CodeMethod CreateMethod(CodeClass parentClass, string methodName, string returnTypeName)
    {
        CodeMethod method = new()
        {
            Name = methodName,
            ReturnType = new CodeType
            {
                Name = returnTypeName
            }
        };
        return parentClass.AddMethod(method).First();
    }
}
