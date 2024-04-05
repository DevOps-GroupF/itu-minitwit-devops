using System.Text.Json;
using UIMiniTwitTests.APITests;

namespace UIMiniTwitTests.APITests;

public class Tests
{   

    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task TestSiteIsUpAndRunning2()
    {   
        var driver = new Driver();
        var response = await driver.ApiRequestContext?.GetAsync("latest",
            new APIRequestContextOptions
            {
                Headers = new Dictionary<string, string>
                {
                   // { "Authorization", $"Bearer {token}" }
                }
            })!;

        var data = await response.JsonAsync();
        var json = data.Value.ToString();
        Assert.IsTrue(json.Contains("latest"));

    }    
}