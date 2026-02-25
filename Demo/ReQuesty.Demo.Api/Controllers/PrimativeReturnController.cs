using Microsoft.AspNetCore.Mvc;
using ReQuesty.Demo.Api.Controllers.Base;

namespace ReQuesty.Demo.Api.Controllers;

/// <summary>
///   A controller to test the behavior of primative return types
/// </summary>
public class PrimativeReturnController : DemoControllerBase
{
    #region Strings
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
    [HttpGet("string/nullable", Name = "GetNullableString")]
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
    #endregion

    #region Integers
    /// <summary>
    ///   Gets an int based on the specified return type.
    /// </summary>
    /// <returns></returns>
    [HttpGet("integer", Name = "GetInt")]
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
    [HttpGet("integer/nullable", Name = "GetNullableInt")]
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
    #endregion

    #region Doubles
    /// <summary>
    ///   Gets an int based on the specified return type.
    /// </summary>
    /// <returns></returns>
    [HttpGet("double", Name = "GetDouble")]
    [ProducesResponseType<double>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<double>> GetDoubleAsync([FromQuery] ReturnType returnType)
    {
        return Ok(returnType switch
        {
            ReturnType.Null => null,
            ReturnType.Random => Random.Shared.NextDouble(),
            ReturnType.Invalid => "not a double",
            _ => throw new()
        });
    }

    /// <summary>
    ///   Gets a nullable double based on the specified return type.
    /// </summary>
    /// <returns></returns>
    [HttpGet("double/nullable", Name = "GetNullableDouble")]
    [ProducesResponseType<double?>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<double?>> GetNullableDoubleAsync([FromQuery] ReturnType returnType)
    {
        return Ok(returnType switch
        {
            ReturnType.Null => null,
            ReturnType.Random => Random.Shared.NextDouble(),
            ReturnType.Invalid => "not a double",
            _ => throw new()
        });
    }
    #endregion

    #region Floats
    /// <summary>
    ///   Gets a float based on the specified return type.
    /// </summary>
    /// <returns></returns>
    [HttpGet("float", Name = "GetFloat")]
    [ProducesResponseType<float>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<float>> GetFloatAsync([FromQuery] ReturnType returnType)
    {
        return Ok(returnType switch
        {
            ReturnType.Null => null,
            ReturnType.Random => (float)Random.Shared.NextDouble(),
            ReturnType.Invalid => "not a float",
            _ => throw new()
        });
    }

    /// <summary>
    ///   Gets a nullable float based on the specified return type.
    /// </summary>
    /// <returns></returns>
    [HttpGet("float/nullable", Name = "GetNullableFloat")]
    [ProducesResponseType<float?>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<float?>> GetNullableFloatAsync([FromQuery] ReturnType returnType)
    {
        return Ok(returnType switch
        {
            ReturnType.Null => null,
            ReturnType.Random => (float)Random.Shared.NextDouble(),
            ReturnType.Invalid => "not a float",
            _ => throw new()
        });
    }
    #endregion

    #region Guids
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
    [HttpGet("guid/nullable", Name = "GetNullableGuid")]
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
    #endregion
}
