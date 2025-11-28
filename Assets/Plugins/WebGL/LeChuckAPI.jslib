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
                const user = window.leChuckAPI.user;
                const userData = {
                    userId: user.getId(),
                    userToken: user.getToken(),
                    userName: user.getUid(),
                    userLevel: user.getLevel(),
                    isGuest: user.isGuest(),
                    avatar: user.getAvatar()
                };
                window.unityInstance.SendMessage(callbackObject, callbackFunction, JSON.stringify(userData));
            }else
                console.warn("Unity Instance not found");
        });
    },
});