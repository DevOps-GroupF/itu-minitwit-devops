using Microsoft.Playwright;

namespace MiniTwitUITests.playwrightstests;

public class Driver : IDisposable
{
    private readonly Task<IAPIRequestContext?>? _requestContext;

    public static readonly string BASEURL = Environment.GetEnvironmentVariable(
        "MINITWIT_SERVICE_URL"
    );
    public static readonly string DB_HOST = Environment.GetEnvironmentVariable("MINITWIT_DB_HOST");
    public static readonly string DB_PORT = Environment.GetEnvironmentVariable("MINITWIT_DB_PORT");
    public static readonly string DB_USERNAME = Environment.GetEnvironmentVariable(
        "MINITWIT_DB_USERNAME"
    );
    public static readonly string DB_PASSWORD = Environment.GetEnvironmentVariable(
        "MINITWIT_DB_PASSWORD"
    );
    public static readonly string DB_NAME = Environment.GetEnvironmentVariable("MINITWIT_DB_NAME");

    public Driver()
    {
        _requestContext = CreateApiContext();
    }

    public IAPIRequestContext? ApiRequestContext => _requestContext?.GetAwaiter().GetResult();

    public void Dispose()
    {
        _requestContext?.Dispose();
    }

    private async Task<IAPIRequestContext?> CreateApiContext()
    {
        var playwright = await Playwright.CreateAsync();

        return await playwright.APIRequest.NewContextAsync(
            new APIRequestNewContextOptions
            {
                BaseURL = BASEURL + "/api/",
                IgnoreHTTPSErrors = true
            }
        );
    }

    public static string RandomString(int length)
    {
        Random random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(
            Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray()
        );
    }
}
