namespace UIMiniTwitTests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task TestSiteIsUpAndRunning()
    {   
        //Playwright
        using var playwright = await Playwright.CreateAsync();
        //Browser
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false
        });
        //Page
        var page = await browser.NewPageAsync();
        await page.GotoAsync("http://localhost:8080/Public");
      
        var isExist = await page.Locator("text='MiniTwit'").IsVisibleAsync();
        Assert.IsTrue(isExist);
        Assert.Pass();
    }

    
}