using Microsoft.AspNetCore.Mvc;
using ReQuesty.Demo.Api.Controllers.Base;
using ReQuesty.Demo.Api.Models;

namespace ReQuesty.Demo.Api.Controllers;

/// <summary>
///   A controller to test the behavior of object return types
/// </summary>
public class ObjectReturnController : DemoControllerBase
{
    /// <summary>
    ///   Gets a SomeObject based on the specified return type.
    /// </summary>
    /// <returns></returns>
    [HttpGet("some-object", Name = "GetSomeObject")]
    [ProducesResponseType<SomeObject>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<SomeObject>> GetSomeObjectAsync([FromQuery] ReturnType returnType)
    {
        return Ok(returnType switch
        {
            ReturnType.Null => null,
            ReturnType.Random => new SomeObject
            {
                Id = Guid.NewGuid(),
                Name = "Sample Object",
                Age = 25,
                RequestedAt = DateTimeOffset.UtcNow,
                Cost = 100.0
            },
            ReturnType.Invalid => -1,
            _ => throw new()
        });
    }
}
