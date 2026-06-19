using UnityEditor;
using UnityEngine;

namespace MegaLock
{
    public static class MegalockUtilities
    {
        public static string HighlightAssetNameFromPath(string path)
        {
            string assetName = path.Substring(path.LastIndexOf('/') + 1);
            string highlightedName = $"<color=yellow>{assetName}</color>";
            return path.Substring(0, path.LastIndexOf('/') + 1) + highlightedName;
        }
        
        public static bool IsDirectory(UnityEngine.Object obj)
        {
            if (obj == null) return false;
            string path = AssetDatabase.GetAssetPath(obj);
            return AssetDatabase.IsValidFolder(path);
        }

        public static string SanitizePostgresUuidToUnityGuid(this string guid)
        {
            return guid.Replace("-", "");
        }
    }
}

