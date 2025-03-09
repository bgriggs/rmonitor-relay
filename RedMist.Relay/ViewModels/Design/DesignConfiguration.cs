﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;

namespace RedMist.Relay.ViewModels.Design;

class DesignConfiguration : IConfiguration
{
    private readonly Dictionary<string, string?> config = new()
    {
        { "Server:OrganizationUrl", "https://localhost:5001" },
        { "Server:EventUrl", "https://localhost:5001" },
        { "Hub:Url", "https://localhost:5001/hub" },
        { "Keycloak:AuthServerUrl", "https://localhost:5001/auth" },
        { "Keycloak:Realm", "test" },
        { "Keycloak:ClientId", "1" },
        { "Keycloak:ClientSecret", "secret" },
        { "AesKey", "11111111111111111111111111111111" }
    };               

    public string? this[string key] { get => config[key]; set => throw new NotImplementedException(); }

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