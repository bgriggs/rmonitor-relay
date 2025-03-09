using BigMission.Shared.Auth;
using Microsoft.Extensions.Configuration;
using RedMist.TimingCommon.Models;
using RedMist.TimingCommon.Models.Configuration;
using RestSharp;
using System;
using System.Threading.Tasks;

namespace RedMist.Relay.Services;

public class OrganizationClient
{
    private readonly RestClient restClient;

    public OrganizationClient(IConfiguration configuration)
    {
        var url = configuration["Server:OrganizationUrl"] ?? throw new InvalidOperationException("Server URL is not configured.");
        var authUrl = configuration["Keycloak:AuthServerUrl"] ?? throw new InvalidOperationException("Keycloak URL is not configured.");
        var realm = configuration["Keycloak:Realm"] ?? throw new InvalidOperationException("Keycloak realm is not configured.");
        var clientId = configuration["Keycloak:ClientId"] ?? throw new InvalidOperationException("Keycloak client ID is not configured.");
        var clientSecret = configuration["Keycloak:ClientSecret"] ?? throw new InvalidOperationException("Keycloak client secret is not configured.");

        var options = new RestClientOptions(url)
        {
            Authenticator = new KeycloakServiceAuthenticator(string.Empty, authUrl, realm, clientId, clientSecret)
        };
        restClient = new RestClient(options);
    }

    public async Task<Organization?> LoadOrganizationAsync()
    {
        var request = new RestRequest("LoadOrganization", Method.Get);
        return await restClient.GetAsync<Organization>(request);
    }

    public async Task SaveOrganizationAsync(Organization organization)
    {
        var request = new RestRequest("UpdateOrganization", Method.Post);
        request.AddJsonBody(organization);
        await restClient.PostAsync(request);
    }

    public async Task<ControlLogStatistics> LoadControlLogStatisticsAsync(Organization organization)
    {
        var request = new RestRequest("GetControlLogStatistics", Method.Get);
        request.AddJsonBody(organization);
        return await restClient.PostAsync<ControlLogStatistics>(request) ?? new ControlLogStatistics();
    }
}
