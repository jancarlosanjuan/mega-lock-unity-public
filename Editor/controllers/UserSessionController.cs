using System;
using UnityEngine;
using System.IO;
using UnityEditor;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;

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

        public void ClearSession()
        {
            userId = string.Empty;
            accessToken = string.Empty;
            projectid = string.Empty;
            isAdmin = false;
        } 
        
        public bool Equals(UserSession other)
        {
            return this.userId == other.userId && this.accessToken == other.accessToken && this.projectid == other.projectid;
        }

        public override bool Equals(object obj)
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

        #region utils
        public static string GetCredentialsPath => credentialsFullPath;
        public static bool DoesCredentialsExist => File.Exists(credentialsFullPath);
        public static bool TryParseLocalCredentials(out UserSession session)
        {
            session = default;
            if (!DoesCredentialsExist)
            {
                Debug.LogWarning("Could not find credential file for MegaLock Authentication. Try Logging in once every 30 days to receive updates automatically.");
                return false;
            }

            var fullText = File.ReadAllText(credentialsFullPath);
            var obj = JObject.Parse(fullText);
            session = new UserSession()
            {
                userId = (string)obj["userId"],
                accessToken = (string)obj["accessToken"],
                projectid =  (string)obj["projectid"],
                isAdmin = (bool)obj["isAdmin"]
            };
            return true;
        }
        
        #endregion        
        
        public static void CreateOrUpdateUserSessionData(UserSession userSession)
        {
            File.WriteAllText(credentialsFullPath, JsonUtility.ToJson(userSession));
            EditorUserSettings.SetConfigValue("userId", userSession.userId);
            EditorUserSettings.SetConfigValue("accessToken", userSession.accessToken);
            MegalockPersistence.instance.SetCurrentUseSession(userSession);
        }

        public static void DeleteUserSessionData()
        {
            File.Delete(credentialsFullPath);
            EditorUserSettings.SetConfigValue("userId", string.Empty);
            EditorUserSettings.SetConfigValue("accessToken", string.Empty);
            SessionState.SetBool(megalock.SESSION_STATE_LOGIN_KEY, false);
            MegalockPersistence.instance.currentUserSession.ClearSession();
        }

        public static bool IsSessionValid()
        {
            return SessionState.GetBool(megalock.SESSION_STATE_LOGIN_KEY, false) && File.Exists(credentialsFullPath) && MegalockPersistence.instance.currentUserSession != default(UserSession);
        }
    }
}

