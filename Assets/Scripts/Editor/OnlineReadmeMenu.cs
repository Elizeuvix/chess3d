#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace Chess3D.EditorTools
{
    public static class OnlineReadmeMenu
    {
        [MenuItem("Tools/Chess3D/Online/Abrir README Online", priority = 1000)]
        public static void OpenReadme()
        {
            // Caminho relativo ao projeto
            string path = Path.Combine(Application.dataPath, "..", "README-online.md");
            path = Path.GetFullPath(path);
            if (File.Exists(path))
            {
                UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(path, 1);
            }
            else
            {
                EditorUtility.DisplayDialog("README Online", "Arquivo não encontrado:\n" + path, "OK");
            }
        }
    }
}
#endif
