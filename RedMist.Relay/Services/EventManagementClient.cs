using BigMission.Shared.Auth;
using Microsoft.Extensions.Configuration;
using RedMist.TimingCommon.Models.Configuration;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedMist.Relay.Services;

public class EventManagementClient
{
    private readonly RestClient restClient;

    public EventManagementClient(IConfiguration configuration)
    {
        var url = configuration["Server:Url"] ?? throw new InvalidOperationException("Server URL is not configured.");
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

    public virtual async Task<List<EventSummary>> LoadEventSummariesAsync()
    {
        var request = new RestRequest("LoadEventSummaries", Method.Get);
        return await restClient.GetAsync<List<EventSummary>>(request) ?? [];
    }

    public virtual async Task<Event?> LoadEventAsync(int eventId)
    {
        var request = new RestRequest("LoadEvent", Method.Get);
        request.AddQueryParameter("eventId", eventId);
        return await restClient.GetAsync<Event>(request);
    }

    public async Task SaveEventAsync(Event eventData)
    {
        var request = new RestRequest("UpdateEvent", Method.Post);
        request.AddJsonBody(eventData);
        await restClient.PostAsync(request);
    }

    public async Task UpdateEventStatusActiveAsync(int eventId)
    {
        var request = new RestRequest("UpdateEventStatusActive", Method.Put);
        request.AddQueryParameter("eventId", eventId);
        await restClient.PutAsync(request);
    }
}
