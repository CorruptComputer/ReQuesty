using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using ReQuesty.Builder.Extensions;

namespace ReQuesty.Builder.CodeDOM;

public enum CodeClassKind
{
    Custom,
    RequestBuilder,
    Model,
    QueryParameters,
    /// <summary>
    /// A single parameter to be provided by the SDK user which will contain query parameters, request body, options, etc.
    /// Only used for languages that do not support overloads or optional parameters like go.
    /// </summary>
    ParameterSet,
    /// <summary>
    /// A class used as a placeholder for the barrel file.
    /// </summary>
    BarrelInitializer,
    /// <summary>
    /// Configuration for the request to be sent with the headers, query parameters, and middleware options
    /// </summary>
    RequestConfiguration,
}
/// <summary>
/// CodeClass represents an instance of a Class to be generated in source code
/// </summary>
public class CodeClass : ProprietableBlock<CodeClassKind, ClassDeclaration>, ITypeDefinition, IDiscriminatorInformationHolder, IDeprecableElement, IAccessibleElement
{
    private readonly ConcurrentDictionary<string, CodeProperty> PropertiesByWireName = new(StringComparer.OrdinalIgnoreCase);

    public AccessModifier Access { get; set; } = AccessModifier.Public;

    public bool IsErrorDefinition
    {
        get; set;
    }

    /// <summary>
    /// Original composed type this class was generated for.
    /// </summary>
    public CodeComposedTypeBase? OriginalComposedType
    {
        get; set;
    }
    public string GetComponentSchemaName(CodeNamespace modelsNamespace)
    {
        if (Kind is not CodeClassKind.Model ||
                Parent is not CodeNamespace parentNamespace ||
                !parentNamespace.IsChildOf(modelsNamespace))
        {
            return string.Empty;
        }

        return $"{parentNamespace.Name[(modelsNamespace.Name.Length + 1)..]}.{Name}";
    }
    public CodeIndexer? Indexer => InnerChildElements.Values.OfType<CodeIndexer>().FirstOrDefault(static x => !x.IsLegacyIndexer);
    public void AddIndexer(params CodeIndexer[] indexers)
    {
        if (indexers is null || Array.Exists(indexers, static x => x is null))
        {
            throw new ArgumentNullException(nameof(indexers));
        }

        if (indexers.Length == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(indexers));
        }

        foreach (CodeIndexer value in indexers)
        {
            CodeIndexer[] existingIndexers = InnerChildElements.Values.OfType<CodeIndexer>().ToArray();
            if (Array.Exists(existingIndexers, x => !x.IndexParameter.Name.Equals(value.IndexParameter.Name, StringComparison.OrdinalIgnoreCase)) ||
                    InnerChildElements.Values.OfType<CodeMethod>().Any(static x => x.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility)))
            {
                foreach (CodeIndexer? existingIndexer in existingIndexers)
                {
                    RemoveChildElement(existingIndexer);
                    AddRange(CodeMethod.FromIndexer(existingIndexer, static x => $"With{x.ToFirstCharacterUpperCase()}", static x => x.ToFirstCharacterUpperCase(), true));
                }
                AddRange(CodeMethod.FromIndexer(value, static x => $"With{x.ToFirstCharacterUpperCase()}", static x => x.ToFirstCharacterUpperCase(), false));
            }
            else
            {
                AddRange(value);
            }
        }
    }
    public override IEnumerable<CodeProperty> AddProperty(params CodeProperty[] properties)
    {
        if (properties is null || properties.Any(static x => x is null))
        {
            throw new ArgumentNullException(nameof(properties));
        }

        if (properties.Length == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(properties));
        }

        return properties.Select(property =>
        {
            if (property.IsOfKind(CodePropertyKind.Custom, CodePropertyKind.QueryParameter))
            {
                if (GetOriginalPropertyDefinedFromBaseType(property.WireName) is CodeProperty original)
                {
                    // the property already exists in a parent type, use its name
                    property.Name = original.Name;
                    property.SerializationName = original.SerializationName;
                    property.OriginalPropertyFromBaseType = original;
                }
                else
                {
                    string uniquePropertyName = ResolveUniquePropertyName(property.Name);
                    if (!uniquePropertyName.Equals(property.Name, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(property.SerializationName))
                    {
                        property.SerializationName = property.Name;
                    }

                    property.Name = uniquePropertyName;
                }
            }
            CodeProperty result = base.AddProperty(property).First();
            return PropertiesByWireName.GetOrAdd(result.WireName, result);
        }).ToArray();
    }
    public override void RenameChildElement(string oldName, string newName)
    {
        if (InnerChildElements.TryRemove(oldName, out CodeElement? element))
        {
            if (element is CodeProperty removedProperty)
            {
                PropertiesByWireName.TryRemove(removedProperty.WireName, out _);
            }
            element.Name = newName;
            AddRange(element);
            if (element is CodeProperty propertyToAdd)
            {
                PropertiesByWireName.TryAdd(propertyToAdd.WireName, propertyToAdd);
            }
        }
        else
        {
            throw new InvalidOperationException($"The element {oldName} could not be found in the class {Name}");
        }
    }
    public override void RemoveChildElementByName(params string[] names)
    {
        if (names is null)
        {
            return;
        }

        foreach (string name in names)
        {
            if (InnerChildElements.TryRemove(name, out CodeElement? removedElement))
            {
                if (removedElement is CodeProperty removedProperty)
                {
                    PropertiesByWireName.TryRemove(removedProperty.WireName, out _);
                }
            }
            else
            {
                throw new InvalidOperationException($"The element {name} could not be found in the class {Name}");
            }
        }
    }
    public void RemoveMethodByKinds(params CodeMethodKind[] kinds)
    {
        RemoveChildElementByName(InnerChildElements.Where(x => x.Value is CodeMethod method && method.IsOfKind(kinds)).Select(static x => x.Key).ToArray());
    }
    private string ResolveUniquePropertyName(string name)
    {
        if (FindPropertyByNameInTypeHierarchy(name) is null)
        {
            return name;
        }
        // the CodeClass.Name is not very useful as prefix for the property name, so keep the original name and add a number
        string nameWithTypeName = Kind == CodeClassKind.QueryParameters ? name : Name + name.ToFirstCharacterUpperCase();
        if (Kind != CodeClassKind.QueryParameters && FindPropertyByNameInTypeHierarchy(nameWithTypeName) is null)
        {
            return nameWithTypeName;
        }

        int i = 0;
        while (FindPropertyByNameInTypeHierarchy(nameWithTypeName + i) is not null)
        {
            i++;
        }

        return nameWithTypeName + i;
    }
    private CodeProperty? FindPropertyByNameInTypeHierarchy(string propertyName)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        if (FindChildByName<CodeProperty>(propertyName, findInChildElements: false) is CodeProperty result)
        {
            return result;
        }
        if (BaseClass is CodeClass currentParentClass)
        {
            return currentParentClass.FindPropertyByNameInTypeHierarchy(propertyName);
        }
        return default;
    }
    private CodeProperty? GetOriginalPropertyDefinedFromBaseType(string serializationName)
    {
        ArgumentException.ThrowIfNullOrEmpty(serializationName);

        if (BaseClass is CodeClass currentParentClass)
        {
            if (currentParentClass.FindPropertyByWireName(serializationName) is CodeProperty currentProperty && !currentProperty.ExistsInBaseType && currentProperty.Kind is not CodePropertyKind.AdditionalData or CodePropertyKind.BackingStore)
            {
                return currentProperty;
            }
            else
            {
                return currentParentClass.GetOriginalPropertyDefinedFromBaseType(serializationName);
            }
        }

        return default;
    }
    private CodeProperty? FindPropertyByWireName(string wireName)
    {
        return PropertiesByWireName.TryGetValue(wireName, out CodeProperty? result) ? result : default;
    }
    public bool ContainsPropertyWithWireName(string wireName)
    {
        return PropertiesByWireName.ContainsKey(wireName);
    }
    public IEnumerable<CodeClass> AddInnerClass(params CodeClass[] codeClasses)
    {
        if (codeClasses is null || codeClasses.Any(static x => x is null))
        {
            throw new ArgumentNullException(nameof(codeClasses));
        }

        if (codeClasses.Length == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(codeClasses));
        }

        return AddRange(codeClasses);
    }
    public IEnumerable<CodeInterface> AddInnerInterface(params CodeInterface[] codeInterfaces)
    {
        if (codeInterfaces is null || codeInterfaces.Any(static x => x is null))
        {
            throw new ArgumentNullException(nameof(codeInterfaces));
        }

        if (codeInterfaces.Length == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(codeInterfaces));
        }

        return AddRange(codeInterfaces);
    }
    public CodeClass? BaseClass => StartBlock.Inherits?.TypeDefinition as CodeClass;
    /// <summary>
    /// The interface associated with this class, if any.
    /// </summary>
    public CodeInterface? AssociatedInterface
    {
        get; set;
    }
    public bool DerivesFrom(CodeClass codeClass)
    {
        ArgumentNullException.ThrowIfNull(codeClass);
        CodeClass? parent = BaseClass;
        if (parent is null)
        {
            return false;
        }

        if (parent == codeClass)
        {
            return true;
        }

        return parent.DerivesFrom(codeClass);
    }
    public Collection<CodeClass> GetInheritanceTree(bool currentNamespaceOnly = false, bool includeCurrentClass = true)
    {
        CodeClass? parentClass = BaseClass;
        if (parentClass is null || (currentNamespaceOnly && parentClass.GetImmediateParentOfType<CodeNamespace>() != GetImmediateParentOfType<CodeNamespace>()))
        {
            if (includeCurrentClass)
            {
                return [this];
            }
            else
            {
                return [];
            }
        }

        Collection<CodeClass> result = parentClass.GetInheritanceTree(currentNamespaceOnly);
        result.Add(this);
        return result;
    }
    public CodeClass? GetGreatestGrandparent(CodeClass? startClassToSkip = default)
    {
        CodeClass? parentClass = BaseClass;
        if (parentClass is null)
        {
            return startClassToSkip is not null && startClassToSkip == this ? null : this;
        }
        // we don't want to return the current class if this is the start node in the inheritance tree and doesn't have parent
        return parentClass.GetGreatestGrandparent(startClassToSkip);
    }
    private DiscriminatorInformation? _discriminatorInformation;
    /// <inheritdoc />
    public DiscriminatorInformation DiscriminatorInformation
    {
        get
        {
            if (_discriminatorInformation is null)
            {
                DiscriminatorInformation = new DiscriminatorInformation();
            }

            return _discriminatorInformation!;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            EnsureElementsAreChildren(value);
            _discriminatorInformation = value;
        }
    }
    public DeprecationInformation? Deprecation
    {
        get; set;
    }
}
public class ClassDeclaration : ProprietableBlockDeclaration
{
    private CodeType? inherits;
    public CodeType? Inherits
    {
        get => inherits; set
        {
            if (value is not null && !value.IsExternal && Parent is CodeClass codeClass && codeClass.Properties.Any())
            {
                throw new InvalidOperationException("Cannot change the inherits-property of an already populated type");
            }

            EnsureElementsAreChildren(value);
            inherits = value;
        }
    }
    public CodeProperty? GetOriginalPropertyDefinedFromBaseType(string propertyName)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        if (inherits is CodeType currentInheritsType &&
            !inherits.IsExternal &&
            currentInheritsType.TypeDefinition is CodeClass currentParentClass)
        {
            if (currentParentClass.FindChildByName<CodeProperty>(propertyName, false) is CodeProperty currentProperty && !currentProperty.ExistsInBaseType)
            {
                return currentProperty;
            }
            else
            {
                return currentParentClass.StartBlock.GetOriginalPropertyDefinedFromBaseType(propertyName);
            }
        }

        return default;
    }
    public bool InheritsFrom(CodeClass candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (inherits is CodeType currentInheritsType &&
            currentInheritsType.TypeDefinition is CodeClass currentParentClass)
        {
            if (currentParentClass == candidate)
            {
                return true;
            }
            else
            {
                return currentParentClass.StartBlock.InheritsFrom(candidate);
            }
        }

        return false;
    }
}

