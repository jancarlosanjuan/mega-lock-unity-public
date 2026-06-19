using System;
using UnityEngine;
using System.IO;
using JetBrains.Annotations;
using UnityEditor;

namespace MegaLock
{
    //This file is used  to authenticate on the git side. We'll use editor prefs in editor.
    [Serializable]
    public struct UserSession : IEquatable<UserSession>
    {
        public string userId;
        public string accessToken;//Valid for 5 days... let me know if we need to increase this -jc
        public string projectid;
        public bool isAdmin;
        
        public bool Equals(UserSession other)
        {
            return this.userId == other.userId && this.accessToken == other.accessToken && this.projectid == other.projectid;
        }

        public override bool Equals([CanBeNull] object obj)
        {
            return obj is UserSession other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return this.userId.GetHashCode() ^ this.accessToken.GetHashCode() ^ this.projectid.GetHashCode();
        }
        
        public static bool operator ==(UserSession a, UserSession b) => a.Equals(b);
        public static bool operator !=(UserSession a, UserSession b) => !a.Equals(b);
    }
    
    public static class UserSessionController
    {
        private static string fileName = ".credentials";
        private static string projectRoot = Directory.GetCurrentDirectory();
        private static string credentialsFullPath = Path.Join(projectRoot, fileName);
        
        public static void CreateOrUpdateUserSessionData(UserSession userSession)
        {
            File.WriteAllText(credentialsFullPath, JsonUtility.ToJson(userSession));
            EditorUserSettings.SetConfigValue("userId", userSession.userId);
            EditorUserSettings.SetConfigValue("accessToken", userSession.accessToken);
            MegalockPersistence.instance.SetCurrentUseSession(userSession);
        }

        public static bool IsSessionValid()
        {
            return SessionState.GetBool(megalock.SESSION_STATE_LOGIN_KEY, false) && File.Exists(credentialsFullPath) && MegalockPersistence.instance.currentUserSession != default(UserSession);
        }
    }
}

