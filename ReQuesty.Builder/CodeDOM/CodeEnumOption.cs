﻿using ReQuesty.Builder.Extensions;

namespace ReQuesty.Builder.CodeDOM;

public class CodeEnumOption : CodeElement, IDocumentedElement, ITypeDefinition, IAlternativeName
{
    /// <inheritdoc/>
    public string SerializationName { get; set; } = string.Empty;
    public CodeDocumentation Documentation { get; set; } = new();
    /// <inheritdoc/>
    public bool IsNameEscaped
    {
        get => !string.IsNullOrEmpty(SerializationName);
    }
    /// <inheritdoc/>
    public string WireName => IsNameEscaped ? SerializationName : Name;
    /// <inheritdoc/>
    public string SymbolName
    {
        get => IsNameEscaped ? SerializationName.CleanupSymbolName() : Name;
    }
}
