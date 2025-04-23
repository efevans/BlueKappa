using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using System.Text.Json;
using Npgsql;
using System.Security.Cryptography;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Yaush.UrlCreateLambda;

public class Function
{

    ///// <summary>
    ///// A simple function that takes a string and does a ToUpper
    ///// </summary>
    ///// <param name="input">The event for the Lambda function handler to process.</param>
    ///// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    ///// <returns></returns>
    //public string FunctionHandler(string input, ILambdaContext context)
    //{
    //    return input.ToUpper();
    //}

    private static readonly JsonSerializerOptions serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        CreateShortenedLinkRequest? createRequest;
        try
        {
            createRequest = JsonSerializer.Deserialize<CreateShortenedLinkRequest>(request.Body, serializerOptions);
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, ex.Message);
            return new APIGatewayProxyResponse
            {
                StatusCode = 400,
                Body = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "message", "Improperly formatted request" },
                }),
            };
        }

        if (string.IsNullOrEmpty(createRequest?.Url))
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = 400,
                Body = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "message", "Invalid request" },
                }),
            };
        }

        if (!Uri.IsWellFormedUriString(createRequest.Url, UriKind.Absolute))
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = 400,
                Body = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "message", "Invalid URL" },
                }),
            };
        }

        if (createRequest.Url.Length > 900)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = 400,
                Body = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "message", "URL is too long" },
                }),
            };
        }

        string? connString = GetConnectionString();
        string? redirectEndpoint = GetRedirectEndpoint();
        if (string.IsNullOrEmpty(connString) || string.IsNullOrEmpty(redirectEndpoint))
        {
            EnvironmentVariables environmentVariables = new(connString, redirectEndpoint);
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

        string hash = RandomNumberGenerator.GetHexString(16);

        await using (var cmd = new NpgsqlCommand("INSERT INTO links (url, hash) VALUES ($1, $2)", connection))
        {
            cmd.Parameters.Add(new() { Value = createRequest.Url });
            cmd.Parameters.Add(new() { Value = hash });
            await cmd.ExecuteNonQueryAsync();
        }

        var response = new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(new CreateShortenedLinkResponse()
            {
                ShortenedUrl = $"{redirectEndpoint}/{hash}"
            })
        };
        return response;
    }

    private record EnvironmentVariables(string? ConnectionString, string? RedirectEndpoint);


    private static string? GetConnectionString()
    {
        string? connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");
        return connectionString;
    }

    private static string? GetRedirectEndpoint()
    {
        string? endpoint = Environment.GetEnvironmentVariable("REDIRECT_ENDPOINT");
        return endpoint;
    }


    private class CreateShortenedLinkRequest
    {
        public required string Url { get; set; }
    }

    private class CreateShortenedLinkResponse
    {
        public required string ShortenedUrl { get; set; }
    }
}
