using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.Core;
using UnityEngine;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public class MiniJuegos : MonoBehaviour
{
    [Serializable]
    public class LeChuckUserData
    {
        public string userId = "USER_ID";
        public string userToken = "EXT_TOKEN";
        public string userName = "USE_NAME";
        public int userLevel = 1;
        public bool isGuest = false;
        public string avatar ="http://";
    }
    void Awake() {
#if !UNITY_WEBGL || UNITY_EDITOR
        OnAPIReady(null);
#endif
    }
    public static string ModuleName = "ExternalAuthModule";
    async void Start() {
        // Initialize Unity Services.
        await UnityServices.InitializeAsync();
//        AuthenticationService.Instance.ClearSessionToken();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        Debug.Log($"Backend Version: {await Version()}");
        await SignInUserWithLeChuck();
        Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");
    }
    // Backend functions.
    async Task<string> Version() => await CloudCodeService.Instance.CallModuleEndpointAsync<string>(ModuleName,"Version");
    [System.Serializable]
    public class UnityAuthTokens {
        public string idToken;
        public string sessionToken;
    }
    // Call Cloud Code to authenticate or link user with external system.
    async Task<UnityAuthTokens> AuthenticateWithExternalSystem(string userId, string externalToken, bool autoLink) {
        return await CloudCodeService.Instance.CallModuleEndpointAsync<UnityAuthTokens>(
            ModuleName,
            "AuthenticateWithExternalSystem",
            new Dictionary<string, object> { { "userId", userId }, { "externalToken", externalToken }, { "autoLink", autoLink } }
        );
    }    
    async Task SignInUserWithLeChuck() {
        var customID = AuthenticationService.Instance.PlayerInfo.GetCustomId();
        if (customID != null) return; // Already signed in with CustomID.
        await InitializeLeChuck();
        try {
            // Check or link user account.
            var response = await AuthenticateWithExternalSystem(userData.userId, userData.userToken, true); // Use autolink.
            // Signout anonymous session before processing new tokens.
            AuthenticationService.Instance.SignOut();
            // Process the returned authentication tokens.
            AuthenticationService.Instance.ProcessAuthenticationTokens(response.idToken, response.sessionToken);
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (System.Exception ex)
        {
            // Handle exceptions from your method call
            Debug.LogException(ex);
        }
    }
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern bool initializeLeChuckAPI(string callbackObject, string callbackFunction);
#else
    private static bool initializeLeChuckAPI(string callbackObject, string callbackFunction) {
        GameObject.Find(callbackObject).SendMessage(callbackFunction, "{}") ;
        return true;
    }
#endif
    private static LeChuckUserData userData = null;
    // Initialize LeChuck API and get user data.
    public async Task InitializeLeChuck() {
        initializeLeChuckAPI(gameObject.name, "OnAPIReady");
        // Wait for MiniJuegos API to be ready and get user data.
        while (userData==null) await Task.Delay(100);
        Debug.Log($"Minijuegos: UserID: {userData.userId}, userName: {userData.userName}, userLevel: {userData.userLevel}. isGuest: {userData.isGuest}, avatar: {userData.avatar}");
    }
    // Callback from MiniJuegos API when ready.
    public void OnAPIReady(string jsonData) {
        try {
            userData = JsonUtility.FromJson<LeChuckUserData>(jsonData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing LeChuck user data: {ex.Message}");
        }
    }
}
