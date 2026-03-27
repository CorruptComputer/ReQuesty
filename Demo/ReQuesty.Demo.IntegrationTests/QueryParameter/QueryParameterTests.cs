namespace ReQuesty.Demo.IntegrationTests.QueryParameter;

/// <summary>
///   Tests for the query parameter echo endpoints
/// </summary>
public class QueryParameterTests(ApiClientFixture fixture) : TestBase(fixture)
{
    /// <summary>
    ///   String query parameter should be echoed back
    /// </summary>
    [Fact]
    public async Task String_Echo()
    {
        string value = Guid.NewGuid().ToString();

        Task<string?> task = ApiClient.QueryParameter.String.GetAsync(options =>
        {
            options.QueryParameters.Value = value;
        });

        task.ShouldNotThrow();
        string? result = await task;
        result.ShouldBe(value);
    }

    /// <summary>
    ///   Integer query parameter should be echoed back
    /// </summary>
    [Fact]
    public async Task Integer_Echo()
    {
        int value = Random.Shared.Next(1, 10000);

        Task<int?> task = ApiClient.QueryParameter.Integer.GetAsync(options =>
        {
            options.QueryParameters.Value = value;
        });

        task.ShouldNotThrow();
        int? result = await task;
        result.ShouldBe(value);
    }

    /// <summary>
    ///   Guid query parameter should be echoed back
    /// </summary>
    [Fact]
    public async Task Guid_Echo()
    {
        Guid value = Guid.NewGuid();

        Task<Guid?> task = ApiClient.QueryParameter.Guid.GetAsync(options =>
        {
            options.QueryParameters.Value = value;
        });

        task.ShouldNotThrow();
        Guid? result = await task;
        result.ShouldBe(value);
    }

    /// <summary>
    ///   Bool query parameter should be echoed back
    /// </summary>
    [Fact]
    public async Task Bool_Echo()
    {
        Task<bool?> task = ApiClient.QueryParameter.Bool.GetAsync(options =>
        {
            options.QueryParameters.Value = true;
        });

        task.ShouldNotThrow();
        bool? result = await task;
        result.ShouldBe(true);
    }

    /// <summary>
    ///   Filter should return only objects within the specified age range
    /// </summary>
    [Fact]
    public async Task Filter_AgeRange()
    {
        Task<List<SomeObject>?> task = ApiClient.QueryParameter.Filter.GetAsync(options =>
        {
            options.QueryParameters.MinAge = 18;
            options.QueryParameters.MaxAge = 50;
        });

        task.ShouldNotThrow();
        List<SomeObject>? result = await task;
        result.ShouldNotBeNull();
        result.ShouldAllBe(o => o.Age >= 18 && o.Age <= 50);
    }
}
