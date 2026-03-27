namespace ReQuesty.Demo.Api.Models.Animals;

/// <summary>
///   A cat
/// </summary>
public sealed record Cat : AnimalBase
{
    /// <summary>
    ///   Whether the cat lives indoors
    /// </summary>
    public required bool IsIndoor { get; init; }
}
