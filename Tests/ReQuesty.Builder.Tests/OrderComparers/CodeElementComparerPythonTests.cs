using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.OrderComparers;
using Xunit;

namespace ReQuesty.Builder.Tests.OrderComparers;
public class CodeElementComparerPythonTests
{
    [Fact]
    public void OrdersWithMethodWithinClass()
    {
        CodeNamespace root = CodeNamespace.InitRootNamespace();
        CodeElementOrderComparerPython comparer = new();
        CodeClass codeClass = new()
        {
            Name = "Class"
        };
        root.AddClass(codeClass);
        CodeMethod method = new()
        {
            Name = "Method",
            ReturnType = new CodeType
            {
                Name = "string"
            }
        };
        codeClass.AddMethod(method);
        method.AddParameter(new CodeParameter
        {
            Name = "param",
            Type = new CodeType
            {
                Name = "string"
            }
        });
        List<Tuple<CodeElement, CodeElement, int>> dataSet =
        [
            new(null!, null!, 0),
            new(null!, new CodeClass(), -1),
            new(new CodeClass(), null!, 1),
            new(new CodeUsing(), new CodeProperty() {
                Name = "prop",
                Type = new CodeType {
                    Name = "string"
                }
            }, -10100),
            new(new CodeIndexer() {
                ReturnType = new CodeType {
                    Name = "string"
                },
                IndexParameter = new() {
                    Name = "param",
                    Type = new CodeType {
                        Name = "string"
                    },
                }
            }, new CodeProperty() {
                Name = "prop",
                Type = new CodeType {
                    Name = "string"
                }
            }, -10100),
            new(method, new CodeProperty() {
                Name = "prop",
                Type = new CodeType {
                    Name = "string"
                }
            }, -9899),
            new(method, codeClass, -9699),
            new(new CodeMethod() {
                Kind = CodeMethodKind.Constructor,
                ReturnType = new CodeType
                {
                    Name = "null",
                }
            }, method, -301),
            new(new CodeMethod() {
                Kind = CodeMethodKind.ClientConstructor,
                ReturnType = new CodeType
                {
                    Name = "null",
                }
            }, method, -301),

        ];
        foreach (Tuple<CodeElement, CodeElement, int> dataEntry in dataSet)
        {
            Assert.Equal(dataEntry.Item3, comparer.Compare(dataEntry.Item1, dataEntry.Item2));
        }
    }
}
