using System;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace MiniTwitAPI.Controllers;

    [Route("[controller]")]
    [ApiController]
    public class LatestController : ControllerBase
    {   

        private readonly IMemoryCache _memoryCache;
        public string cacheKey = "latest";

        public LatestController(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }
        
        [HttpGet]
        public async Task<ActionResult<Dictionary<string, int>>> Get()
        {
            Dictionary<string, int> respose = new Dictionary<string, int>();
            string? latest; 


            if(!_memoryCache.TryGetValue(cacheKey, out latest))
            {  
                latest = "-1";
            }

            int int_latest = int.Parse(latest);

            respose.Add(cacheKey, int_latest);

            return respose;
        }
    }
