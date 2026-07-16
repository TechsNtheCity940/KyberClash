using UnityEngine;
using UnityEditor;
using System.IO;

public class KyberClashSetupWindow : EditorWindow
{
    [MenuItem("Kyber Clash/Setup Window")]
    public static void ShowWindow()
    {
        GetWindow<KyberClashSetupWindow>("Kyber Clash Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Kyber Clash Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("1. Create Test Data Assets", GUILayout.Height(40)))
        {
            CreateTestDataAssets();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("2. Setup Prototype Scene", GUILayout.Height(40)))
        {
            SetupPrototypeScene();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("3. Run Both (Complete Setup)", GUILayout.Height(40)))
        {
            CreateTestDataAssets();
            SetupPrototypeScene();
        }

        GUILayout.Space(15);
        GUILayout.Label("After running, open: Assets/_Project/Scenes/PrototypeArena.unity", EditorStyles.helpBox);
    }

    static void CreateTestDataAssets()
    {
        // Use the TestDataCreator if it compiles, otherwise inline
        try
        {
            var method = System.Type.GetType("KyberKlash.Utilities.TestDataCreator, Assembly-CSharp")?.GetMethod("CreateDefaultAssets");
            if (method != null)
            {
                method.Invoke(null, null);
                Debug.Log("✅ Test data assets created via TestDataCreator!");
                return;
            }
        }
        catch { }

        // Fallback inline creation
        Debug.Log("Creating test data assets inline...");
        CreateTestDataInline();
    }

    static void CreateTestDataInline()
    {
        string dataPath = "Assets/_Project/Data";
        Directory.CreateDirectory(dataPath + "/Characters");
        Directory.CreateDirectory(dataPath + "/Forms");
        Directory.CreateDirectory(dataPath + "/Attacks");
        Directory.CreateDirectory(dataPath + "/Stages");

        // Load required assemblies
        var attackType = System.Type.GetType("KyberKlash.Data.AttackSO, Assembly-CSharp");
        var formType = System.Type.GetType("KyberKlash.Data.FormSO, Assembly-CSharp");
        var characterType = System.Type.GetType("KyberKlash.Data.CharacterData, Assembly-CSharp");
        var stageType = System.Type.GetType("KyberKlash.Data.StageData, Assembly-CSharp");

        if (attackType == null || formType == null || characterType == null || stageType == null)
        {
            EditorUtility.DisplayDialog("Error", "Script types not found. Scripts may not be compiling.", "OK");
            return;
        }

        // Create using reflection
        var attackSO = ScriptableObject.CreateInstance(attackType);
        // ... this gets complex with reflection

        EditorUtility.DisplayDialog("Info", "Please use C# Interactive or Package Manager Console instead.\n\nWindow > General > C# Interactive\n\nThen paste the code from the previous message.", "OK");
    }

    static void SetupPrototypeScene()
    {
        EditorUtility.DisplayDialog("Info", "Please use C# Interactive or Package Manager Console for scene setup.\n\nWindow > General > C# Interactive", "OK");
    }
}

// Auto-open on first compile
[InitializeOnLoad]
public static class KyberClashAutoSetup
{
    static KyberClashAutoSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorPrefs.GetBool("KyberClashSetupShown", false))
            {
                EditorPrefs.SetBool("KyberClashSetupShown", true);
                KyberClashSetupWindow.ShowWindow();
            }
        };
    }
}