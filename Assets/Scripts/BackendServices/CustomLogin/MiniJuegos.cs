using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.CloudCode;
using Unity.Services.Core;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public class MiniJuegos : MonoBehaviour
{
    private static MiniJuegos instance;

    [Serializable]
    public class LeChuckUserData
    {
        public string userId;
        public string userToken;
        public string userName;
        public int userLevel;
        public bool isGuest;
        public string avatar;
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
#if !UNITY_WEBGL || UNITY_EDITOR
        OnAPIReady();
#endif
    }

    public static string ModuleName = "ExternalAuthModule";
    async void Start()
    {
        await UnityServices.InitializeAsync();

//        AuthenticationService.Instance.ClearSessionToken();
//        AuthenticationService.Instance.SignedIn += () => Debug.Log("Player signed in: " + AuthenticationService.Instance.PlayerId);
        await SignInUserWithCustomID();
        var responseVersion = await Version();
        Debug.Log($"responseTest: {responseVersion}");

    }
    async Task<string> Version() => await CloudCodeService.Instance.CallModuleEndpointAsync<string>(ModuleName,"Version");
    async Task<UnityAuthTokens> AuthenticateWithExternalSystem(string userId, string externalToken, bool autoLink)
    {
        return await CloudCodeService.Instance.CallModuleEndpointAsync<UnityAuthTokens>(
            ModuleName,
            "AuthenticateWithExternalSystem",
            new Dictionary<string, object> { { "userId", userId }, { "externalToken", externalToken }, { "autoLink", autoLink } }
        );
    }

    async Task SignInUserWithCustomID()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            // Si no estoy vinculado a una cuenta de CustomID, hago la vinculacion.
            var customID = AuthenticationService.Instance.PlayerInfo.GetCustomId();
            if (customID == null) {
                // Mira si ya existe una cuenta con ese CustomID
                var response = await AuthenticateWithExternalSystem("USER_ID", "EXT_TOKEN", true); // Use autolink.
                AuthenticationService.Instance.SignOut();
                AuthenticationService.Instance.ProcessAuthenticationTokens(response.idToken, response.sessionToken);
                customID = AuthenticationService.Instance.PlayerInfo.GetCustomId();
            }

            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId} -> customID {customID}");            
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

    public void OnAPIReady()
    {
        try
        {
            Debug.Log($"Minijuegos: UserID: {MiniJuegos.GetUserId()}, token: {MiniJuegos.GetUserToken()}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing LeChuck user data: {ex.Message}");
        }
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string getLeChuckUserId();

    [DllImport("__Internal")]
    private static extern string getLeChuckUserToken();

    [DllImport("__Internal")]
    private static extern string getLeChuckUserData();

    public static string GetUserId()
    {
        return getLeChuckUserId();
    }

    public static string GetUserToken()
    {
        return getLeChuckUserToken();
    }

    public static LeChuckUserData GetUserData()
    {
        string jsonData = getLeChuckUserData();
        if (string.IsNullOrEmpty(jsonData))
            return null;
        return JsonUtility.FromJson<LeChuckUserData>(jsonData);
    }
#else
    public static string GetUserId() => "<USER_ID>";
    public static string GetUserToken() =>  "<USER_TOKEN>";
    public static LeChuckUserData GetUserData() => new LeChuckUserData();
#endif

    [System.Serializable]
    public class UnityAuthTokens
    {
        public string idToken;
        public string sessionToken;
    }



}
