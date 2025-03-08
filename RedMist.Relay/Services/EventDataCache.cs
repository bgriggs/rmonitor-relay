using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedMist.Relay.Services;

public class EventDataCache
{
    private readonly Dictionary<string, string> a = [];
    private readonly Dictionary<string, string> comp = [];
    private string b = string.Empty;
    private readonly Dictionary<string, string> c = [];
    private readonly Dictionary<string, string> g = [];
    private readonly Dictionary<string, string> gStarting = [];
    private readonly Dictionary<string, string> h = [];
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public int EventNumber { get; set; }
    public string EventName { get; set; } = string.Empty;
    public event Action<(int eventId, string name)>? EventChanged;


    public async Task Update(string data)
    {
        await semaphore.WaitAsync();
        try
        {
            var parts = data.Split('\n');
            foreach (var p in parts)
            {
                var msgParts = Parse(p);
                var cmd = msgParts[0];

                // https://github.com/bradfier/rmonitor/blob/master/docs/RMonitor%20Timing%20Protocol.pdf
                // A - Competitor information
                if (cmd == "$A" && msgParts.Length > 1)
                {
                    var reg = msgParts[1];
                    a[reg] = p;
                }
                // COMP - Competitor information
                else if (cmd == "$COMP" && msgParts.Length > 1)
                {
                    var reg = msgParts[1];
                    comp[reg] = p;
                }
                // B - Run information
                else if (cmd == "$B")
                {
                    b = p;
                    if (int.TryParse(msgParts[1], out int en))
                    {
                        EventNumber = en;
                    }
                    EventName = msgParts[2];
                    EventChanged?.Invoke((EventNumber, EventName));
                }
                // C - Class information
                else if (cmd == "$C" && msgParts.Length > 1)
                {
                    var classId = msgParts[1];
                    c[classId] = p;
                }
                //// E - Setting information
                //else if (cmd == "$E")
                //{
                //}
                // G - Race information
                else if (cmd == "$G" && msgParts.Length > 2)
                {
                    var reg = msgParts[2];
                    g[reg] = p;
                    var t = msgParts[4].Replace("\"", "").Trim();
                    if (string.IsNullOrWhiteSpace(msgParts[3]) && t == "00:00:00.000")
                    {
                        gStarting[reg] = p;
                    }
                }
                // H - Practice/qualifying information
                else if (cmd == "$H" && msgParts.Length > 2)
                {
                    var reg = msgParts[2];
                    h[reg] = p;
                }
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task Clear()
    {
        await semaphore.WaitAsync();
        try
        {
            a.Clear();
            comp.Clear();
            b = string.Empty;
            c.Clear();
            g.Clear();
            gStarting.Clear();
            h.Clear();
            EventNumber = 0;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static string[] Parse(string p)
    {
        var data = p.Split(',');
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = data[i].Replace("\"", "").Trim();
        }
        return data;
    }

    public async Task<string> GetData()
    {
        await semaphore.WaitAsync();
        try
        {
            var sb = new StringBuilder();
            // Force a reset on the server
            sb.AppendLine("$I, \"00:00:00\", \"0/0/0000\"");
            foreach (var a in a.Values)
            {
                sb.AppendLine(a);
            }
            foreach (var c in comp.Values)
            {
                sb.AppendLine(c);
            }
            if (!string.IsNullOrEmpty(b))
            {
                sb.AppendLine(b);
            }
            foreach (var c in c.Values)
            {
                sb.AppendLine(c);
            }
            foreach (var gs in gStarting.Values)
            {
                sb.AppendLine(gs);
            }
            foreach (var g in g.Values)
            {
                sb.AppendLine(g);
            }
            foreach (var h in h.Values)
            {
                sb.AppendLine(h);
            }
            return sb.ToString();
        }
        finally
        {
            semaphore.Release();
        }
    }
}
