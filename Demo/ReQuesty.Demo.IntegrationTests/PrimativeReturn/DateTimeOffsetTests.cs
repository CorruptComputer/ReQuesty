namespace ReQuesty.Demo.IntegrationTests.PrimativeReturn;

/// <summary>
///   Tests for the DateTimeOffset endpoints
/// </summary>
public class DateTimeOffsetTests(ApiClientFixture fixture) : TestBase(fixture)
{
    /// <summary>
    ///   Null DateTimeOffset should not throw
    /// </summary>
    [Fact]
    public async Task DateTimeOffset_Null()
    {
        Task<DateTimeOffset?> task = ApiClient.PrimativeReturn.Datetimeoffset.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
        });

        task.ShouldNotThrow();
        DateTimeOffset? result = await task;
        result.ShouldBe(default(DateTimeOffset));
    }

    /// <summary>
    ///   Valid DateTimeOffset should not throw
    /// </summary>
    [Fact]
    public async Task DateTimeOffset_Random()
    {
        Task<DateTimeOffset?> task = ApiClient.PrimativeReturn.Datetimeoffset.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Random;
        });

        task.ShouldNotThrow();
        DateTimeOffset? result = await task;
        result.ShouldNotBeNull();
    }

    /// <summary>
    ///   An invalid DateTimeOffset should throw
    /// </summary>
    [Fact]
    public async Task DateTimeOffset_Invalid()
    {
        Task<DateTimeOffset?> task = ApiClient.PrimativeReturn.Datetimeoffset.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Invalid;
        });

        task.ShouldThrow<Exception>();
    }

    /// <summary>
    ///   Null nullable DateTimeOffset should not throw
    /// </summary>
    [Fact]
    public async Task NullableDateTimeOffset_Null()
    {
        Task<DateTimeOffset?> task = ApiClient.PrimativeReturn.Datetimeoffset.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
        });

        task.ShouldNotThrow();
        DateTimeOffset? result = await task;
        result.ShouldBe(default(DateTimeOffset));
    }

    /// <summary>
    ///   Valid nullable DateTimeOffset should not throw
    /// </summary>
    [Fact]
    public async Task NullableDateTimeOffset_Random()
    {
        Task<DateTimeOffset?> task = ApiClient.PrimativeReturn.Datetimeoffset.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Random;
        });

        task.ShouldNotThrow();
        DateTimeOffset? result = await task;
        result.ShouldNotBeNull();
    }

    /// <summary>
    ///   An invalid nullable DateTimeOffset should throw
    /// </summary>
    [Fact]
    public async Task NullableDateTimeOffset_Invalid()
    {
        Task<DateTimeOffset?> task = ApiClient.PrimativeReturn.Datetimeoffset.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Invalid;
        });

        task.ShouldThrow<Exception>();
    }
}
