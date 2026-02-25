namespace ReQuesty.Demo.IntegrationTests.EnumReturn;

/// <summary>
///   Tests for the enum integer values endpoints
/// </summary>
public class IntegerValuesTests : TestBase
{
    /// <summary>
    ///   Null integer value should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task IntegerValues_Null()
    {
        await SetupApiClientAsync();

        int? result = await ApiClient!.EnumReturn.Integer.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
        });

        result.ShouldNotBeNull();
        result.ShouldBe(0);
    }

    /// <summary>
    ///   Valid integer value should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task IntegerValues_Random()
    {
        await SetupApiClientAsync();

        int? result = await ApiClient!.EnumReturn.Integer.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Random;
        });

        result.ShouldNotBeNull();
    }

    /// <summary>
    ///   An invalid integer value should return null instead of throwing
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task IntegerValues_Invalid()
    {
        await SetupApiClientAsync();

        int? result = await ApiClient!.EnumReturn.Integer.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Invalid;
        });

        result.ShouldBe(-1);
    }

    /// <summary>
    ///   Null nullable integer value should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task NullableInteger_Null()
    {
        await SetupApiClientAsync();

        int? result = await ApiClient!.EnumReturn.Integer.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
        });

        result.ShouldNotBeNull();
        result.ShouldBe(0);
    }

    /// <summary>
    ///   Valid nullable integer value should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task NullableInteger_Random()
    {
        await SetupApiClientAsync();

        int? result = await ApiClient!.EnumReturn.Integer.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Random;
        });

        result.ShouldNotBeNull();
    }

    /// <summary>
    ///   An invalid nullable integer value should return null instead of throwing
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task NullableInteger_Invalid()
    {
        await SetupApiClientAsync();

        int? result = await ApiClient!.EnumReturn.Integer.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Invalid;
        });

        result.ShouldBe(-1);
    }
}
