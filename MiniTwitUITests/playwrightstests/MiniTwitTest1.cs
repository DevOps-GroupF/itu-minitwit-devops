using System;
using Microsoft.Playwright;

namespace MiniTwitUITests.playwrightstests;

public class Tests 
{   
    private static string baseURL = Driver.BASEURL;

    public async Task<IPage> getPage()
    {
        // Playwright
        var playwright = await Playwright.CreateAsync();

        //Browser
        await using var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions { Headless = true }
        );

        //Page
        var page = await browser.NewPageAsync();
        await page.GotoAsync(baseURL+"/Public");
        
        return page;
    }

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
        await page.GotoAsync(baseURL+"/Public");
       

        var isExist = await page.Locator("text='MiniTwit'").IsVisibleAsync();
        Assert.True(isExist);
    }

     [Fact]
    public async Task TestUserCanSignUp()
    {   
        // Playwright
        using var playwright = await Playwright.CreateAsync();
        //Browser
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        //Page
        var Page = await browser.NewPageAsync();
        await Page.GotoAsync(baseURL+"/Public");
        
        // Perform the test
        await Page.ClickAsync("text='sign up'");
        string username = Driver.RandomString(8);
        
        Page = await PerformSignUp(Page, username);
        var isExist = await Page.GetByText("You were successfully registered and can login now").First.IsVisibleAsync();        
        Assert.True(isExist);
    }
    
    public async Task<IPage> PerformSignUp(IPage Page, string username)
    {   
        await Page.FillAsync("#Username", username);
        await Page.FillAsync("#Email", username+"@live.dk");
        await Page.FillAsync("#Password", "password");
        await Page.FillAsync("#Password2", "password");
        
        await Page.Locator("[type=submit]").ClickAsync();
        return Page;

    }

     [Fact]
    public async Task TestUserCanSignIn()
    {
        // Playwright
        using var playwright = await Playwright.CreateAsync();
        //Browser
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        //Page
        var Page = await browser.NewPageAsync();
        await Page.GotoAsync(baseURL+"/Public");

        // Perform the test
        await Page.ClickAsync("text='sign up'");
        string username = Driver.RandomString(8);

        Page = await PerformSignUp(Page, username);
        var isExist = await Page.GetByText("You were successfully registered and can login now").First.IsVisibleAsync();        
        
        if(!isExist) Assert.True(!isExist);
        
        Page = await PerformSignIn(Page, username);

        isExist = await Page.GetByText("You were logged in").First.IsVisibleAsync();        
        
        Assert.True(isExist);    
    }

     public async Task<IPage> PerformSignIn(IPage Page, string username)
    {   
        await Page.FillAsync("[name='username']", username);
        await Page.FillAsync("[name='password']", "password");
        await Page.Locator("[type=submit]").ClickAsync();
        return Page;
    }


    [Fact]
    public async Task TestUserCanFollowAnotherUser()
    {
        // Playwright
        using var playwright = await Playwright.CreateAsync();
        //Browser
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        //Page
        
        var Page = await browser.NewPageAsync();
        string FollowingUsername = Driver.RandomString(8);
        string FollowerUsername = Driver.RandomString(8);
        
        await Page.GotoAsync(baseURL+"/Register");
        Page = await PerformSignUp(Page, FollowingUsername);

        await Page.GotoAsync(baseURL+"/Register");
        Page = await PerformSignUp(Page, FollowerUsername);

        await Page.GotoAsync(baseURL+"/Login");

        Page = await PerformSignIn(Page, FollowerUsername);

        await Page.GotoAsync(baseURL+"/"+FollowingUsername);

        await Page.ClickAsync("text='Follow user'");

        var isExist = await Page.GetByText("You are now following \""+FollowingUsername+"\"").First.IsVisibleAsync();  

       Assert.True(isExist); 
    }



    [Fact]
    public async Task TestSiteAPIIsUpAndRunning2()
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
        bool containKey = json.Contains("latest"); 
        Assert.True(containKey);
    }
}

