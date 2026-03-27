using Microsoft.AspNetCore.Mvc;
using ReQuesty.Demo.Api.Controllers.Base;
using ReQuesty.Demo.Api.Models;

namespace ReQuesty.Demo.Api.Controllers;

/// <summary>
///   A controller to test the behavior of request body parameters (POST, PUT, PATCH, DELETE)
/// </summary>
public class BodyParameterController : DemoControllerBase
{
    /// <summary>
    ///   Accepts a SomeObject body and returns it with 201 Created.
    /// </summary>
    /// <param name="body">The object to create.</param>
    /// <returns></returns>
    [HttpPost("object", Name = "PostObject")]
    [ProducesResponseType<SomeObject>(StatusCodes.Status201Created)]
    public async ValueTask<ActionResult<SomeObject>> PostObjectAsync([FromBody] SomeObject body)
    {
        return StatusCode(StatusCodes.Status201Created, body);
    }

    /// <summary>
    ///   Accepts a SomeObject body and returns it with 200 OK.
    /// </summary>
    /// <param name="id">The ID of the object to replace.</param>
    /// <param name="body">The replacement object.</param>
    /// <returns></returns>
    [HttpPut("object/{id:guid}", Name = "PutObject")]
    [ProducesResponseType<SomeObject>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<SomeObject>> PutObjectAsync([FromRoute] Guid id, [FromBody] SomeObject body)
    {
        return body with { Id = id };
    }

    /// <summary>
    ///   Accepts a SomeObject body and returns the merged result with 200 OK.
    /// </summary>
    /// <param name="id">The ID of the object to patch.</param>
    /// <param name="body">The partial object to merge.</param>
    /// <returns></returns>
    [HttpPatch("object/{id:guid}", Name = "PatchObject")]
    [ProducesResponseType<SomeObject>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<SomeObject>> PatchObjectAsync([FromRoute] Guid id, [FromBody] SomeObject body)
    {
        return body with { Id = id };
    }

    /// <summary>
    ///   Deletes an object by ID and returns 204 No Content.
    /// </summary>
    /// <param name="id">The ID of the object to delete.</param>
    /// <returns></returns>
    [HttpDelete("object/{id:guid}", Name = "DeleteObject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async ValueTask<ActionResult> DeleteObjectAsync([FromRoute] Guid id)
    {
        return NoContent();
    }
}
