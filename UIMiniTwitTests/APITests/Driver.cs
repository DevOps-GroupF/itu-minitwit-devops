using Microsoft.Playwright;

namespace UIMiniTwitTests.APITests;

public class Driver : IDisposable
{
    private readonly Task<IAPIRequestContext?>? _requestContext;

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

        return await playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            BaseURL = "http://localhost:8080/api/",
            IgnoreHTTPSErrors = true
        });
    }
}