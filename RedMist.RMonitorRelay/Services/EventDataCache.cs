using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedMist.RMonitorRelay.Services;

public class EventDataCache
{
    private Dictionary<string, string> a = [];
    private Dictionary<string, string> comp = [];
    private string b = string.Empty;
    private Dictionary<string, string> c = [];
    private Dictionary<string, string> g = [];
    private Dictionary<string, string> h = [];
    private SemaphoreSlim semaphore = new(1, 1);

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
            h.Clear();
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

    public async Task<string[]> GetData()
    {
        await semaphore.WaitAsync();
        try
        {
            var data = new List<string>();
            foreach (var a in a.Values)
            {
                data.Add(a);
            }
            foreach (var c in comp.Values)
            {
                data.Add(c);
            }
            if (!string.IsNullOrEmpty(b))
            {
                data.Add(b);
            }
            foreach (var c in c.Values)
            {
                data.Add(c);
            }
            foreach (var g in g.Values)
            {
                data.Add(g);
            }
            foreach (var h in h.Values)
            {
                data.Add(h);
            }
            return [.. data];
        }
        finally
        {
            semaphore.Release();
        }
    }
}
