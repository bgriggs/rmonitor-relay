using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RedMist.Relay.Services;

public class WindowsSettingsProvider : ISettingsProvider
{
    private readonly Dictionary<string, string> userSettings = [];
    private readonly string userSettingsFile = "redmist-relay-settings.json";

    private readonly IConfiguration? appSettings;
    //private readonly string appSettingsFile = "appsettings.json";

    public WindowsSettingsProvider(IConfiguration appSettings)
    {
        var store = IsolatedStorageFile.GetUserStoreForAssembly();
        if (store.FileExists(userSettingsFile))
        {
            using IsolatedStorageFileStream isoStream = new(userSettingsFile, FileMode.Open, store);
            using StreamReader reader = new(isoStream);
            var json = reader.ReadToEnd();
            userSettings = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
        }

        //var dir = Directory.GetCurrentDirectory();
        //var appSettingsPath = Path.Combine(dir, appSettingsFile);
        //if (!string.IsNullOrEmpty(appSettingsPath) && File.Exists(appSettingsPath))
        //{
        //    var cb = new ConfigurationBuilder();
        //    cb.AddJsonFile(appSettingsPath);
        //    appSettings = cb.Build();
        //}
        this.appSettings = appSettings;
    }

    public string? GetUser(string key)
    {
        return userSettings.GetValueOrDefault(key);
    }

    public async Task SaveUser(string key, string value)
    {
        userSettings[key] = value;
        var json = JsonSerializer.Serialize(userSettings);
        var store = IsolatedStorageFile.GetUserStoreForAssembly();
        using IsolatedStorageFileStream isoStream = new(userSettingsFile, FileMode.Create, store);
        using StreamWriter writer = new(isoStream);
        await writer.WriteAsync(json);
    }

    public async Task DeleteUser(string key)
    {
        if (userSettings.Remove(key))
        {
            var json = JsonSerializer.Serialize(userSettings);
            var store = IsolatedStorageFile.GetUserStoreForAssembly();
            using IsolatedStorageFileStream isoStream = new(userSettingsFile, FileMode.Create, store);
            using StreamWriter writer = new(isoStream);
            await writer.WriteAsync(json);
        }
    }

    public string? GetApp(string key)
    {
        return appSettings?.GetValue<string>(key);
    }

    /// <summary>
    /// Uses user settings if available, otherwise uses app settings.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public string? GetWithOverride(string key)
    {
        var value = GetUser(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            value = GetApp(key);
        }
        return value;
    }


    public static string GetCsv(int[] values) => string.Join(",", values);
    public static int[] GetInts(string csv) => [.. csv.Split(',').Select(int.Parse)];
    public static string[] GetStrings(string csv) => [.. csv.Split(',')];
}
