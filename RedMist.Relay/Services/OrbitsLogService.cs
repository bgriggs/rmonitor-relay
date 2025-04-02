using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RedMist.Relay.Models;
using RedMist.TimingCommon.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RedMist.Relay.Services;

public class OrbitsLogService : IRecipient<OrbitsLogConnectionChanged>
{
    private readonly OrganizationConfigurationService configurationService;
    private ILogger Logger { get; }
    private bool isRunning;
    private readonly TimeSpan maxHistory;
    private readonly TimeSpan checkInterval;


    public OrbitsLogService(OrganizationConfigurationService configurationService, ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        this.configurationService = configurationService;
        Logger = loggerFactory.CreateLogger(GetType().Name);
        WeakReferenceMessenger.Default.RegisterAll(this);
        maxHistory = TimeSpan.FromDays(configuration.GetValue("OrbitsLogs:MaxHistoryDays", 7));
        checkInterval = TimeSpan.FromMinutes(configuration.GetValue("OrbitsLogs:CheckIntervalMinutes", 5));
    }


    public void Initialize() { }

    /// <summary>
    /// Notification of when the Orbits log path is known and is present on disk.
    /// </summary>
    /// <param name="message"></param>
    public void Receive(OrbitsLogConnectionChanged message)
    {
        var path = configurationService.OrganizationConfiguration?.Orbits.LogsPath;
        if (!isRunning && Directory.Exists(path))
        {
            isRunning = true;
            _ = Task.Run(CheckLogs);
        }
    }

    private async Task CheckLogs()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            while (true)
            {
                var competitorMetadata = new List<CompetitorMetadata>();
                var paths = GetValidLogFilePaths();
                foreach (var file in paths)
                {
                    try
                    {
                        using FileStream fileStream = new(file.path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        using StreamReader streamReader = new(fileStream);
                        var contents = await streamReader.ReadToEndAsync();

                        var cms = ParseForCompetitorMetadata(contents, file.date);
                        competitorMetadata.AddRange(cms);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error reading log file: {0}", file.path);
                    }
                }

                // Find the newest competitor metadata per car
                var latestCompetitors = competitorMetadata.GroupBy(c => c.CarNumber)
                    .Select(g => g.OrderByDescending(c => c.LastUpdated).First())
                    .ToList();

                WeakReferenceMessenger.Default.Send(new CompetitorMetadataUpdate(latestCompetitors));
                await Task.Delay(checkInterval);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking Orbits logs");
        }
        finally
        {
            isRunning = false;
        }
    }

    /// <summary>
    /// Retrieves a list of valid log file paths along with their corresponding dates from a specified directory. It
    /// filters log files based on their date and a maximum history limit.
    /// </summary>
    /// <returns>A list of tuples containing the file path and the date of each valid log file.</returns>
    private List<(string path, DateTime date)> GetValidLogFilePaths()
    {
        var filePaths = new List<(string, DateTime)>();
        var path = configurationService.OrganizationConfiguration?.Orbits.LogsPath;
        if (Directory.Exists(path))
        {
            var today = DateTime.Now.Date;
            var logFiles = Directory.GetFiles(path, "*.log");
            foreach (var f in logFiles)
            {
                var name = Path.GetFileNameWithoutExtension(f);
                if (DateTime.TryParseExact(name, "yyyy-MM-dd", null, DateTimeStyles.None, out var logDate))
                {
                    if (today - logDate <= maxHistory)
                    {
                        filePaths.Add((f, logDate));
                    }
                }
            }
        }

        return filePaths;
    }

    // 0            1   2       3       4           5                6                      7           8   9   10          11          12          13  14      15  16  17  18  19  
    // 13:13:56.280	xps	Server 	brian	datamanager	Competitor added [0x80000000]:c7f12c72	ebfeacdc	777	GP1	123123123	222222222	123123123	Bob	Smith	CO	BMM	BMW	PL	WRL	M3	Conti	asdf@sdf.com	100	4	0
    // 13:18:09.682	xps	Server 	brian	datamanager	Competitor modified [0x40000001]:e1c13b60	De1c13b60	1		1004	0	1004				100	4	0

    private List<CompetitorMetadata> ParseForCompetitorMetadata(string contents, DateTime date)
    {
        var competitors = new List<CompetitorMetadata>();
        var lines = contents.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            var parts = line.Split('\t', StringSplitOptions.TrimEntries);
            var action = parts[5];
            if (parts.Length > 22 && (action.StartsWith("Competitor added") || action.StartsWith("Competitor modified")))
            {
                _ = uint.TryParse(parts[11], out uint tActive);
                _ = uint.TryParse(parts[10], out uint t2);
                var time  = DateTime.ParseExact(parts[0], "HH:mm:ss.fff", CultureInfo.InvariantCulture);
                var dt = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, time.Millisecond);

                var competitor = new CompetitorMetadata
                {
                    LastUpdated = dt,
                    CarNumber = parts[7],
                    Class = parts[8],
                    Transponder = tActive,
                    Transponder2 = t2,
                    FirstName = parts[12],
                    LastName = parts[13],
                    NationState = parts[14],
                    Sponsor = parts[15],
                    Make = parts[16],
                    Hometown = parts[17],
                    Club = parts[18],
                    ModelEngine = parts[19],
                    Tires = parts[20],
                    Email = parts[21],
                };
                competitors.Add(competitor);
            }
        }
        return competitors;
    }
}
