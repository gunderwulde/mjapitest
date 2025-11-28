mergeInto(LibraryManager.library, {
    initializeLeChuckAPI: function() {
        if (typeof leChuckAPI === 'undefined' ) {
            console.error("[FPA] LeChuck API not loaded");
            return false;
        }
        console.log("[FPA] Initializcing LeChuckAPI");
        window.leChuckAPI = new LeChuckAPI({ id: '8806'});
        window.leChuckAPI.events.onApiReady(function() {
            // Enviar datos a Unity
            if (unityInstance){
                const user = window.leChuckAPI.user;
                const userData = {
                    userId: user.getId(),
                    userToken: user.getToken(),
                    userName: user.getUid(),
                    userLevel: user.getLevel(),
                    isGuest: user.isGuest(),
                    avatar: user.getAvatar()
                };
                unityInstance.SendMessage('CallbackTarget', 'OnAPIReady', JSON.stringify(userData) );
            }else{
                console.error("[FPA] Unity Instance not found");
                return false;
            }
        });
        return true;
    },
});