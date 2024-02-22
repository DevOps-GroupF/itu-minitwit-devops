using Microsoft.AspNetCore.Mvc;

namespace MiniTwitAPI;

public class ApiKeyAuthMiddleware 
{   

    private readonly RequestDelegate _next; 
    private readonly IConfiguration _configuration; 

    public ApiKeyAuthMiddleware(RequestDelegate next, IConfiguration _config)
    {
        _next = next; 
        _configuration = _config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
       if(!context.Request.Headers.TryGetValue("Authorization", out var extractedApiKeyBase64Encoded))
       {
        await context.Response.WriteAsync("API key missing"); 
        return; 
       } 

       var ApiKey = _configuration.GetValue<string>("Authentication:Authorization");
       string de = extractedApiKeyBase64Encoded.ToString().Split(' ')[1]; // string format is "basic <token>", dont know why basic, so remove basic, and decode the token. 
       byte[] data = Convert.FromBase64String(de);
       var extractedApiKey = System.Text.Encoding.UTF8.GetString(data);
       if(!ApiKey.Equals(extractedApiKey))
       {
        context.Response.StatusCode = 401; 
        await context.Response.WriteAsync("Invalid Api Key");
       }

        await _next(context);
    }
}