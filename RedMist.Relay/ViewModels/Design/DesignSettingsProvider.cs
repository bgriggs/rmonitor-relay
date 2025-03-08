using RedMist.Relay.Services;
using System.Threading.Tasks;

namespace RedMist.Relay.ViewModels.Design;

internal class DesignSettingsProvider : ISettingsProvider
{
    public Task DeleteUser(string key)
    {
        return Task.CompletedTask;
    }

    public string? GetApp(string key)
    {
        return string.Empty;
    }

    public string? GetUser(string key)
    {
        return string.Empty;
    }

    public string? GetWithOverride(string key)
    {
        return string.Empty;
    }

    public Task SaveUser(string key, string value)
    {
        return Task.CompletedTask;
    }
}
