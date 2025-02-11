using System.Globalization;

namespace RedMist.ReplayRMonitorData;

class EventData
{
    private readonly List<(DateTime ts, string data)> events = [];
    private int replayIndex = 0;
    public int Count => events.Count;

    public void Load(string file)
    {
        var raw = File.ReadAllText(file);
        var packets = raw.Split("##");

        foreach (var packet in packets)
        {
            var tsLineEnd = packet.IndexOf("\n");
            if (tsLineEnd < 0)
                continue;
            var tsLine = packet[..(tsLineEnd + 1)].Trim();
            var ts = DateTime.ParseExact(tsLine, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var data = packet[(tsLineEnd + 1)..];
            events.Add((ts, data));
        }
    }

    public string GetNextData()
    {
        if (replayIndex >= events.Count)
            return string.Empty;
        var data = events[replayIndex].data;
        replayIndex++;
        return data;
    }

    public TimeSpan GetNextTime()
    {
        if (replayIndex == 0)
            return TimeSpan.Zero;
        if (replayIndex >= events.Count)
            return TimeSpan.Zero;
        var last = events[replayIndex - 1].ts;
        var diff = events[replayIndex].ts - last;
        return diff;
    }

    public void Reset()
    {
        replayIndex = 0;
    }
}
