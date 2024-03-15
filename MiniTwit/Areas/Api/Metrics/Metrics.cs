using Prometheus;

namespace MiniTwit.Areas.Api.Metrics
{
    public class Metrics
    {
        public static readonly Counter RequestsCounter =
            Prometheus.Metrics.CreateCounter("minitwit_requests_total", "Number of requests received by the application.");
        internal static readonly Gauge RequestsInFlight =
            Prometheus.Metrics.CreateGauge("minitwit_requests_in_flight", "Number of requests currently being processed.");
        public static readonly Histogram RequestResponseTime = Prometheus.Metrics.CreateHistogram(
            "minitwit_response_time_seconds",
            "Response time of the application.",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10) // Define histogram buckets
            }
        );
    }
}