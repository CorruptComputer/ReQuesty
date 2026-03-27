namespace ReQuesty.Demo.IntegrationTests.CollectionReturn;

/// <summary>
///   Tests for the object list endpoints
/// </summary>
public class ObjectListTests(ApiClientFixture fixture) : TestBase(fixture)
{
    /// <summary>
    ///   Null object list should not throw and return null
    /// </summary>
    [Fact]
    public async Task ObjectList_Null()
    {
        Task<List<SomeObject>?> task = ApiClient.CollectionReturn.Objects.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
        });

        task.ShouldNotThrow();
        List<SomeObject>? result = await task;
        result.ShouldBeNull();
    }

    /// <summary>
    ///   Valid object list should not throw and return a populated list
    /// </summary>
    [Fact]
    public async Task ObjectList_Random()
    {
        Task<List<SomeObject>?> task = ApiClient.CollectionReturn.Objects.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Random;
        });

        task.ShouldNotThrow();
        List<SomeObject>? result = await task;
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        result.ShouldAllBe(o => o != null);
    }

    /// <summary>
    ///   Null nullable object list should not throw and return null
    /// </summary>
    [Fact]
    public async Task NullableObjectList_Null()
    {
        Task<List<SomeObject>?> task = ApiClient.CollectionReturn.Objects.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
        });

        task.ShouldNotThrow();
        List<SomeObject>? result = await task;
        result.ShouldBeNull();
    }

    /// <summary>
    ///   Valid nullable object list should not throw and return a populated list
    /// </summary>
    [Fact]
    public async Task NullableObjectList_Random()
    {
        Task<List<SomeObject>?> task = ApiClient.CollectionReturn.Objects.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Random;
        });

        task.ShouldNotThrow();
        List<SomeObject>? result = await task;
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        result.ShouldAllBe(o => o != null);
    }
}
