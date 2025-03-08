using System.Threading.Tasks;

namespace RedMist.Relay.Services;

public interface ISettingsProvider
{
    string? GetUser(string key);
    Task SaveUser(string key, string value);
    Task DeleteUser(string key);
    string? GetApp(string key);
    string? GetWithOverride(string key);
}
