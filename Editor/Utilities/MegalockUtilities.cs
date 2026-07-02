using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MegaLock
{
    public static class MegalockUtilities
    {
        public static string HighlightAssetNameFromPath(string path, string color = "yellow")
        {
            string assetName = path.Substring(path.LastIndexOf('/') + 1);
            string highlightedName = $"<color={color}>{assetName}</color>";
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

        private static int GetFuzzySearchScore(string input, string text)
        {
            if (string.IsNullOrWhiteSpace(input))
                return 1;
            
            input = input.ToLowerInvariant().Trim();
            text = text.ToLowerInvariant().Trim();

            int score = 0;
            int patternIndex = 0;

            for (int i = 0; i < text.Length && patternIndex < input.Length; i++)
            {
                if (text[i] == input[patternIndex])
                {
                    score += 10;
                    patternIndex++;
                }
            }

            if (patternIndex != input.Length)
                return 0;

            return score;
        } 
        
        public static int MatchContains(string input, string text)
        {
            if (string.IsNullOrWhiteSpace(input))
                return 1;

            if (string.IsNullOrEmpty(text))
                return 0;

            return text.Contains(input, StringComparison.OrdinalIgnoreCase)
                ? 999
                : 0;
        }
        
        public static int MatchAssetPath(string filter, string value)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return 1;

            if (string.IsNullOrEmpty(value))
                return 0;

            if (value.Contains(filter, StringComparison.OrdinalIgnoreCase))
                return 999;
            
            return GetFuzzySearchScore(filter, value);
        }
    }
    
    [System.Serializable]
    public class SerializableDictionary<K, V> : Dictionary<K, V>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<K> m_Keys = new List<K>();

        [SerializeField]
        private List<V> m_Values = new List<V>();

        public void OnBeforeSerialize()
        {
            m_Keys.Clear();
            m_Values.Clear();
            using Enumerator enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                KeyValuePair<K, V> current = enumerator.Current;
                m_Keys.Add(current.Key);
                m_Values.Add(current.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            Clear();
            for (int i = 0; i < m_Keys.Count; i++)
            {
                Add(m_Keys[i], m_Values[i]);
            }

            m_Keys.Clear();
            m_Values.Clear();
        }
    }
}

