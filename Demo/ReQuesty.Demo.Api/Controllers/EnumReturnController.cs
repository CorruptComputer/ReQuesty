using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using ReQuesty.Demo.Api.Controllers.Base;

namespace ReQuesty.Demo.Api.Controllers;

/// <summary>
///   A controller to test the behavior of enum return types
/// </summary>
public class EnumReturnController : DemoControllerBase
{
    #region IntegerValues enum
    /// <summary>
    ///   Example enum with its values as integers
    /// </summary>
    public enum IntegerValues
    {
        /// <summary>
        ///   Zero value
        /// </summary>
        Zero = 0,

        /// <summary>
        ///   One value
        /// </summary>
        One = 1,

        /// <summary>
        ///   Two value
        /// </summary>
        Two = 2
    }

    /// <summary>
    ///   Gets a IntegerValues enum based on the specified return type.
    /// </summary>
    /// <returns></returns>
    [HttpGet("integer", Name = "GetIntegerValuesEnum")]
    [ProducesResponseType<IntegerValues>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<IntegerValues>> GetIntegerValuesEnumAsync([FromQuery] ReturnType returnType)
    {
        return Ok(returnType switch
        {
            ReturnType.Null => null,
            ReturnType.Random => IntegerValues.One,
            ReturnType.Invalid => (IntegerValues)(-1),
            _ => throw new()
        });
    }

    /// <summary>
    ///   Gets a nullable IntegerValues enum based on the specified return type.
    /// </summary>
    /// <returns></returns>
    [HttpGet("integer/nullable", Name = "GetNullableIntegerValuesEnum")]
    [ProducesResponseType<IntegerValues?>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<IntegerValues?>> GetNullableIntegerValuesEnumAsync([FromQuery] ReturnType returnType)
    {
        return Ok(returnType switch
        {
            ReturnType.Null => null,
            ReturnType.Random => IntegerValues.One,
            ReturnType.Invalid => (IntegerValues)(-1),
            _ => throw new()
        });
    }
    #endregion

    #region StringValues enum
    /// <summary>
    ///   Example enum with its values as strings
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum StringValues
    {
        /// <summary>
        ///   North value
        /// </summary>
        North,

        /// <summary>
        ///   East value
        /// </summary>
        East,

        /// <summary>
        ///   South value
        /// </summary>
        South,

        /// <summary>
        ///   West value
        /// </summary>
        West
    }

    /// <summary>
    ///   Gets a StringValues enum based on the specified return type.
    /// </summary>
    /// <returns></returns>
    [HttpGet("string", Name = "GetStringValuesEnum")]
    [ProducesResponseType<StringValues>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<StringValues>> GetStringValuesEnumAsync([FromQuery] ReturnType returnType)
    {
        return Ok(returnType switch
        {
            ReturnType.Null => null,
            ReturnType.Random => StringValues.North,
            ReturnType.Invalid => (StringValues)(-1),
            _ => throw new()
        });
    }

    /// <summary>
    ///   Gets a nullable StringValues enum based on the specified return type.
    /// </summary>
    /// <returns></returns>
    [HttpGet("string/nullable", Name = "GetNullableStringValuesEnum")]
    [ProducesResponseType<StringValues?>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<StringValues?>> GetNullableStringValuesEnumAsync([FromQuery] ReturnType returnType)
    {
        return Ok(returnType switch
        {
            ReturnType.Null => null,
            ReturnType.Random => StringValues.North,
            ReturnType.Invalid => (StringValues)(-1),
            _ => throw new()
        });
    }
    #endregion
}
