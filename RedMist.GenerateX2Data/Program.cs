using RedMist.TimingCommon.Models.X2;
using System.Text.Json;

namespace RedMist.GenerateX2Data;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var loops = new List<Loop>
        {
            new() { Id = 1, Name = "S/F", IsOnline = true, HasActivity = true, IsSyncOk = true, HasDeviceWarnings = false, HasDeviceErrors = false },
            new() { Id = 2, Name = "Pit Enter", IsInPit = true, IsOnline = true, HasActivity = true, IsSyncOk = true, HasDeviceWarnings = false, HasDeviceErrors = false },
            new() { Id = 3, Name = "Pit S/F", IsInPit = true, IsOnline = true, HasActivity = true, IsSyncOk = true, HasDeviceWarnings = false, HasDeviceErrors = false },
            new() { Id = 4, Name = "Pit Exit", IsInPit = true, IsOnline = true, HasActivity = true, IsSyncOk = true, HasDeviceWarnings = false, HasDeviceErrors = false },
            new() { Id = 5, Name = "Sector 1", IsOnline = true, HasActivity = true, IsSyncOk = true, HasDeviceWarnings = false, HasDeviceErrors = false },
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(loops, options);
        File.WriteAllText("C:\\Code\\RedMist.RMonitorRelay\\RedMist.Relay\\Services\\X2Test\\Loops.json", json);

        Console.WriteLine("Loops.json written to disk.");

        // Passings
        var passings = new List<Passing>();
        var lastTime = DateTime.Now;
        var pitRemaining = Transponders.ToList();
        uint passingId = 1000;
        int runs = 0;
        for (int i = 0; i < 100; i++)
        {
            foreach (var tp in Transponders)
            {
                passings.Add(new Passing { Id = passingId, LoopId = 1, TransponderId = tp, TimestampLocal = lastTime, TimestampUtc = lastTime.ToUniversalTime(), Hits = 1, });
                lastTime = lastTime.AddSeconds(10);
                passingId++;

                if (pitRemaining.Contains(tp))
                {
                    passings.Add(new Passing { Id = passingId, LoopId = 2, TransponderId = tp, TimestampLocal = lastTime, TimestampUtc = lastTime.ToUniversalTime(), Hits = 1, IsInPit = true });
                    lastTime = lastTime.AddSeconds(25);
                    passingId++;

                    passings.Add(new Passing { Id = passingId, LoopId = 3, TransponderId = tp, TimestampLocal = lastTime, TimestampUtc = lastTime.ToUniversalTime(), Hits = 1, IsInPit = true });
                    lastTime = lastTime.AddSeconds(60);
                    passingId++;

                    passings.Add(new Passing { Id = passingId, LoopId = 4, TransponderId = tp, TimestampLocal = lastTime, TimestampUtc = lastTime.ToUniversalTime(), Hits = 1, IsInPit = false });
                    lastTime = lastTime.AddSeconds(20);
                    passingId++;

                    pitRemaining.Remove(tp);
                }

                passings.Add(new Passing { Id = passingId, LoopId = 5, TransponderId = tp, TimestampLocal = lastTime, TimestampUtc = lastTime.ToUniversalTime(), Hits = 1, });
                lastTime = lastTime.AddSeconds(10);
                passingId++;
                runs++;
            }
            lastTime = lastTime.AddSeconds(60);
        }

        var passingsJson = JsonSerializer.Serialize(passings, options);
        File.WriteAllText("C:\\Code\\RedMist.RMonitorRelay\\RedMist.Relay\\Services\\X2Test\\Passings.json", passingsJson);
        Console.WriteLine("Passings.json written to disk.");
    }

    readonly static uint[] Transponders =
    [
        14451114,
        10338369,
        12681555,
        1361474,
        12787092,
        15290308,
        58488,
        7031721,
        15884862,
        8827890,
        15575796,
        5419375,
        8040857,
        13157717,
        14534871,
        16453837,
        10908083,
        10143245,
        16522850,
        7362558,
        15294666,
        15485835,
        1069014,
        11650187,
        14039373,
        15312143,
        10533699,
        15609158,
        1426952,
        5482235,
        7634362,
        11685101,
        14450594,
        5135911,
        3096971,
        8806773,
        14249013,
        10229897,
        15990734,
        6680467,
        5176909,
        11092686,
        6336507,
        1911829,
        16535518,
        427305,
        1794203,
        3534820,
        11728986,
        3171391,
        197948,
        16765537,
        16240450,
        10766169,
        435976,
        14686985,
        14616635,
        13911143,
        315696,
    ];
}
