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
        public string avatar = "http://";
    }
    void Awake() {
        gameObject.name = "CallbackTarget";
#if !UNITY_WEBGL || UNITY_EDITOR
        OnAPIReady(null);
#endif
    }
    async void Start() {
        // Initialize Unity Services.
        await UnityServices.InitializeAsync();
        //        
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (AuthenticationException ex)
        {
            // Handle authentication errors
            if (ex.ErrorCode == AuthenticationErrorCodes.InvalidSessionToken)
                Debug.LogError("[FPA] Deleted session token. Try again.");
            else
                Debug.LogError("[FPA] Sign in failed: " + ex.ErrorCode);
            return;
        }
        catch (RequestFailedException ex)
        {
            // Handle network or other request errors
                Debug.LogError($"Request failed: ({ex.ErrorCode}) {ex.Message}.");
            return;
        }
        Debug.Log($"[FPA] Backend Version: {await Version()}");
        var res = await SignInUserWithLeChuck();
        Debug.Log($"[FPA] PlayerID: {AuthenticationService.Instance.PlayerId}");
    }
    // Backend functions.
    public static string ModuleName = "ExternalAuthModule";
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
    async Task<bool> SignInUserWithLeChuck() {
        var customID = AuthenticationService.Instance.PlayerInfo.GetCustomId();
        if (customID != null) return true; // Already signed in with CustomID.
        var res = await InitializeLeChuck();
        if(!res) {
            Debug.LogError("[FPA] Could not initialize LeChuck API.");
            return false;
        }
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
            return false;
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
            return false;
        }
        catch (System.Exception ex)
        {
            // Handle exceptions from your method call
            Debug.LogException(ex);
            return false;
        }
        return true;
    }
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern bool initializeLeChuckAPI();
#else
    private static bool initializeLeChuckAPI() {
        GameObject.Find("CallbackTarget").SendMessage("OnAPIReady", "{}") ;
        return true;
    }
#endif
    private LeChuckUserData userData = null;
    // Initialize LeChuck API and get user data.
    public async Task<bool> InitializeLeChuck() {
        userData = null;
        initializeLeChuckAPI();
        // Wait for MiniJuegos API to be ready and get user data.
        float timeout = Time.time + 10f; // timeout in seconds
        while (userData==null && timeout > Time.time)
            await Task.Delay(100);
        if(userData==null) {
            Debug.LogError($"[FPA] Minijuegos API did not respond in time.");
            return false;
        }
        Debug.Log($"[FPA] Minijuegos: UserID: {userData.userId}, userName: {userData.userName}, userLevel: {userData.userLevel}. isGuest: {userData.isGuest}, avatar: {userData.avatar}");
        return true;
    }
    // Callback from MiniJuegos API when ready.
    public void OnAPIReady(string jsonData) {
        Debug.Log($"[FPA] Minijuegos API is ready.");
        try {
            userData = JsonUtility.FromJson<LeChuckUserData>(jsonData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FPA] Error parsing LeChuck user data: {ex.Message}");
        }
    }
}
