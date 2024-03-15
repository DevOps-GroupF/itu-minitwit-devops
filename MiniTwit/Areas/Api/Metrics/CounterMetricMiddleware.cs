using Prometheus;
using MiniTwit.Areas.Api.Metrics;

namespace MiniTwit.Areas.Api.Metrics
{

    public class CounterMetricMiddleware
    {
        private readonly RequestDelegate _next;
        public CounterMetricMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task Invoke(HttpContext context)
        {
            // Increment RequestsInFlight at the start of the request
            Metrics.RequestsCounter.Inc();

            // Call the next middleware in the pipeline
            await _next(context);
        }
    }
}