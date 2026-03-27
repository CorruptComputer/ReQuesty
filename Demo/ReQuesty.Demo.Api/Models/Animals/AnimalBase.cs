using System.Text.Json.Serialization;

namespace ReQuesty.Demo.Api.Models.Animals;

/// <summary>
///   The base record for all animal types
/// </summary>
[JsonPolymorphic]
[JsonDerivedType(typeof(GoldenRetriever), nameof(GoldenRetriever))]
[JsonDerivedType(typeof(Boxer), nameof(Boxer))]
[JsonDerivedType(typeof(Cat), nameof(Cat))]
public abstract record AnimalBase
{
    /// <summary>
    ///   The name of the animal
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///   The age of the animal in years
    /// </summary>
    public required int AgeYears { get; init; }
}
