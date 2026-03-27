using Microsoft.AspNetCore.Mvc;
using ReQuesty.Demo.Api.Controllers.Base;
using ReQuesty.Demo.Api.Models;

namespace ReQuesty.Demo.Api.Controllers;

/// <summary>
///   A controller to test the behavior of collection return types
/// </summary>
public class CollectionReturnController : DemoControllerBase
{
    #region Integer Arrays
    /// <summary>
    ///   Gets an int array based on the specified return type.
    /// </summary>
    /// <returns></returns>
    [HttpGet("integers", Name = "GetIntArray")]
    [ProducesResponseType<int[]>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<int[]>> GetIntArrayAsync([FromQuery] ReturnType returnType)
    {
        return Ok(returnType switch
        {
            ReturnType.Null => null,
            ReturnType.Random => new[] { Random.Shared.Next(), Random.Shared.Next(), Random.Shared.Next() },
            ReturnType.Invalid => "not an array",
            _ => throw new()
        });
    }

    /// <summary>
    ///   Gets a nullable int array based on the specified return type.
    /// </summary>
    /// <returns></returns>
    [HttpGet("integers/nullable", Name = "GetNullableIntArray")]
    [ProducesResponseType<int[]>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<int[]?>> GetNullableIntArrayAsync([FromQuery] ReturnType returnType)
    {
        return Ok(returnType switch
        {
            ReturnType.Null => null,
            ReturnType.Random => new[] { Random.Shared.Next(), Random.Shared.Next(), Random.Shared.Next() },
            ReturnType.Invalid => "not an array",
            _ => throw new()
        });
    }
    #endregion

    #region String Lists
    /// <summary>
    ///   Gets a list of strings based on the specified return type.
    /// </summary>
    /// <returns></returns>
    [HttpGet("strings", Name = "GetStringList")]
    [ProducesResponseType<List<string>>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<List<string>>> GetStringListAsync([FromQuery] ReturnType returnType)
    {
        return Ok(returnType switch
        {
            ReturnType.Null => null,
            ReturnType.Random => new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            ReturnType.Invalid => "not a list",
            _ => throw new()
        });
    }

    /// <summary>
    ///   Gets a nullable list of strings based on the specified return type.
    /// </summary>
    /// <returns></returns>
    [HttpGet("strings/nullable", Name = "GetNullableStringList")]
    [ProducesResponseType<List<string>>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<List<string>?>> GetNullableStringListAsync([FromQuery] ReturnType returnType)
    {
        return Ok(returnType switch
        {
            ReturnType.Null => null,
            ReturnType.Random => new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            ReturnType.Invalid => "not a list",
            _ => throw new()
        });
    }
    #endregion

    #region Object Lists
    /// <summary>
    ///   Gets a list of SomeObject based on the specified return type.
    /// </summary>
    /// <returns></returns>
    [HttpGet("objects", Name = "GetObjectList")]
    [ProducesResponseType<List<SomeObject>>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<List<SomeObject>>> GetObjectListAsync([FromQuery] ReturnType returnType)
    {
        return Ok(returnType switch
        {
            ReturnType.Null => null,
            ReturnType.Random => new List<SomeObject>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Object One",
                    Age = 10,
                    RequestedAt = DateTimeOffset.UtcNow,
                    Cost = 1.0
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Object Two",
                    Age = 20,
                    RequestedAt = DateTimeOffset.UtcNow,
                    Cost = 2.0
                },
            },
            ReturnType.Invalid => "not a list",
            _ => throw new()
        });
    }

    /// <summary>
    ///   Gets a nullable list of SomeObject based on the specified return type.
    /// </summary>
    /// <returns></returns>
    [HttpGet("objects/nullable", Name = "GetNullableObjectList")]
    [ProducesResponseType<List<SomeObject>>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<List<SomeObject>?>> GetNullableObjectListAsync([FromQuery] ReturnType returnType)
    {
        return Ok(returnType switch
        {
            ReturnType.Null => null,
            ReturnType.Random => new List<SomeObject>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Object One",
                    Age = 10,
                    RequestedAt = DateTimeOffset.UtcNow,
                    Cost = 1.0
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Object Two",
                    Age = 20,
                    RequestedAt = DateTimeOffset.UtcNow,
                    Cost = 2.0
                },
            },
            ReturnType.Invalid => "not a list",
            _ => throw new()
        });
    }
    #endregion
}
