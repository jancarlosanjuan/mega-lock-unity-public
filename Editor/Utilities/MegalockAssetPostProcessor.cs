using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MegaLock
{
    public class MegalockAssetPostProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets, 
            string[] deletedAssets, 
            string[] movedAssets, 
            string[] movedFromAssetPaths)
        {
            if (!UserSessionController.IsSessionValid())
            {
                //EditorUtility.DisplayDialog("Warning!", "Megalock session is inactive. Login to receive latest updates", "OK");
                Debug.LogWarning("Megalock session is inactive. Login to receive latest updates");
                return;
            }
            
            MegalockPersistence.instance.RefreshAllLocks(() =>
            {
                HashSet<string> warningPaths = new HashSet<string>();
                
                foreach (string str in importedAssets)
                {
                    TryAddPathToWarningPath(str);
                }

                foreach (string str in deletedAssets)
                {
                    TryAddPathToWarningPath(str);
                }

                foreach (string str in movedAssets)
                {
                    TryAddPathToWarningPath(str);
                }

                if (warningPaths.Count > 0)
                {
                    EditorUtility.DisplayDialog("Warning!", "Ignore if Git change... \n \n " + 
                                                            "Commiting these files will result in commit failure because it's locked by someone else: \n \n" +
                                                            string.Join("\n", warningPaths)+ 
                                                            "\n \n \n \nDiscard the change in git to fix.", "I understand");
                }
                
                
                return;
            
                void TryAddPathToWarningPath(string str)
                {
                    var guid = AssetDatabase.AssetPathToGUID(str);
                    if (MegalockPersistence.IsCurrentlyLockedBySomeoneElse(guid))
                    {
                        warningPaths.Add(str);
                    }
                }
            });
            
        }
    }
}

