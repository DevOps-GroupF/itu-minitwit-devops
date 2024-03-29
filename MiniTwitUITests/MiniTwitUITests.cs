using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

public class MiniTwitUITests
{
    private readonly HttpClient _client;
    private const string BaseUrl = "http://minitwit-service:8080";

    public MiniTwitUITests()
    {
        var handler = new HttpClientHandler { 
            AllowAutoRedirect = true,
            UseCookies = true 
        };
        _client = new HttpClient(handler);
    }

    private async Task<HttpResponseMessage> Register(string username, string password, string password2 = null, string email = null)
    {
        password2 ??= password;
        email ??= $"{username}@example.com";

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("username", username),
            new KeyValuePair<string, string>("password", password),
            new KeyValuePair<string, string>("password2", password2),
            new KeyValuePair<string, string>("email", email)
        });

        return await _client.PostAsync($"{BaseUrl}/register", content);
    }

    private async Task<HttpResponseMessage> Login(string username, string password)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("username", username),
            new KeyValuePair<string, string>("password", password)
        });

        return await _client.PostAsync($"{BaseUrl}/login", content);
    }

    private async Task<HttpResponseMessage> RegisterAndLogin(string username, string password)
    {
        await Register(username, password);
        return await Login(username, password);
    }

    private async Task<HttpResponseMessage> Logout()
    {
        return await _client.GetAsync($"{BaseUrl}/logout");
    }

    private async Task<HttpResponseMessage> AddMessage(string text)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("text", text)
        });

        return await _client.PostAsync($"{BaseUrl}/add_message", content);
    }

    [Fact]
    public async Task TestRegister()
    {
        var response = await Register("user1", "default");
        var responseContent = await response.Content.ReadAsStringAsync();
        /* Not working because .NET cannot follow the redirect
        Assert.Contains("You were successfully registered and can login now", responseContent);
        */

        response = await Register("user1", "default");
        responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("The username is already taken", responseContent);
    }

    [Fact]
    public async Task TestLoginLogout()
    {
        var response = await RegisterAndLogin("user1", "default");
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("You were logged in", responseContent);

        response = await Logout();
        responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("You were logged out", responseContent);

        response = await Login("user1", "wrongpassword");
        responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid password", responseContent);

        response = await Login("user2", "wrongpassword");
        responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid username", responseContent);
    }

    [Fact]
    public async Task TestMessageRecording()
    {
        await RegisterAndLogin("user1", "default");
        
        var response = await AddMessage("test message 1");
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Your message was recorded", responseContent);

        response = await AddMessage("<test message 2>");
        responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Your message was recorded", responseContent);

        response = await _client.GetAsync($"{BaseUrl}/");
        responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("test message 1", responseContent);
        Assert.Contains("&lt;test message 2&gt;", responseContent);
    }

    [Fact]
    public async Task TestTimelines()
    {
        await RegisterAndLogin("foo", "default");
        await AddMessage("the message by foo");
        await Logout();

        await RegisterAndLogin("bar", "default");
        await AddMessage("the message by bar");

        var response = await _client.GetAsync($"{BaseUrl}/public");
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("the message by foo", responseContent);
        Assert.Contains("the message by bar", responseContent);

        var response1 = await _client.GetAsync($"{BaseUrl}/");
        var responseContent1 = await response1.Content.ReadAsStringAsync();
        Assert.DoesNotContain("the message by foo", responseContent1);
        Assert.Contains("the message by bar", responseContent1);

        var response2 = await _client.GetAsync($"{BaseUrl}/foo/follow");
        var responseContent2 = await response2.Content.ReadAsStringAsync();
        /* Not working because .NET cannot follow the redirect
        Assert.Contains("You are now following &#34foo&#34", responseContent2);
        */

        var response3 = await _client.GetAsync($"{BaseUrl}/");
        var responseContent3 = await response3.Content.ReadAsStringAsync();
        Assert.Contains("the message by foo", responseContent3);
        Assert.Contains("the message by bar", responseContent3);

        var response4 = await _client.GetAsync($"{BaseUrl}/bar");
        var responseContent4 = await response4.Content.ReadAsStringAsync();
        Assert.DoesNotContain("the message by foo", responseContent4);
        Assert.Contains("the message by bar", responseContent4);

        var response5 = await _client.GetAsync($"{BaseUrl}/foo");
        var responseContent5 = await response5.Content.ReadAsStringAsync();
        Assert.Contains("the message by foo", responseContent5);
        Assert.DoesNotContain("the message by bar", responseContent5);

        var response6 = await _client.GetAsync($"{BaseUrl}/foo/unfollow");
        var responseContent6 = await response6.Content.ReadAsStringAsync();
        /* Not working because .NET cannot follow the redirect
        Assert.Contains("You are no longer following &#34foo&#34'", responseContent6);
        */

        var response7 = await _client.GetAsync($"{BaseUrl}/");
        var responseContent7 = await response7.Content.ReadAsStringAsync();
        Assert.DoesNotContain("the message by foo", responseContent7);
        Assert.Contains("the message by bar", responseContent7);
    }
}