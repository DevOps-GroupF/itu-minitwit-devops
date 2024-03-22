using Prometheus;
using MiniTwit.Areas.Api.Metrics;

namespace MiniTwit.Areas.Api.Metrics
{

    public class RequestInFlightMiddleware
    {
        private readonly RequestDelegate _next;
        public RequestInFlightMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task Invoke(HttpContext context)
        {
            // Increment RequestsInFlight at the start of the request
            Metrics.RequestsInFlight.Inc();

            // Call the next middleware in the pipeline
            await _next(context);

            // Decrement RequestsInFlight at the end of the request
            Metrics.RequestsInFlight.Dec();
        }
    }
}