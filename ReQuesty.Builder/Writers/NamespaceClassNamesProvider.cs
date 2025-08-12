using ReQuesty.Builder.CodeDOM;

namespace ReQuesty.Builder.Writers;

public static class NamespaceClassNamesProvider
{
    /// <summary>
    /// Orders given list of classes in a namespace based on inheritance.
    /// That is, if class B extends class A then A should exported before class B.
    /// </summary>
    /// <param name="codeNamespace"> Code Namespace to get the classes for</param>
    /// <returns> List of class names in the code name space ordered based on inheritance</returns>
    public static void WriteClassesInOrderOfInheritance(CodeNamespace codeNamespace, Action<CodeClass> callbackToWriteImport)
    {
        ArgumentNullException.ThrowIfNull(codeNamespace);
        ArgumentNullException.ThrowIfNull(callbackToWriteImport);
        HashSet<string> writtenClassNames = new(StringComparer.OrdinalIgnoreCase);
        List<System.Collections.ObjectModel.Collection<CodeClass>> inheritanceBranches = codeNamespace.Classes.Where(c => c.IsOfKind(CodeClassKind.Model))
                                                .Select(static x => x.GetInheritanceTree(true))
                                                .ToList();
        int maxDepth = inheritanceBranches.Count != 0 ? inheritanceBranches.Max(static x => x.Count) : 0;
        for (int depth = 0; depth < maxDepth; depth++)
        {
            foreach (CodeClass? name in inheritanceBranches
                                                .Where(x => x.Count > depth)
                                                .Select(x => x[depth])
                                                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)//order is important to get a deterministic output
                                                .Where(x => writtenClassNames.Add(x.Name))) // linq distinct does not guarantee order
            {
                callbackToWriteImport(name);
            }
        }
    }
}
