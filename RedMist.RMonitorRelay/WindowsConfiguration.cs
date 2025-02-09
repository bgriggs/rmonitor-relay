using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using RedMist.RMonitorRelay.Services;
using System;
using System.Collections.Generic;

namespace RedMist.RMonitorRelay;

public class WindowsConfiguration : IConfiguration
{
    private readonly ISettingsProvider settings;

    public WindowsConfiguration(ISettingsProvider settings)
    {
        this.settings = settings;
    }

    public string? this[string key] { get => settings.GetWithOverride(key); set => throw new NotImplementedException(); }

    public IEnumerable<IConfigurationSection> GetChildren()
    {
        throw new NotImplementedException();
    }

    public IChangeToken GetReloadToken()
    {
        throw new NotImplementedException();
    }

    public IConfigurationSection GetSection(string key)
    {
        throw new NotImplementedException();
    }
}
