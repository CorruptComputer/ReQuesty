using System.Text.Json.Serialization;

namespace ReQuesty.Demo.Api.Models.Animals;

/// <summary>
///   The base record for all dog types
/// </summary>
[JsonPolymorphic]
[JsonDerivedType(typeof(GoldenRetriever), nameof(GoldenRetriever))]
[JsonDerivedType(typeof(Boxer), nameof(Boxer))]
public abstract record Dog : AnimalBase
{
    /// <summary>
    ///   Whether the dog has been trained
    /// </summary>
    public required bool IsTrained { get; init; }
}
