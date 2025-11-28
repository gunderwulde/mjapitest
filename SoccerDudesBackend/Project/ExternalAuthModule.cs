using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Core;

namespace ExternalAuthModule;

public class ExternalAuthModule
{
    private readonly HttpClient _httpClient;
    public static ILogger<ExternalAuthModule> _logger;
    private string version = "0.1.0";
    private string keyID = "35d311a8-6f7b-48f3-89df-82cdb6e33286";
    private string keySecret = "Twl2voz540AZ11LliMm9gsP1JkxvNZ3s";
    public ExternalAuthModule(ILogger<ExternalAuthModule> logger) {
        _httpClient = new HttpClient();
        _logger = logger;
    }

    /// <summary>
    /// Cloud Code function that returns the current module version.
    /// </summary>
    [CloudCodeFunction("Version")]
    public async Task<string> Version(IExecutionContext context) {
        return version;
    }
    /// <summary>
    /// Log to console with module version prefix.
    /// </summary>
    /// <param name="message"></param>
    private void Log(string message) {
        _logger.LogInformation($"[{version}] {message}");
    }
    /// <summary>
    /// Authenticate a user with an external system.
    /// Flow:
    /// 1) Obtain a Unity stateless token for server-side authentication.
    /// 2) Try to sign in the player by calling AuthenticationCustomID with signInOnly = true (checks if account exists).
    /// 3) If sign-in returns null (account does not exist), create the account by calling AuthenticationCustomID with signInOnly = false.
    /// 4) If a new account was created, attempt to delete the anonymous account with the same external id.
    /// </summary>
    [CloudCodeFunction("AuthenticateWithExternalSystem")]
    public async Task<UnityAuthTokens> AuthenticateWithExternalSystem(IExecutionContext context, string userId, string externalToken, bool autoLink) {
        var statelessToken = await GetUnityStatelessToken(context);
        // Check for existing account.
        var ret = await AuthenticationCustomID(userId, context, statelessToken, true);
        if (ret != null)
        {
            // Account exists, delete anonymous account and return tokens.
            await DeleteAnonymousAccount(context, statelessToken);
            return ret;
        }
        // Account does not exist, create it.
        return await AuthenticationCustomID(userId, context, statelessToken, false);
    }
    /// <summary>
    /// Exchanges server credentials for a Unity stateless token.
    /// </summary>
    private async Task<string> GetUnityStatelessToken(IExecutionContext context)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://services.api.unity.com/auth/v1/token-exchange?projectId={context.ProjectId}&environmentId={context.EnvironmentId}");
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyID}:{keySecret}"));
        request.Headers.Add("Authorization", $"Basic {credentials}");
        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<TokenResponse>(content);
        return tokenData.Token;
    }
    /// <summary>
    /// Authenticate (or create) a player using a custom-id endpoint.
    /// If <paramref name="signInOnly"/> is true the method will only attempt sign-in and return null for non-200 responses.
    /// If signInOnly is false it attempts to create the account if needed.
    /// </summary>
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
        if (response.StatusCode != HttpStatusCode.OK)
        {
            Log($"AuthenticationCustomID failed for customId {customId} with status code {response.StatusCode}");
            return null;
        }            
        var content = await response.Content.ReadAsStringAsync();
        var tokens = JsonSerializer.Deserialize<UnityAuthTokens>(content);
        if (tokens == null) throw new InvalidOperationException("Failed to deserialize UnityAuthTokens from response.");
        return tokens;
    }
     /// <summary>
    /// Deletes an anonymous player that uses the same custom id.
    /// This method logs success and failures but does not throw for non-fatal conditions.
    /// </summary>
    private async Task DeleteAnonymousAccount(IExecutionContext context, string statelessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"https://services.api.unity.com/player-identity/v1/projects/{context.ProjectId}/users/{context.PlayerId}");
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyID}:{keySecret}"));
        request.Headers.Add("Authorization", $"Basic {credentials}");
        var response = await _httpClient.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            Log($"Failed to delete anonymous account for player {context.PlayerId} with status code {response.StatusCode}");
        }
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

