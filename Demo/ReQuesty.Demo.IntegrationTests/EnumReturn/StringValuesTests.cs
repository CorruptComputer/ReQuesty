namespace ReQuesty.Demo.IntegrationTests.EnumReturn;

/// <summary>
///   Tests for the enum string values endpoints
/// </summary>
public class StringValuesTests : TestBase
{
    /// <summary>
    ///   Null string value should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task StringValues_Null()
    {
        await SetupApiClientAsync();

        StringValues? result = await ApiClient!.EnumReturn.String.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
        });

        result.ShouldBe(StringValues.North);
    }

    /// <summary>
    ///   Valid string value should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task StringValues_Random()
    {
        await SetupApiClientAsync();

        StringValues? result = await ApiClient!.EnumReturn.String.GetAsync(options =>
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
    public async Task StringValues_Invalid()
    {
        await SetupApiClientAsync();

        Task<StringValues?> result = ApiClient!.EnumReturn.String.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Invalid;
        });

        result.ShouldThrow<InvalidOperationException>();
    }

    /// <summary>
    ///   Null nullable string value should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task NullableString_Null()
    {
        await SetupApiClientAsync();

        StringValues? result = await ApiClient!.EnumReturn.String.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
        });

        result.ShouldBe(StringValues.North);
    }

    /// <summary>
    ///   Valid nullable string value should not throw
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task NullableString_Random()
    {
        await SetupApiClientAsync();

        StringValues? result = await ApiClient!.EnumReturn.String.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Random;
        });

        result.ShouldNotBeNull();
    }

    /// <summary>
    ///   An invalid nullable string value should return null instead of throwing
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task NullableString_Invalid()
    {
        await SetupApiClientAsync();

        Task<StringValues?> result = ApiClient!.EnumReturn.String.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Invalid;
        });

        result.ShouldThrow<InvalidOperationException>();
    }
}
