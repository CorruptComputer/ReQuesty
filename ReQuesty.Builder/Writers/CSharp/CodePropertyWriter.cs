using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Extensions;

namespace ReQuesty.Builder.Writers.CSharp;
public class CodePropertyWriter(CSharpConventionService conventionService) : BaseElementWriter<CodeProperty, CSharpConventionService>(conventionService)
{
    public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.ExistsInExternalBaseType)
        {
            return;
        }

        string propertyType = conventions.GetTypeString(codeElement.Type, codeElement);

        bool isNullable = codeElement.Type.IsNullable;
        if (isNullable && !propertyType.EndsWith('?'))
        {
            propertyType += "?";
        }

        bool hasDescription = conventions.WriteShortDescription(codeElement, writer);
        conventions.WriteDeprecationAttribute(codeElement, writer);

        if (!hasDescription)
        {
            CSharpConventionService.WritePragmaDisable(writer, CSharpConventionService.CS1591);
        }

        WritePropertyInternal(codeElement, writer, propertyType);

        if (!hasDescription)
        {
            CSharpConventionService.WritePragmaRestore(writer, CSharpConventionService.CS1591);
        }
    }

    private void WritePropertyInternal(CodeProperty codeElement, LanguageWriter writer, string propertyType)
    {
        if (codeElement.Parent is not CodeClass parentClass)
        {
            throw new InvalidOperationException("The parent of a property should be a class");
        }

        CodeProperty? backingStoreProperty = parentClass.GetBackingStoreProperty();
        string setterAccessModifier = codeElement.ReadOnly && codeElement.Access > AccessModifier.Private ? "private " : string.Empty;
        string simpleBody = $"get; {setterAccessModifier}set;";
        string defaultValue = " = default!;";
        switch (codeElement.Kind)
        {
            case CodePropertyKind.RequestBuilder:
                writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {propertyType} {codeElement.Name.ToFirstCharacterUpperCase()}");
                writer.StartBlock();
                writer.Write("get => ");
                conventions.AddRequestBuilderBody(parentClass, propertyType, writer, includeIndent: false);
                writer.CloseBlock();
                break;
            case CodePropertyKind.AdditionalData when backingStoreProperty != null:
            case CodePropertyKind.Custom when backingStoreProperty != null:
                string backingStoreKey = codeElement.WireName;
                string nullableOp = !codeElement.IsOfKind(CodePropertyKind.AdditionalData) ? "?" : string.Empty;
                string defaultPropertyValue = codeElement.IsOfKind(CodePropertyKind.AdditionalData) ? " ?? new Dictionary<string, object>()" : string.Empty;
                writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {propertyType} {codeElement.Name.ToFirstCharacterUpperCase()}");
                writer.StartBlock();
                writer.WriteLine($"get {{ return {backingStoreProperty.Name.ToFirstCharacterUpperCase()}{nullableOp}.Get<{propertyType}>(\"{backingStoreKey}\"){defaultPropertyValue}; }}");
                writer.WriteLine($"set {{ {backingStoreProperty.Name.ToFirstCharacterUpperCase()}{nullableOp}.Set(\"{backingStoreKey}\", value); }}");
                writer.CloseBlock();
                break;
            case CodePropertyKind.ErrorMessageOverride when parentClass.IsErrorDefinition:
                if (parentClass.GetPrimaryMessageCodePath(static x => x.Name.ToFirstCharacterUpperCase(), static x => x.Name.ToFirstCharacterUpperCase(), "?.") is string primaryMessageCodePath && !string.IsNullOrEmpty(primaryMessageCodePath))
                {
                    writer.WriteLine($"public override {propertyType} {codeElement.Name.ToFirstCharacterUpperCase()} {{ get => {primaryMessageCodePath} ?? string.Empty; }}");
                }
                else
                {
                    writer.WriteLine($"public override {propertyType} {codeElement.Name.ToFirstCharacterUpperCase()} {{ get => base.Message; }}");
                }

                break;
            case CodePropertyKind.QueryParameter when codeElement.IsNameEscaped:
                writer.WriteLine($"[QueryParameter(\"{codeElement.SerializationName}\")]");
                goto default;
            case CodePropertyKind.QueryParameters:
                defaultValue = $" = new {propertyType}();";
                goto default;
            default:
                writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)} {propertyType} {codeElement.Name.ToFirstCharacterUpperCase()} {{ {simpleBody} }}{defaultValue}");
                break;
        }
    }
}
