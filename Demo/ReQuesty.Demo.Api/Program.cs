using System.Text.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;

namespace ReQuesty.Demo.Api;

/// <summary>
///
/// </summary>
public static class Program
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="args"></param>
    public static async Task Main(string[] args)
    {
        await WebApplication.CreateBuilder(args).BuildBonesApi().RunBonesApiAsync();
    }

    private static WebApplication BuildBonesApi(this WebApplicationBuilder builder)
    {

        builder.WebHost.UseKestrel().ConfigureKestrel(kestrelServerOptions =>
        {
            kestrelServerOptions.AddServerHeader = false;
        });

        builder.Services.AddControllers().AddJsonOptions(configure =>
        {
            configure.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            configure.JsonSerializerOptions.WriteIndented = true;
            configure.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            configure.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            configure.JsonSerializerOptions.AllowTrailingCommas = true;
            configure.JsonSerializerOptions.RespectNullableAnnotations = true;
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi("main", options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
        });

        return builder.Build();
    }

    private static async Task RunBonesApiAsync(this WebApplication app)
    {
        app.UseCors(configurePolicy =>
        {
            configurePolicy
                .WithOrigins("*")
                .AllowAnyMethod()
                .AllowAnyHeader();
        });

        app.UseExceptionHandler(opt => { });

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseReDoc(c =>
            {
                c.DocumentTitle = "Demo API Documentation";
                c.SpecUrl = "/openapi/main.json";
            });
        }
        else
        {
            app.UseHttpsRedirection();
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        await app.RunAsync();
    }
}