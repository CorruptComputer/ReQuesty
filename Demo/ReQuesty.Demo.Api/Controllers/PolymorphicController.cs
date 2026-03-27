using Microsoft.AspNetCore.Mvc;
using ReQuesty.Demo.Api.Controllers.Base;
using ReQuesty.Demo.Api.Models.Animals;

namespace ReQuesty.Demo.Api.Controllers;

/// <summary>
///   A controller to test the behavior of polymorphic request and response body types
/// </summary>
public class PolymorphicController : DemoControllerBase
{
    /// <summary>
    ///   Accepts an AnimalBase body and echoes it back, verifying polymorphic round-trip.
    /// </summary>
    /// <param name="body">The animal to echo.</param>
    /// <returns></returns>
    [HttpPost("animal", Name = "PostAnimal")]
    [ProducesResponseType<AnimalBase>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<AnimalBase>> PostAnimalAsync([FromBody] AnimalBase body)
    {
        return body;
    }

    /// <summary>
    ///   Accepts a List&lt;AnimalBase&gt; and echoes it back, verifying polymorphic round-trip.
    /// </summary>
    /// <param name="body">The animal to echo.</param>
    /// <returns></returns>
    [HttpPost("animal/bulk", Name = "PostAnimalList")]
    [ProducesResponseType<List<AnimalBase>>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<List<AnimalBase>>> PostAnimalListAsync([FromBody] List<AnimalBase> body)
    {
        return body;
    }

    /// <summary>
    ///   Accepts a Dog body and echoes it back.
    /// </summary>
    /// <param name="body">The dog to echo.</param>
    /// <returns></returns>
    [HttpPost("dog", Name = "PostDog")]
    [ProducesResponseType<Dog>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<Dog>> PostDogAsync([FromBody] Dog body)
    {
        return body;
    }

    /// <summary>
    ///   Accepts a Dog body and echoes it back.
    /// </summary>
    /// <param name="body">The dog to echo.</param>
    /// <returns></returns>
    [HttpPost("dog/bulk", Name = "PostDogList")]
    [ProducesResponseType<List<Dog>>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<List<Dog>>> PostDogListAsync([FromBody] List<Dog> body)
    {
        return Ok(body);
    }

    /// <summary>
    ///   Accepts a Dog body and echoes it back, as an AnimalBase.
    /// </summary>
    /// <param name="body">The dog to echo.</param>
    /// <returns></returns>
    [HttpPost("dog-animal", Name = "PostDogGetAnimal")]
    [ProducesResponseType<AnimalBase>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<AnimalBase>> PostDogGetAnimalAsync([FromBody] Dog body)
    {
        return body;
    }

    /// <summary>
    ///   Accepts a Dog body and echoes it back, as an AnimalBase.
    /// </summary>
    /// <param name="body">The dog to echo.</param>
    /// <returns></returns>
    [HttpPost("dog-animal/bulk", Name = "PostDogListGetAnimalList")]
    [ProducesResponseType<List<AnimalBase>>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<List<AnimalBase>>> PostDogListGetAnimalListAsync([FromBody] List<Dog> body)
    {
        return Ok(body);
    }
}
