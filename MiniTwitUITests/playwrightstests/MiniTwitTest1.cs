using System;
using Microsoft.Playwright;
using Npgsql;

namespace MiniTwitUITests.playwrightstests;

public class Tests
{
    private static readonly string dbHost = Driver.DB_HOST;
    private static readonly string CS =
        $"Host={Driver.DB_HOST};Port={Driver.DB_PORT};Username={Driver.DB_USERNAME};Password={Driver.DB_PASSWORD};Database={Driver.DB_NAME};";

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
        await page.GotoAsync(Driver.BASEURL + "/Public");

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
        await page.GotoAsync(Driver.BASEURL + "/Public");

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
        await Page.GotoAsync(Driver.BASEURL + "/Public");

        // Perform the test
        await Page.ClickAsync("text='sign up'");
        string username = Driver.RandomString(8);
        
        Page = await PerformSignUp(Page, username);
        var isExist = await Page.GetByText("You were successfully registered and can login now").First.IsVisibleAsync();     
        var existsInDatabase = UserExistInDatabase(username);
        isExist = isExist && existsInDatabase; // Test the user also exists in the database. 

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
        await Page.GotoAsync(Driver.BASEURL + "/Public");

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

        await Page.GotoAsync(Driver.BASEURL + "/Register");
        Page = await PerformSignUp(Page, FollowingUsername);

        await Page.GotoAsync(Driver.BASEURL + "/Register");
        Page = await PerformSignUp(Page, FollowerUsername);

        await Page.GotoAsync(Driver.BASEURL + "/Login");

        Page = await PerformSignIn(Page, FollowerUsername);

        await Page.GotoAsync(Driver.BASEURL + "/" + FollowingUsername);

        await Page.ClickAsync("text='Follow user'");

        var isExist = await Page.GetByText("You are now following \""+FollowingUsername+"\"").First.IsVisibleAsync();  

        var followInDatabase = UserFollwerAnotherUser(FollowingUsername, FollowerUsername);

       Assert.True(isExist && followInDatabase); 
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

    private bool UserExistInDatabase(string username)
    {
        string sql = "SELECT username FROM \"user\" WHERE username = '"+username+"' limit 1;";
        var result = ConnectToDatabase(sql);
        return result != "";
    }   

    private bool UserFollwerAnotherUser(string followerUsername, string followingUsername)
    {
        var cs = CS;
        var con = new NpgsqlConnection(cs);
        con.Open();

        string sql = "SELECT who_id from follower where who_id IN (SELECT user_id FROM \"user\" WHERE username = '"+followingUsername+"' limit 1) AND whom_id IN (SELECT user_id FROM \"user\" WHERE username = '"+followerUsername+"' limit 1);";
        var cmd = new NpgsqlCommand(sql, con);

        NpgsqlDataReader rdr = cmd.ExecuteReader();

        rdr.Read();
        int result;
        try 
        {
            result = rdr.GetInt32(0);
        
        } catch (Exception ex)
        {
            result = -1;
            Console.WriteLine(ex);
        }

        con.Close();
        return result != -1;

    }   
    


    private string ConnectToDatabase(string sql) 
    {
        var cs = CS;
        var con = new NpgsqlConnection(cs);
        con.Open();

        var cmd = new NpgsqlCommand(sql, con);

        NpgsqlDataReader rdr = cmd.ExecuteReader();

        rdr.Read();
        string result;
        try
        {
            result = rdr.GetString(0);
        } catch 
        {
           result = "";
        }

        con.Close();
        return result;
    }
    



}

