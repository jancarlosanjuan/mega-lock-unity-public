using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Text;
using log4net.Appender;
using Unity.Plastic.Newtonsoft.Json;

namespace MegaLock
{
    public struct LoginData
    {
        public string userid;
        public string password;
        public string projectname;
    }

    public struct RegisterData
    {
        public string userid;
        public string password;
    }

    //This is the actual shape needed for the JSON body
    [Serializable]
    public struct AddLockDataList
    {
        public List<AddLockData> locks;
    }
    [Serializable]
    public struct AddLockData
    {
        public string guid;
        public string path;
        public string description;
    }
    
    [Serializable]
    public struct DeleteLocksDataList
    {
        public List<string> guids;
    }

    public static class MegalockAPIController
    {
        private static string API_ROOT_URL = "https://e1kstypsh8.execute-api.us-east-1.amazonaws.com/production";

        public static IEnumerator CallValidateSessionToken(UserSession userSession, Action<bool, string> callback)
        {
            string url = API_ROOT_URL + "/user/" + userSession.userId;
            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {userSession.accessToken}");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Fetch locks failed: {request.error}");
                }
                else
                {
                    //Debug.Log($"Response Code: {request.responseCode}");
                }

                callback?.Invoke(request.result == UnityWebRequest.Result.Success, request.downloadHandler?.text);
            }
        }
        public static IEnumerator CallLoginApi(LoginData loginData, Action<bool,string> callback)
        {
            //Debug.Log("Logging in");
            string url = API_ROOT_URL + "/user/login";
            /*
             * {
                "userid" : "U03B1FVSNG1",
                "password" : "somepasswordhere"
                }
             */
            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.SetRequestHeader("Accept", "application/json");
            
                request.downloadHandler = new DownloadHandlerBuffer();
                string body = JsonUtility.ToJson(loginData);
                byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    EditorUtility.DisplayDialog("Validation Error: " + request.error,
                        "Credentials error. Invalid project name, username, or password.",
                        "OK");
                }
                else
                {
                    //Debug.Log($"Response Code: {request.responseCode}");
                    string json = request.downloadHandler?.text;
                }
                
                callback?.Invoke(request.result == UnityWebRequest.Result.Success, request.downloadHandler?.text);
            }
        }
        public static IEnumerator CallFetchLocksApi(UserSession userSession, Action<bool, string> callback)
        {
            string url = API_ROOT_URL + $"/project/{MegalockPersistence.instance.currentUserSession.projectid}/locks";
            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
            {
                request.SetRequestHeader("Authorization", $"Bearer {userSession.accessToken}");
                //request.SetRequestHeader("Accept", "application/json");

                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Accept", "application/json");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Fetch locks failed: {request.error}");
                }
                else
                {
                    //Debug.Log($"Response Code: {request.responseCode}");
                }

                callback?.Invoke(request.result == UnityWebRequest.Result.Success, request.downloadHandler?.text);
            }
        }
        public static IEnumerator CallFetchUserLocksApi(UserSession userSession, Action<bool, string> callback)
        {
            //project/1/locks/user/U03B1FVSNG1
            string url = API_ROOT_URL + $"/project/" +
                         $"{MegalockPersistence.instance.currentUserSession.projectid}/locks/" +
                         $"user/{MegalockPersistence.instance.currentUserSession.userId}";
            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {userSession.accessToken}");
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Fetch locks failed: {request.error}");
                }
                else
                {
                    //Debug.Log($"Response Code: {request.responseCode}");
                }

                callback?.Invoke(request.result == UnityWebRequest.Result.Success, request.downloadHandler?.text);
            }
        }

        public static IEnumerator CallAddLocksApi(AddLockDataList addLockDataList,UserSession userSession, Action<bool, string> callback)
        {
            //project/1/locks/user/U03B1FVSNG1/files
            string url = API_ROOT_URL + $"/project/" +
                         $"{MegalockPersistence.instance.currentUserSession.projectid}/locks/" +
                         $"user/{MegalockPersistence.instance.currentUserSession.userId}/files";
            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {userSession.accessToken}");
                
                string body = JsonUtility.ToJson(addLockDataList);
                byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Error: {request.error} | Response Code: {request.responseCode}");
                }
                else
                {
                    //Debug.Log($"Successful POST insert locks: {request.downloadHandler.text}");
                }

                callback?.Invoke(request.result == UnityWebRequest.Result.Success, request.downloadHandler?.text);
            }
        }
        
        public static IEnumerator CallDeleteLocksApi(DeleteLocksDataList deleteLockDataList,UserSession userSession, Action<bool, string> callback)
        {
            //project/1/locks/user/U03B1FVSNG1/files
            /*{
                "guids" : [
                "37354188-b257-a1c4-584a-8bf85aea5838",
                "9d33d77f-1ff8-d4a4-f90f-5bd31b826cd9"
                    ]
            }*/
            
            string url = API_ROOT_URL + $"/project/" +
                         $"{MegalockPersistence.instance.currentUserSession.projectid}/locks/" +
                         $"user/{MegalockPersistence.instance.currentUserSession.userId}/files";
            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbDELETE))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {userSession.accessToken}");
                
                string body = JsonUtility.ToJson(deleteLockDataList);
                byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Error: {request.error} | Response Code: {request.responseCode}");
                }
                else
                {
                    //Debug.Log($"Successful POST insert locks: {request.downloadHandler.text}");
                }

                callback?.Invoke(request.result == UnityWebRequest.Result.Success, request.downloadHandler?.text);
            }
        }
        
        public static IEnumerator CallAdminDeleteLocksApi(DeleteLocksDataList deleteLockDataList,UserSession userSession, Action<bool, string> callback)
        {
            //project/1/admin/U03B1FVSNG1/files
            /*{
                "guids" : [
                "37354188-b257-a1c4-584a-8bf85aea5838",
                "9d33d77f-1ff8-d4a4-f90f-5bd31b826cd9"
                    ]
            }*/
            
            string url = API_ROOT_URL + $"/project/" +
                         $"{MegalockPersistence.instance.currentUserSession.projectid}/admin/" +
                         $"{MegalockPersistence.instance.currentUserSession.userId}/files";
            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbDELETE))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {userSession.accessToken}");
                
                string body = JsonUtility.ToJson(deleteLockDataList);
                byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Error: {request.error} | Response Code: {request.responseCode}");
                }
                else
                {
                    //Debug.Log($"Successful POST insert locks: {request.downloadHandler.text}");
                }

                callback?.Invoke(request.result == UnityWebRequest.Result.Success, request.downloadHandler?.text);
            }
        }

        public static IEnumerator CallRegisterApi(RegisterData registerData, Action<bool, string> callback)
        {
            string url = API_ROOT_URL + "/user/register";
            /*
             * {
                "userid" : "U03B1FVSNG1",
                "password" : "somepasswordhere"
                }
             */
            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                string body = JsonUtility.ToJson(registerData);
                byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Register failed: {request.error}. Invalid user. Does not exist in slack org.");
                }
                else
                {
                    //Debug.Log($"Response Code: {request.responseCode}");
                    //string json = request.downloadHandler?.text;
                }
                
                callback?.Invoke(request.result == UnityWebRequest.Result.Success, request.downloadHandler?.text);
            }
        }
    }

}
