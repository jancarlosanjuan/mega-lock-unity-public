using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using PlasticPipe.PlasticProtocol.Messages;
using Unity.Collections;
using Unity.Properties;
using Object = UnityEngine.Object;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor.Rendering;
using UnityEngine.UIElements;
using UnityEngine.Windows;

namespace MegaLock
{
    
    [System.Serializable]
    [GeneratePropertyBag]
    public struct LockData
    {
        public int id;
        public string guid;
        public string user_id;
        public int project_id;
        public string locked_since;
        public string description;
        public string path;
        public string name;
        [CreateProperty] 
        public string lock_duration
        {
            get
            {
                if (DateTime.TryParse(locked_since, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime parsedTime))
                {
                    TimeSpan duration = DateTime.UtcNow - parsedTime;
                    return $"{Math.Abs(duration.Days)}d {Math.Abs(duration.Hours)}h {Math.Abs(duration.Minutes)}m";
                }
                return "0d 0h 0m"; 
            }
        }
    }

    [Serializable]
    public struct SearchFields
    {
        public string assetPath;
        public string ownerName;
        public string description;
    }
    
    [FilePath("./locks.asset", FilePathAttribute.Location.ProjectFolder), Serializable]
    public class MegalockPersistence : ScriptableSingleton<MegalockPersistence>
    {
       [SerializeField] private List<LockData> _locks;
       [SerializeField] public List<LockData> _userLocks;
       
       //Use this field to query using the search fields.
       [SerializeField] public List<LockData> project_locks;
       [SerializeField] public List<LockData> user_locks;
       [SerializeField] public SerializableDictionary<string, LockData> locksGuidHash; //This is used in the asset highlighter. We take up more storage, but we save so much linq operations in return
       
       //FIELDS THAT DO NOT NEED TO BE SAVED. WE ONLY NEED THESE DURING THE DURATION OF THE EDITOR SESSION
       //Megalock TAB states to hold and initialize after domain reloads. Different usage from ViewManager. This one is Tab specific
       [SerializeField] public BaseView currentTabView;
       //Handling User session persistence
       [SerializeField] public UserSession currentUserSession;
       
       public DateTime lastFetchedAt;
       
       //Fields to keep in every tab. We just read from here so all the fields remain the same
       public SearchFields searchFields; //this might look weird but we need it to keep track of the search states per tab
       public string userLocksSearchField;
       
        public List<LockData> SaveAll(List<LockData> rows)
        {
            _locks.Clear();
            _locks.AddRange(rows);
            locksGuidHash.Clear();
            _userLocks.Clear();
            foreach (LockData row in rows)
            {
                locksGuidHash.Add(row.guid.SanitizePostgresUuidToUnityGuid(), row);
                if(row.user_id == currentUserSession.userId)
                    _userLocks.Add(row);
            }
            UpdateProjectLockSearchStates(this.searchFields, false);
            UpdateUserLockSearchStates(this.userLocksSearchField, false);
            lastFetchedAt = DateTime.UtcNow;
            instance.Save(true);
            EditorApplication.RepaintProjectWindow();
            return _locks;
        }
        
        [Obsolete("Use SaveAll(List<LockData> rows) instead.")]
        public List<LockData> SaveUserLocks(List<LockData> rows)
        {
            _userLocks.Clear();
            _userLocks.AddRange(rows);
            instance.Save(true);
            EditorApplication.RepaintProjectWindow();
            return _userLocks;
        }

        public List<LockData> UpdateProjectLockSearchStates(SearchFields searchFields, bool callSave = true)
        {
            this.searchFields = searchFields;
            project_locks.Clear();
            project_locks.AddRange(
                _locks.Select(
                    r => new
                    {
                        Row = r,
                        pathScore = MegalockUtilities.MatchAssetPath(searchFields.assetPath, r.path),
                        ownerScore = MegalockUtilities.MatchContains(searchFields.ownerName, r.name),
                        descriptionScore = MegalockUtilities.MatchContains(searchFields.description, r.description)
                    }).Where(
                        e =>
                            e.pathScore > 0 &&
                            e.ownerScore > 0 &&
                            e.descriptionScore > 0
                    ).OrderByDescending(
                    s=>s.pathScore + s.ownerScore + s.descriptionScore
                    ).Select(
                     v => v.Row
                    ).ToList()
                );

            if (callSave)
            {
                instance.Save(true);
                EditorApplication.RepaintProjectWindow();
            }
            return project_locks;
        }
        
        public List<LockData> UpdateUserLockSearchStates(string searchText, bool callSave = true)
        {
            this.userLocksSearchField = searchText;
            user_locks.Clear();
            user_locks.AddRange(
                _userLocks.Select(
                    r => new
                    {
                        Row = r,
                        pathScore = MegalockUtilities.MatchAssetPath(searchText, r.path),
                    }).Where(
                    e =>
                        e.pathScore > 0
                ).OrderByDescending(
                    s=>s.pathScore
                ).Select(
                    v => v.Row
                ).ToList()
            );
            if (callSave)
            {
                instance.Save(true);
                EditorApplication.RepaintProjectWindow();
            }
            return user_locks;
        }
        
        public void ClearAll()
        {
            _locks.Clear();
            Save(true);
        }
        
        //Handling Selections. We don't need to save this. It already survives between domain reloads.
        [SerializeField] public List<Object> selectedObjects;

        [MenuItem("Assets/Add Selections To Lock Staging")]
        public static void AddSelectionsToLockStaging()
        {
            var selections = Selection.objects;

            if (selections == null || selections.Length == 0) return;

            var existingSet = new HashSet<Object>(instance.selectedObjects);

            foreach (var obj in selections)
            {
                if (!MegalockUtilities.IsDirectory(obj) && existingSet.Add(obj))
                {
                    instance.selectedObjects.Add(obj);
                }
            }
            
            var megaLockWindow = EditorWindow.GetWindow<megalock>("Mega Lock");
            if (megaLockWindow != null)
            {
                var viewmanager = megaLockWindow.GetViewManager();
                if (viewmanager == null)
                {
                    Debug.LogError("Could not find view manager");
                    return;
                }

                var mainView = viewmanager.GetView<view_main>();
                if (mainView == null) return;

                if(mainView.tabViews.TryGetValue(typeof(view_add_locks), out var view) ? (view_add_locks)view : null)
                    ((view_add_locks)view)?.RefreshStagingList();
            }
            else
            {
                Debug.LogError("Cannot add Selections To Lock Staging");
            }
        }
        
        [MenuItem("Assets/Add Selections To Lock Staging", true)]
        public static bool AddSelectionsToLockStaging_Check()
        {
            return Selection.objects.Length > 0;
        }
        
        public void RefreshAllLocks(Action callback)
        {
            TimeSpan diff = lastFetchedAt - DateTime.UtcNow;
            if (Math.Abs(diff.TotalSeconds) < 15)
            {
                callback?.Invoke();
                return;
            }
                
            var megaLockWindow = EditorWindow.GetWindow<megalock>("Mega Lock");
            if (megaLockWindow != null)
            {
                megaLockWindow.GetViewManager()?.TryRunCoroutine(MegalockAPIController.CallFetchLocksApi(MegalockPersistence.instance.currentUserSession,
                        (res, json) =>
                    {
                        if (res)
                        {
                            var rows = JsonConvert.DeserializeObject<List<LockData>>(json);
                            SaveAll(rows);
                            return;
                        }
                    }),
                    (r) =>
                    {
                        callback?.Invoke();
                    });
            }
            else
            {
                
            }
        }
        public void SetCurrentUseSession(UserSession userSession)
        {
            currentUserSession = userSession;
            Save(true);
        }
        
        public static bool IsCurrentlyLockedBySomeoneElse(string guid)
        {
            if (!MegalockPersistence.instance.locksGuidHash.TryGetValue(guid.SanitizePostgresUuidToUnityGuid(),
                    out var data))
                return false;
            return data.user_id != MegalockPersistence.instance.currentUserSession.userId;
        }
    }

}
