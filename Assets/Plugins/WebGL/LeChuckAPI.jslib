mergeInto(LibraryManager.library, {
    initializeLeChuckAPI: function(callbackObject, callbackFunction) {
        if (typeof leChuckAPI === 'undefined' ) {
            console.warn("LeChuck API not loaded");
            return;
        }
        window.leChuckAPI = new LeChuckAPI({ id: '8806'});
        window.leChuckAPI.events.onApiReady(function() {
            console.log("LeChuck API Ready!");
            // Enviar datos a Unity
            if (window.unityInstance){
                const userData = {
                    userId: leChuckAPI.user.getId(),
                    userToken: leChuckAPI.user.getToken(),
                    userName: leChuckAPI.user.getUid(),
                    userLevel: leChuckAPI.user.getLevel(),
                    isGuest: leChuckAPI.user.isGuest(),
                    avatar: leChuckAPI.user.getAvatar()
                };
                window.unityInstance.SendMessage(callbackObject, callbackFunction, JSON.stringify(userData));
            }else
                console.warn("Unity Instance not found");
        });
    },
});