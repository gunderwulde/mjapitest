using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Core;

namespace ExternalAuthModule;

public class ExternalAuthModule
{
    private readonly HttpClient _httpClient;
    public static ILogger<ExternalAuthModule> _logger;
    public ExternalAuthModule(ILogger<ExternalAuthModule> logger)
    {
        _httpClient = new HttpClient();
        _logger = logger;
    }

    [CloudCodeFunction("Version")]
    public async Task<string> Version(IExecutionContext context) {
        var version = "0.0.21";
        _logger?.LogInformation($"====================== Version {version} ======================");
        return version;
    }

    [CloudCodeFunction("AuthenticateWithExternalSystem")]
    public async Task<UnityAuthTokens> AuthenticateWithExternalSystem(IExecutionContext context, string userId, string externalToken, bool autoLink)
    {
        var statelessToken = await GetUnityStatelessToken(context);
        var ret = await AuthenticationCustomID(userId, context, statelessToken, true);
        // ret = await AuthenticationCustomID(userId, context, statelessToken, false);

        return ret;
    }
    private async Task<string> GetUnityStatelessToken(IExecutionContext context)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://services.api.unity.com/auth/v1/token-exchange?projectId={context.ProjectId}&environmentId={context.EnvironmentId}");
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"35d311a8-6f7b-48f3-89df-82cdb6e33286:Twl2voz540AZ11LliMm9gsP1JkxvNZ3s"));
        request.Headers.Add("Authorization", $"Basic {credentials}");
        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<TokenResponse>(content);
        return tokenData.Token;
    }
    private async Task<UnityAuthTokens> AuthenticationCustomID(string customId, IExecutionContext context, string statelessToken, bool signInOnly)
    {        
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://player-auth.services.api.unity.com/v1/projects/{context.ProjectId}/authentication/server/custom-id");
        request.Headers.Add("Authorization", $"Bearer {statelessToken}");
        request.Headers.Add("UnityEnvironment", context.EnvironmentName);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { externalId = customId, accessToken = signInOnly ? null: context.AccessToken, signInOnly = signInOnly }),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var tokens = JsonSerializer.Deserialize<UnityAuthTokens>(content);
        if (tokens == null) throw new InvalidOperationException("Failed to deserialize UnityAuthTokens from response.");
        return tokens;
    }
}
[System.Serializable]
public class UnityAuthTokens
{
    [JsonPropertyName("idToken")]
    public string IdToken { get; set; }
    [JsonPropertyName("sessionToken")]
    public string SessionToken { get; set; }
}
[System.Serializable]
public class TokenResponse
{
    [JsonPropertyName("accessToken")]
    public string Token { get; set; }
}

