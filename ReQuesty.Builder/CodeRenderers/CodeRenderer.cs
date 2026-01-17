using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.Configuration;
using ReQuesty.Builder.OrderComparers;
using ReQuesty.Builder.Writers;

namespace ReQuesty.Builder.CodeRenderers;

/// <summary>
/// Convert CodeDOM classes to strings or files
/// </summary>
public class CodeRenderer
{
    public CodeRenderer(GenerationConfiguration configuration, CodeElementOrderComparer? elementComparer = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        Configuration = configuration;
        _rendererElementComparer = elementComparer ?? new CodeElementOrderComparer();
    }

    public async Task RenderCodeNamespaceToSingleFileAsync(LanguageWriter writer, CodeElement codeElement, string outputFile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentException.ThrowIfNullOrEmpty(outputFile);
        await using FileStream stream = new(outputFile, FileMode.Create);

        StreamWriter sw = new(stream);
        writer.SetTextWriter(sw);
        RenderCode(writer, codeElement);
        if (!cancellationToken.IsCancellationRequested)
        {
            await sw.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
    // We created barrels for code namespaces. Skipping for empty namespaces, ones created for users, and ones with same namespace as class name.
    public async Task RenderCodeNamespaceToFilePerClassAsync(LanguageWriter writer, CodeNamespace currentNamespace, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(currentNamespace);
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        foreach (CodeElement codeElement in currentNamespace.GetChildElements(true))
        {
            switch (codeElement)
            {
                case CodeClass:
                case CodeEnum:
                case CodeFunction:
                case CodeInterface:
                case CodeFile:
                    if (writer.PathSegmenter?.GetPath(currentNamespace, codeElement) is string path)
                    {
                        await RenderCodeNamespaceToSingleFileAsync(writer, codeElement, path, cancellationToken).ConfigureAwait(false);
                    }

                    break;
                case CodeNamespace codeNamespace:
                    await RenderCodeNamespaceToFilePerClassAsync(writer, codeNamespace, cancellationToken).ConfigureAwait(false);
                    break;
            }
        }
    }
    private readonly CodeElementOrderComparer _rendererElementComparer;
    protected GenerationConfiguration Configuration
    {
        get; private set;
    }
    private void RenderCode(LanguageWriter writer, CodeElement element)
    {
        writer.Write(element);
        if (element is not CodeNamespace)
        {
            foreach (CodeElement? childElement in element.GetChildElements()
                                                .Order(_rendererElementComparer))
            {
                RenderCode(writer, childElement);
            }
        }
    }

    public virtual bool ShouldRenderNamespaceFile(CodeNamespace codeNamespace)
    {
        if (codeNamespace is null)
        {
            return false;
        }
        // if the module already has a class with the same name, it's going to be declared automatically
        string namespaceNameLastSegment = codeNamespace.Name.Split('.')[^1];
        return Configuration.ShouldWriteBarrelsIfClassExists || codeNamespace.FindChildByName<CodeClass>(namespaceNameLastSegment, false) == null;
    }

    public static CodeRenderer GetCodeRender(GenerationConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return new CodeRenderer(config);
    }

}
