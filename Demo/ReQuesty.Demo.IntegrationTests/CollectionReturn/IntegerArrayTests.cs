namespace ReQuesty.Demo.IntegrationTests.CollectionReturn;

/// <summary>
///   Tests for the integer array endpoints
/// </summary>
public class IntegerArrayTests(ApiClientFixture fixture) : TestBase(fixture)
{
    /// <summary>
    ///   Null int array should not throw and return an empty list
    /// </summary>
    [Fact]
    public async Task IntegerArray_Null()
    {
        Task<List<int>?> task = ApiClient.CollectionReturn.Integers.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
        });

        task.ShouldNotThrow();
        List<int>? result = await task;
        result.ShouldBeNull();
    }

    /// <summary>
    ///   ReQuesty does not support deserializing collections of primitive types (List&lt;int&gt;).
    /// </summary>
    [Fact]
    public async Task IntegerArray_Random()
    {
        Task<List<int>?> task = ApiClient.CollectionReturn.Integers.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Random;
        });

        task.ShouldThrow<InvalidOperationException>();
    }

    /// <summary>
    ///   Null nullable int array should not throw and return null
    /// </summary>
    [Fact]
    public async Task NullableIntegerArray_Null()
    {
        Task<List<int>?> task = ApiClient.CollectionReturn.Integers.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Null;
        });

        task.ShouldNotThrow();
        List<int>? result = await task;
        result.ShouldBeNull();
    }

    /// <summary>
    ///   ReQuesty does not support deserializing collections of primitive types (List&lt;int&gt;).
    /// </summary>
    [Fact]
    public async Task NullableIntegerArray_Random()
    {
        Task<List<int>?> task = ApiClient.CollectionReturn.Integers.Nullable.GetAsync(options =>
        {
            options.QueryParameters.ReturnType = ReturnType.Random;
        });

        task.ShouldThrow<InvalidOperationException>();
    }
}
