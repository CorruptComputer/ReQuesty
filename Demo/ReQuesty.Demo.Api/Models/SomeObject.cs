namespace ReQuesty.Demo.Api.Models;

/// <summary>
///   A sample object model to be used in the demo API
/// </summary>
public sealed record SomeObject
{
    /// <summary>
    ///   The unique identifier of the object
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    ///   The name of the object
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///   The age of the object
    /// </summary>
    public required int Age { get; init; }

    /// <summary>
    ///   The timestamp of when the object was requested
    /// </summary>
    public required DateTimeOffset RequestedAt { get; init; }

    /// <summary>
    ///   The cost of the object
    /// </summary>
    public required double Cost { get; init; }
}
