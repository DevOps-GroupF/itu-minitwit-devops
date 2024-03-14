using Prometheus;
using MiniTwit.Areas.Api.Metrics;
using System.Diagnostics;

namespace MiniTwit.Areas.Api.Metrics
{

    public class ResponseTimeMiddleware
    {
        private readonly RequestDelegate _next;
        public ResponseTimeMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task Invoke(HttpContext context)
        {
            // Increment RequestsInFlight at the start of the request
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                // Measure response time and record it in a histogram
                Metrics.RequestResponseTime.Observe(stopwatch.Elapsed.TotalMilliseconds);
            }
        }
    }
}