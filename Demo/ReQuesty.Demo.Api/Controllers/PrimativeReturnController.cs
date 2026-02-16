using Microsoft.AspNetCore.Mvc;
using ReQuesty.Demo.Api.Controllers.Base;

namespace ReQuesty.Demo.Api.Controllers;

/// <summary>
///
/// </summary>
public class PrimativeReturnController : DemoControllerBase
{
    /// <summary>
    ///   Gets a string based on the specified return type.
    /// </summary>
    /// <returns></returns>
    [HttpGet("string", Name = "GetString")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<string>> GetStringAsync([FromQuery] ReturnType returnType)
    {
        return Ok(returnType switch
        {
            ReturnType.Null => null,
            ReturnType.Random => Guid.NewGuid().ToString(),
            ReturnType.Invalid => 123,
            _ => throw new()
        });
    }

    /// <summary>
    ///   Gets a nullable string based on the specified return type.
    /// </summary>
    /// <returns></returns>
    [HttpGet("nullable-string", Name = "GetNullableString")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)] // <string?> isn't valid here
    public async ValueTask<ActionResult<string?>> GetNullableStringAsync([FromQuery] ReturnType returnType)
    {
        return Ok(returnType switch
        {
            ReturnType.Null => null,
            ReturnType.Random => Guid.NewGuid().ToString(),
            ReturnType.Invalid => 123,
            _ => throw new()
        });
    }

    /// <summary>
    ///   Gets an int based on the specified return type.
    /// </summary>
    /// <returns></returns>
    [HttpGet("int", Name = "GetInt")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<int>> GetIntAsync([FromQuery] ReturnType returnType)
    {
        return Ok(returnType switch
        {
            ReturnType.Null => null,
            ReturnType.Random => Random.Shared.Next(),
            ReturnType.Invalid => "not an int",
            _ => throw new()
        });
    }

    /// <summary>
    ///   Gets a nullable int based on the specified return type.
    /// </summary>
    /// <returns></returns>
    [HttpGet("nullable-int", Name = "GetNullableInt")]
    [ProducesResponseType<int?>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<int?>> GetNullableIntAsync([FromQuery] ReturnType returnType)
    {
        return Ok(returnType switch
        {
            ReturnType.Null => null,
            ReturnType.Random => Random.Shared.Next(),
            ReturnType.Invalid => "not an int",
            _ => throw new()
        });
    }

    /// <summary>
    ///   Gets an Guid based on the specified return type.
    /// </summary>
    /// <returns></returns>
    [HttpGet("guid", Name = "GetGuid")]
    [ProducesResponseType<Guid>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<Guid>> GetGuidAsync([FromQuery] ReturnType returnType)
    {
        return Ok(returnType switch
        {
            ReturnType.Null => null,
            ReturnType.Random => Guid.NewGuid(),
            ReturnType.Invalid => "not a guid",
            _ => throw new()
        });
    }

    /// <summary>
    ///   Gets a nullable guid based on the specified return type.
    /// </summary>
    /// <returns></returns>
    [HttpGet("nullable-guid", Name = "GetNullableGuid")]
    [ProducesResponseType<Guid?>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<Guid?>> GetNullableGuidAsync([FromQuery] ReturnType returnType)
    {
        return Ok(returnType switch
        {
            ReturnType.Null => null,
            ReturnType.Random => Guid.NewGuid(),
            ReturnType.Invalid => "not a guid",
            _ => throw new()
        });
    }
}
