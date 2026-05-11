using UnityEditor;
using UnityEditor.SceneManagement;

public static class Chapter1PlayModeStartScene
{
    private const string StartScenePath = "Assets/Scenes/Chapter1/Chapter1_TownGate.unity";

    [InitializeOnLoadMethod]
    private static void SetPlayModeStartScene()
    {
        EditorApplication.delayCall += () =>
        {
            SceneAsset startScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(StartScenePath);
            if (startScene != null)
            {
                EditorSceneManager.playModeStartScene = startScene;
            }
        };
    }

    [MenuItem("JianDeng/Set Play Start Scene To TownGate")]
    public static void SetStartSceneFromMenu()
    {
        SceneAsset startScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(StartScenePath);
        if (startScene != null)
        {
            EditorSceneManager.playModeStartScene = startScene;
            EditorUtility.DisplayDialog("JianDeng", "Play Mode start scene set to Chapter1_TownGate.", "OK");
        }
    }
}
