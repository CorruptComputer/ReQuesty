using System.Net;
using System.Net.Mime;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace ReQuesty.Demo.Api.Controllers.Base;

/// <summary>
///   Base controller for the Demo API, everything should extend from this.
/// </summary>
[ApiController]
[Route("[controller]")]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public abstract class DemoControllerBase : ControllerBase
{
    /// <summary>
    ///   The type of data to return
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReturnType
    {
        /// <summary>
        ///   Return null
        /// </summary>
        Null,

        /// <summary>
        ///   Return random valid data for the type
        /// </summary>
        Random,

        /// <summary>
        ///   Return invalid data for the type
        /// </summary>
        Invalid
    }
}