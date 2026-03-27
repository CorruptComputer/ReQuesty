namespace ReQuesty.Demo.Api.Models.Animals;

/// <summary>
///   A Boxer dog
/// </summary>
public sealed record Boxer : Dog
{
    /// <summary>
    ///   Whether the Boxer has a brindle coat
    /// </summary>
    public required bool IsBrindle { get; init; }
}
