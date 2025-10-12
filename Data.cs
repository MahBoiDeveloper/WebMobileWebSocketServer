using System.Text.Json.Serialization;

namespace WebMobileWebSocketServer;

public struct Data
{
    public Int32 pageViews { get; set; }
    public Int32 uniqueVisitors { get; set; }
    public Int32 avgSessionDuration { get; set; }
    public List<Int32> pageViewsPerDay { get; set; }

    public Data(Random rng)
    {
        uniqueVisitors = (Int32)(rng.NextInt64(10, Int32.MaxValue)) % 1000;
        pageViews = uniqueVisitors + (Int32)(rng.NextInt64(10, Int32.MaxValue)) % 300;
        avgSessionDuration = (Int32)(rng.NextInt64(100, Int32.MaxValue)) % 10000;
        pageViewsPerDay = [ 0, 0, 0, 0, 0, 0, 0 ];
        pageViewsPerDay[0] = pageViews;
        for (int i = 1; i < 7; i++)
        {
            pageViewsPerDay[i] = pageViewsPerDay[i - 1] + (Int32)(rng.NextInt64(10, Int32.MaxValue)) % 100;
        }
    }
}
