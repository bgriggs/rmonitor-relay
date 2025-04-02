using RedMist.TimingCommon.Models;
using RedMist.TimingCommon.Models.X2;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedMist.Relay.Services;

/// <summary>
/// In-memory event data used when reconnecting to the cloud servers.
/// </summary>
public class EventDataCache
{
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private readonly Dictionary<string, string> a = [];
    private readonly Dictionary<string, string> comp = [];
    private string b = string.Empty;
    private readonly Dictionary<string, string> c = [];
    private readonly Dictionary<string, string> g = [];
    private readonly Dictionary<string, string> gStarting = [];
    private readonly Dictionary<string, string> h = [];

    /// <summary>
    /// ID from $B message.
    /// </summary>
    public int SessionNumber { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public event Action<(int sessionId, string name)>? SessionChanged;
    public event Action<(int sessionId, List<FlagDuration> flags)>? FlagsChanged;

    private readonly Dictionary<uint, Passing> passings = [];
    private readonly List<FlagDuration> flags = [];
    private Flags lastFlag = Flags.Unknown;

    private readonly Dictionary<string, CompetitorMetadata> competitors = [];


    public EventDataCache()
    {
        SessionChanged += (s) => passings.Clear();
        SessionChanged += (s) => flags.Clear();
    }


    public async Task UpdateRMonitor(string data)
    {
        await semaphore.WaitAsync();
        try
        {
            var parts = data.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                var msgParts = ParseRMonitor(p);
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
                    var lastSessionNum = SessionNumber;
                    if (int.TryParse(msgParts[1], out int en))
                    {
                        SessionNumber = en;
                    }
                    SessionName = msgParts[2];
                    if (SessionNumber != lastSessionNum)
                    {
                        SessionChanged?.Invoke((SessionNumber, SessionName));
                    }
                }
                // C - Class information
                else if (cmd == "$C" && msgParts.Length > 1)
                {
                    var classId = msgParts[1];
                    c[classId] = p;
                }
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
                // Heartbeat - $F,14,"00:12:45","13:34:23","00:09:47","Green "
                else if (cmd == "$F" && msgParts.Length > 2)
                {
                    if (msgParts.Length != 6)
                        continue;
                    var timeStr = msgParts[3].Replace("\"", "").Trim();
                    var flagStr = msgParts[5].Replace("\"", "").Trim();

                    if (DateTime.TryParseExact(timeStr, "HH:mm:ss", null, DateTimeStyles.None, out var tod))
                    {
                        UpdateFlags(flagStr.ToFlag(), tod);
                    }
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
            SessionNumber = 0;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static string[] ParseRMonitor(string p)
    {
        var data = p.Split(',');
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = data[i].Replace("\"", "").Trim();
        }
        return data;
    }

    public async Task<string> GetRMonitorData()
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

    #region Flags

    /// <summary>
    /// Check for changes to flag state.
    /// </summary>
    /// <param name="flag"></param>
    /// <param name="tod">Time of Day</param>
    private void UpdateFlags(Flags flag, DateTime tod)
    {
        if (flag == lastFlag)
        {
            return;
        }
        lastFlag = flag;

        var flagDuration = new FlagDuration
        {
            StartTime = tod,
            Flag = flag
        };

        var lastFlagDuration = flags.LastOrDefault();
        if (lastFlagDuration != null && lastFlagDuration.EndTime == null)
        {
            lastFlagDuration.EndTime = tod.AddSeconds(-1);
        }

        flags.Add(flagDuration);
        FlagsChanged?.Invoke((SessionNumber, flags.ToList()));
    }

    public async Task<List<FlagDuration>> GetFlagsAsync()
    {
        await semaphore.WaitAsync();
        try
        {
            return [.. flags];
        }
        finally
        {
            semaphore.Release();
        }
    }

    #endregion

    #region Passings

    public void UpdatePassings(List<Passing> passings)
    {
        lock (passings)
        {
            // Add passings to the cache
            foreach (var passing in passings)
            {
                this.passings[passing.Id] = passing;
            }
        }
    }

    public List<Passing> GetPassingsWithClear()
    {
        lock (passings)
        {
            var result = passings.Values.ToList();
            passings.Clear();
            return result;
        }
    }

    #endregion

    #region Competitor Metadata

    /// <summary>
    /// Updates and retrieves the list of competitor metadata that has changed.
    /// </summary>
    /// <returns>Returns a list of CompetitorMetadata that has changed.</returns>
    public List<CompetitorMetadata> UpdateCompetitorMetadataWithChanges(List<CompetitorMetadata> cms)
    {
        var changedCompetitors = new List<CompetitorMetadata>();
        lock (competitors)
        {
            foreach (var cm in cms)
            {
                if (competitors.TryGetValue(cm.CarNumber, out var last))
                {
                    if (cm.LastUpdated > last.LastUpdated)
                    {
                        competitors[cm.CarNumber] = cm;
                        changedCompetitors.Add(cm);
                    }
                }
                else
                {
                    competitors[cm.CarNumber] = cm;
                    changedCompetitors.Add(cm);
                }
            }
        }

        return changedCompetitors;
    }

    public List<CompetitorMetadata> GetCompetitorMetadata()
    {
        lock (competitors)
        {
            return [.. competitors.Values];
        }
    }

    #endregion
}
