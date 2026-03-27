using Microsoft.AspNetCore.Mvc;
using ReQuesty.Demo.Api.Controllers.Base;
using ReQuesty.Demo.Api.Models;

namespace ReQuesty.Demo.Api.Controllers;

/// <summary>
///   A controller to demonstrate handling of path parameters in ReQuesty.
/// </summary>
public class PathParameterController : DemoControllerBase
{
    /// <summary>
    ///   Gets a SomeObject based on the specified return type.
    /// </summary>
    /// <param name="integerId">The integer ID to determine the return type.</param>
    /// <returns></returns>
    [HttpGet("integerId/{integerId:int}", Name = "GetSomeObjectByIntegerId")]
    [ProducesResponseType<SomeObject>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<SomeObject>> GetSomeObjectByIntegerIdAsync([FromRoute] int integerId)
    {
        return new SomeObject
        {
            Id = Guid.NewGuid(),
            Name = "Sample Object",
            Age = 25,
            RequestedAt = DateTimeOffset.UtcNow,
            Cost = 100.0
        };
    }

    /// <summary>
    ///   Gets a SomeObject based on the specified return type.
    /// </summary>
    /// <param name="guidId">The GUID ID to determine the return type.</param>
    /// <returns></returns>
    [HttpGet("guidId/{guidId:guid}", Name = "GetSomeObjectByGuidId")]
    [ProducesResponseType<SomeObject>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<SomeObject>> GetSomeObjectByGuidIdAsync([FromRoute] Guid guidId)
    {
        return new SomeObject
        {
            Id = guidId,
            Name = "Sample Object",
            Age = 25,
            RequestedAt = DateTimeOffset.UtcNow,
            Cost = 100.0
        };
    }

    /// <summary>
    ///   Gets a SomeObject based on a string ID path parameter.
    /// </summary>
    /// <param name="stringId">The string ID.</param>
    /// <returns></returns>
    [HttpGet("stringId/{stringId}", Name = "GetSomeObjectByStringId")]
    [ProducesResponseType<SomeObject>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<SomeObject>> GetSomeObjectByStringIdAsync([FromRoute] string stringId)
    {
        return new SomeObject
        {
            Id = Guid.NewGuid(),
            Name = stringId,
            Age = 25,
            RequestedAt = DateTimeOffset.UtcNow,
            Cost = 100.0
        };
    }

    /// <summary>
    ///   Gets a SomeObject based on a long ID path parameter.
    /// </summary>
    /// <param name="longId">The long ID.</param>
    /// <returns></returns>
    [HttpGet("longId/{longId:long}", Name = "GetSomeObjectByLongId")]
    [ProducesResponseType<SomeObject>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<SomeObject>> GetSomeObjectByLongIdAsync([FromRoute] long longId)
    {
        return new SomeObject
        {
            Id = Guid.NewGuid(),
            Name = $"Object {longId}",
            Age = 25,
            RequestedAt = DateTimeOffset.UtcNow,
            Cost = longId
        };
    }
}