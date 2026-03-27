namespace ReQuesty.Demo.IntegrationTests.EnumReturn;

/// <summary>
///   Tests for the SomeObject endpoints
/// </summary>
public class SomeObjectTests(ApiClientFixture fixture) : TestBase(fixture)
{
    /// <summary>
    ///   Null string value should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task SomeObject_Null()
    {
        SomeObject? result = await ApiClient.ObjectReturn.SomeObject.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
        });

        result.ShouldBeNull();
    }

    /// <summary>
    ///   Valid string value should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task SomeObject_Random()
    {
        SomeObject? result = await ApiClient.ObjectReturn.SomeObject.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Random;
        });

        result.ShouldNotBeNull();
    }

    /// <summary>
    ///   An invalid string value should return null instead of throwing
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task SomeObject_Invalid()
    {
        SomeObject? result = await ApiClient.ObjectReturn.SomeObject.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Invalid;
        });

        result.ShouldNotBeNull();
    }
}
