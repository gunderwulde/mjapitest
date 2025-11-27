mergeInto(LibraryManager.library, {
    getLeChuckUserId: function() {
        if (typeof window.leChuckAPI === 'undefined' || !window.leChuckAPI) {
            return null;
        }
        var userId = window.leChuckAPI.user.getId();
        if (!userId) return null;
        
        var bufferSize = lengthBytesUTF8(userId) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(userId, buffer, bufferSize);
        return buffer;
    },

    getLeChuckUserToken: function() {
        if (typeof window.leChuckAPI === 'undefined' || !window.leChuckAPI) {
            return null;
        }
        var token = window.leChuckAPI.user.getToken();
        if (!token) return null;
        
        var bufferSize = lengthBytesUTF8(token) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(token, buffer, bufferSize);
        return buffer;
    },

    getLeChuckUserData: function() {
        if (typeof window.leChuckAPI === 'undefined' || !window.leChuckAPI) {
            return null;
        }
        
        try {
            var userData = {
                userId: window.leChuckAPI.user.getId(),
                userToken: window.leChuckAPI.user.getToken(),
                userName: window.leChuckAPI.user.getUid(),
                userLevel: window.leChuckAPI.user.getLevel(),
                isGuest: window.leChuckAPI.user.isGuest(),
                avatar: window.leChuckAPI.user.getAvatar()
            };
            
            var jsonString = JSON.stringify(userData);
            var bufferSize = lengthBytesUTF8(jsonString) + 1;
            var buffer = _malloc(bufferSize);
            stringToUTF8(jsonString, buffer, bufferSize);
            return buffer;
        } catch (e) {
            console.error("Error getting LeChuck user data:", e);
            return null;
        }
    }
});