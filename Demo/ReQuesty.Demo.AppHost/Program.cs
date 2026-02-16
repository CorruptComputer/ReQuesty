using System.Reflection;

namespace ReQuesty.Demo.AppHost;

/// <summary>
///   This app host is only used for local development and testing purposes, its not intended for production use.
/// </summary>
public static class Program
{
    /// <summary>
    ///   The name of the API project.
    /// </summary>
    public static string ApiProjectName = "DemoApi";

    /// <summary>
    ///   Entry point
    /// </summary>
    /// <param name="args"></param>
    public static async Task Main(string[] args)
    {
        await DistributedApplication.CreateBuilder(args).BuildBonesAppHost().RunBonesAppHostAsync();
    }

    private static DistributedApplication BuildBonesAppHost(this IDistributedApplicationBuilder builder)
    {
        builder.AddProject<Projects.ReQuesty_Demo_Api>(ApiProjectName);

        return builder.Build();
    }

    private static async Task RunBonesAppHostAsync(this DistributedApplication app)
    {
        await app.RunAsync();
    }
}