using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Npgsql;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Yaush.UrlRedirectLambda;

public class Function
{

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        string redirectId = request.PathParameters["redirectId"];

        if (string.IsNullOrEmpty(redirectId))
        {
            context.Logger.LogLine($"No redirect id set");
            return new APIGatewayProxyResponse
            {
                StatusCode = 400,
                Body = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "message", "Invalid request" },
                }),
            };
        }

        context.Logger.LogLine($"Getting site for hash: {redirectId}");

        string? connString = GetConnectionString();
        if (string.IsNullOrEmpty(connString))
        {
            EnvironmentVariables environmentVariables = new(connString);
            context.Logger.LogError($"Failed to read environment variables: {environmentVariables}");
            return new APIGatewayProxyResponse
            {
                StatusCode = 500,
                Body = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "message", "Unexpected error" },
                }),
            };
        }

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connString);
        using var dataSource = dataSourceBuilder.Build();

        using var connection = await dataSource.OpenConnectionAsync();

        string? redirectUrl = string.Empty;
        try
        {
            await using var cmd = new NpgsqlCommand("SELECT url FROM links WHERE hash = $1;", connection);
            cmd.Parameters.Add(new() { Value = redirectId });
            redirectUrl = await cmd.ExecuteScalarAsync() as string;
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Error: {ex.Message}");
        }

        context.Logger.LogLine($"Redirect to site: {redirectUrl}");

        var response = new APIGatewayProxyResponse
        {
            StatusCode = 301,
            Headers = new Dictionary<string, string>()
        };
        response.Headers.Add("Location", redirectUrl);
        return response;
    }

    private record EnvironmentVariables(string? ConnectionString);


    private static string? GetConnectionString()
    {
        string? connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");
        return connectionString;
    }
}
