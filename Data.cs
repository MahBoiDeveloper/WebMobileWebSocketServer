using System.Text.Json.Serialization;

namespace WebMobileWebSocketServer;

public struct Data
{
    public Int32 pageViews { get; set; }
    public Int32 uniqueVisitors { get; set; }
    public Int32 avgSessionDuration { get; set; }

    public Data(Random rng)
    {
        pageViews = (Int32)(rng.NextInt64(10, Int32.MaxValue)) % 1000;
        uniqueVisitors = (Int32)(rng.NextInt64(10, Int32.MaxValue)) % 1000;
        avgSessionDuration = (Int32)(rng.NextInt64(10, Int32.MaxValue)) % 10000;
    }
}
