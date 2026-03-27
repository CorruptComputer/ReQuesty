namespace ReQuesty.Demo.IntegrationTests.PrimativeReturn;

/// <summary>
///   Tests for the decimal endpoints.
///   Note: OpenAPI maps decimal to number/double, so the generated client returns double?.
/// </summary>
public class DecimalTests(ApiClientFixture fixture) : TestBase(fixture)
{
    /// <summary>
    ///   Null decimal should not throw
    /// </summary>
    [Fact]
    public async Task Decimal_Null()
    {
        Task<double?> task = ApiClient.PrimativeReturn.Decimal.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
        });

        task.ShouldNotThrow();
        double? result = await task;
        result.ShouldBe(0);
    }

    /// <summary>
    ///   Valid decimal should not throw
    /// </summary>
    [Fact]
    public async Task Decimal_Random()
    {
        Task<double?> task = ApiClient.PrimativeReturn.Decimal.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Random;
        });

        task.ShouldNotThrow();
        double? result = await task;
        result.ShouldNotBeNull();
    }

    /// <summary>
    ///   An invalid decimal should throw
    /// </summary>
    [Fact]
    public async Task Decimal_Invalid()
    {
        Task<double?> task = ApiClient.PrimativeReturn.Decimal.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Invalid;
        });

        task.ShouldThrow<NullReferenceException>();
    }

    /// <summary>
    ///   Null nullable decimal should not throw
    /// </summary>
    [Fact]
    public async Task NullableDecimal_Null()
    {
        Task<double?> task = ApiClient.PrimativeReturn.Decimal.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
        });

        task.ShouldNotThrow();
        double? result = await task;
        result.ShouldBe(0);
    }

    /// <summary>
    ///   Valid nullable decimal should not throw
    /// </summary>
    [Fact]
    public async Task NullableDecimal_Random()
    {
        Task<double?> task = ApiClient.PrimativeReturn.Decimal.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Random;
        });

        task.ShouldNotThrow();
        double? result = await task;
        result.ShouldNotBeNull();
    }

    /// <summary>
    ///   An invalid nullable decimal should throw
    /// </summary>
    [Fact]
    public async Task NullableDecimal_Invalid()
    {
        Task<double?> task = ApiClient.PrimativeReturn.Decimal.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Invalid;
        });

        task.ShouldThrow<NullReferenceException>();
    }
}
