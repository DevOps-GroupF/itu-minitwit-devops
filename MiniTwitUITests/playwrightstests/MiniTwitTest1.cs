using System;
using Microsoft.Playwright;

namespace MiniTwitUITests.playwrightstests;

public class Tests
{
    [Fact]
    public async Task TestSiteIsUpAndRunning()
    {
        // Playwright
        using var playwright = await Playwright.CreateAsync();

        //Browser
        await using var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions { Headless = true }
        );

        //Page
        var page = await browser.NewPageAsync();
        await page.GotoAsync("http://minitwit-service:8080/Public");

        var isExist = await page.Locator("text='MiniTwit'").IsVisibleAsync();
        Assert.True(isExist);
    }

    [Fact]
    public async Task TestSiteIsUpAndRunning2()
    {
        var driver = new Driver();
        var response = await driver.ApiRequestContext?.GetAsync(
            "latest",
            new APIRequestContextOptions
            {
                Headers = new Dictionary<string, string>
                {
                    // { "Authorization", $"Bearer {token}" }
                }
            }
        )!;

        var data = await response.JsonAsync();
        var json = data.Value.ToString();
        Assert.True(json.Contains("latest"));
    }
}

