using Microsoft.AspNetCore.Mvc;
using ReQuesty.Demo.Api.Controllers.Base;
using ReQuesty.Demo.Api.Models;

namespace ReQuesty.Demo.Api.Controllers;

/// <summary>
///   A controller to test the behavior of query parameter binding
/// </summary>
public class QueryParameterController : DemoControllerBase
{
    /// <summary>
    ///   Echoes back a string query parameter.
    /// </summary>
    /// <param name="value">The string value to echo.</param>
    /// <returns></returns>
    [HttpGet("string", Name = "GetQueryString")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<string>> GetQueryStringAsync([FromQuery] string value)
    {
        return Ok(value);
    }

    /// <summary>
    ///   Echoes back an integer query parameter.
    /// </summary>
    /// <param name="value">The integer value to echo.</param>
    /// <returns></returns>
    [HttpGet("integer", Name = "GetQueryInteger")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<int>> GetQueryIntegerAsync([FromQuery] int value)
    {
        return Ok(value);
    }

    /// <summary>
    ///   Echoes back a Guid query parameter.
    /// </summary>
    /// <param name="value">The Guid value to echo.</param>
    /// <returns></returns>
    [HttpGet("guid", Name = "GetQueryGuid")]
    [ProducesResponseType<Guid>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<Guid>> GetQueryGuidAsync([FromQuery] Guid value)
    {
        return Ok(value);
    }

    /// <summary>
    ///   Echoes back a bool query parameter.
    /// </summary>
    /// <param name="value">The bool value to echo.</param>
    /// <returns></returns>
    [HttpGet("bool", Name = "GetQueryBool")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<bool>> GetQueryBoolAsync([FromQuery] bool value)
    {
        return Ok(value);
    }

    /// <summary>
    ///   Returns a filtered list of SomeObject based on age range query parameters.
    /// </summary>
    /// <param name="minAge">The minimum age (inclusive).</param>
    /// <param name="maxAge">The maximum age (inclusive).</param>
    /// <returns></returns>
    [HttpGet("filter", Name = "GetFilteredObjects")]
    [ProducesResponseType<List<SomeObject>>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<List<SomeObject>>> GetFilteredObjectsAsync([FromQuery] int minAge, [FromQuery] int maxAge)
    {
        List<SomeObject> objects =
        [
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Object One",
                Age = 10,
                RequestedAt = DateTimeOffset.UtcNow,
                Cost = 10.0
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Object Two",
                Age = 25,
                RequestedAt = DateTimeOffset.UtcNow,
                Cost = 25.0
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Object Three",
                Age = 40,
                RequestedAt = DateTimeOffset.UtcNow,
                Cost = 40.0
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Object Four",
                Age = 70,
                RequestedAt = DateTimeOffset.UtcNow,
                Cost = 70.0
            },
        ];

        return Ok(objects.Where(o => o.Age >= minAge && o.Age <= maxAge).ToList());
    }
}
