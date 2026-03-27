namespace ReQuesty.Demo.Api.Models.Animals;

/// <summary>
///   A Golden Retriever dog
/// </summary>
public sealed record GoldenRetriever : Dog
{
    /// <summary>
    ///   The fur colour of the Golden Retriever
    /// </summary>
    public required string FurColour { get; init; }
}
