using Microsoft.AspNetCore.Mvc;
using ReQuesty.Demo.Api.Controllers.Base;

namespace ReQuesty.Demo.Api.Controllers;

/// <summary>
///   A controller to test the behavior of various HTTP status codes
/// </summary>
public class StatusCodeController : DemoControllerBase
{
    /// <summary>
    ///   Returns 200 OK.
    /// </summary>
    /// <returns></returns>
    [HttpGet("ok", Name = "GetOk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult> GetOkAsync()
    {
        return Ok();
    }

    /// <summary>
    ///   Returns 201 Created.
    /// </summary>
    /// <returns></returns>
    [HttpGet("created", Name = "GetCreated")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async ValueTask<ActionResult> GetCreatedAsync()
    {
        return Created(string.Empty, null);
    }

    /// <summary>
    ///   Returns 204 No Content.
    /// </summary>
    /// <returns></returns>
    [HttpGet("nocontent", Name = "GetNoContent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async ValueTask<ActionResult> GetNoContentAsync()
    {
        return NoContent();
    }

    /// <summary>
    ///   Returns 400 Bad Request.
    /// </summary>
    /// <returns></returns>
    [HttpGet("badrequest", Name = "GetBadRequest")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<ActionResult> GetBadRequestAsync()
    {
        return BadRequest();
    }

    /// <summary>
    ///   Returns 404 Not Found.
    /// </summary>
    /// <returns></returns>
    [HttpGet("notfound", Name = "GetNotFound")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult> GetNotFoundAsync()
    {
        return NotFound();
    }

    /// <summary>
    ///   Returns 500 Internal Server Error.
    /// </summary>
    /// <returns></returns>
    [HttpGet("internalerror", Name = "GetInternalError")]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async ValueTask<ActionResult> GetInternalErrorAsync()
    {
        return StatusCode(StatusCodes.Status500InternalServerError);
    }
}
