using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Yaush.GhostUser.Url;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Yaush.GhostUser;

public class Function
{
    public Function()
    {
        ConfigureServiceCollection();
    }

    public async Task<string> FunctionHandler(string input, ILambdaContext context)
    {
        context.Logger.LogInformation("ghost user created shortened url for {host}", input);
        using ServiceProvider serviceProvider = _serviceCollection.BuildServiceProvider();
        var createShortenedUrlResponse = await serviceProvider.GetService<App>()!.Run(input);
        return createShortenedUrlResponse.ShortenedUrl;
    }

    private ServiceCollection _serviceCollection = null!;

    private void ConfigureServiceCollection()
    {
        _serviceCollection = new ServiceCollection();
        _serviceCollection.AddScoped<App>();
        _serviceCollection.AddScoped<IUrlCreatorService, UrlCreatorService>();

        _serviceCollection.AddHttpClient<IUrlCreatorService, UrlCreatorService>(client =>
        {
            client.BaseAddress = new Uri("https://bluekappa.com");
        });

        _serviceCollection.AddLogging(builder => builder.AddJsonConsole(c => c.IncludeScopes = true));

        //_serviceCollection.AddLogging(builder => builder.ClearProviders().AddLambdaLogger(loggerOptions));
    }
}
