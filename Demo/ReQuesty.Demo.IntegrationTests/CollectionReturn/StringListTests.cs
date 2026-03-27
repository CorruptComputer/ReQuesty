namespace ReQuesty.Demo.IntegrationTests.CollectionReturn;

/// <summary>
///   Tests for the string list endpoints
/// </summary>
public class StringListTests(ApiClientFixture fixture) : TestBase(fixture)
{
    /// <summary>
    ///   Null string list should not throw and return null
    /// </summary>
    [Fact]
    public async Task StringList_Null()
    {
        Task<List<string>?> task = ApiClient.CollectionReturn.Strings.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
        });

        task.ShouldNotThrow();
        List<string>? result = await task;
        result.ShouldBeNull();
    }

    /// <summary>
    ///   Valid string list should not throw and return a populated list
    /// </summary>
    [Fact]
    public async Task StringList_Random()
    {
        Task<List<string>?> task = ApiClient.CollectionReturn.Strings.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Random;
        });

        task.ShouldNotThrow();
        List<string>? result = await task;
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
    }

    /// <summary>
    ///   Null nullable string list should not throw and return null
    /// </summary>
    [Fact]
    public async Task NullableStringList_Null()
    {
        Task<List<string>?> task = ApiClient.CollectionReturn.Strings.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
        });

        task.ShouldNotThrow();
        List<string>? result = await task;
        result.ShouldBeNull();
    }

    /// <summary>
    ///   Valid nullable string list should not throw and return a populated list
    /// </summary>
    [Fact]
    public async Task NullableStringList_Random()
    {
        Task<List<string>?> task = ApiClient.CollectionReturn.Strings.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Random;
        });

        task.ShouldNotThrow();
        List<string>? result = await task;
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
    }
}
