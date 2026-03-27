namespace ReQuesty.Demo.IntegrationTests.Polymorphic;

/// <summary>
///   Tests for post and response of polymorphic types on endpoints.
/// </summary>
public class PostTests(ApiClientFixture fixture) : TestBase(fixture)
{
    /// <summary>
    ///   POSTing a GoldenRetriever as AnimalBase results in a server 500 because the
    ///   composed type wrapper does not emit the $type discriminator.
    /// </summary>
    [Fact]
    public async Task Post_GoldenRetriever()
    {
        AnimalBase body = new()
        {
            AnimalBaseGoldenRetriever = new AnimalBaseGoldenRetriever
            {
                Name = "Buddy",
                AgeYears = 3,
                IsTrained = true,
                FurColour = "Golden"
            }
        };

        Exception? ex = await Record.ExceptionAsync(() => ApiClient.Polymorphic.Animal.PostAsync(body));

        ex.ShouldNotBeNull();
    }

    /// <summary>
    ///   POSTing a Cat as AnimalBase results in a server 500 because the
    ///   composed type wrapper does not emit the $type discriminator.
    /// </summary>
    [Fact]
    public async Task PostAnimal_Cat()
    {
        AnimalBase body = new()
        {
            AnimalBaseCat = new AnimalBaseCat
            {
                Name = "Whiskers",
                AgeYears = 4,
                IsIndoor = true
            }
        };

        Exception? ex = await Record.ExceptionAsync(() => ApiClient.Polymorphic.Animal.PostAsync(body));

        ex.ShouldNotBeNull();
    }

    /// <summary>
    ///   POSTing a GoldenRetriever as Dog results in a server 500 because the
    ///   composed type wrapper does not emit the $type discriminator.
    /// </summary>
    [Fact]
    public async Task PostDog_GoldenRetriever()
    {
        Dog body = new()
        {
            DogGoldenRetriever = new DogGoldenRetriever
            {
                Name = "Buddy",
                AgeYears = 3,
                IsTrained = true,
                FurColour = "Golden"
            }
        };

        Exception? ex1 = await Record.ExceptionAsync(() => ApiClient.Polymorphic.Dog.PostAsync(body));

        ex1.ShouldNotBeNull();
    }

    /// <summary>
    ///   POSTing a Boxer as Dog results in a server 500 because the
    ///   composed type wrapper does not emit the $type discriminator.
    /// </summary>
    [Fact]
    public async Task PostDog_Boxer()
    {
        Dog body = new()
        {
            DogBoxer = new DogBoxer
            {
                Name = "Rocky",
                AgeYears = 5,
                IsTrained = false,
                IsBrindle = true
            }
        };

        Exception? ex = await Record.ExceptionAsync(() => ApiClient.Polymorphic.Dog.PostAsync(body));

        ex.ShouldNotBeNull();
    }
}
