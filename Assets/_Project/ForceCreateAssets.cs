#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class ForceCreateAssets : EditorWindow
{
    [MenuItem("Kyber Clash/Force Create Test Assets")]
    static void Create()
    {
        KyberKlash.Utilities.TestDataCreator.CreateDefaultAssets();
    }
}
#endif
