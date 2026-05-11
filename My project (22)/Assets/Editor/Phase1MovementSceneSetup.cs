using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Phase1MovementSceneSetup
{
    private const string ScenePath = "Assets/Scenes/Chapter1/Chapter1_TownGate.unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player_LinZhaoying.prefab";

    [InitializeOnLoadMethod]
    private static void AutoSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (!SessionState.GetBool("JianDengAllowAutoSceneSetup", false))
            {
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (!File.Exists(ScenePath) || !File.Exists(PlayerPrefabPath))
            {
                return;
            }

            if (File.ReadAllText(ScenePath).Contains("m_Name: Phase1MovementSetupMarker"))
            {
                return;
            }

            SetupTownGateMovementScene();
        };
    }

    [MenuItem("JianDeng/Setup Phase 1 Movement Scene")]
    public static void SetupTownGateMovementScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Setup Phase 1 Movement Scene can only run in Edit Mode.");
            return;
        }

        if (!File.Exists(ScenePath))
        {
            Debug.LogWarning("Missing scene: " + ScenePath);
            return;
        }

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab == null)
        {
            Debug.LogWarning("Missing player prefab: " + PlayerPrefabPath);
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject existingPlayer = GameObject.Find("Player_LinZhaoying");
        if (existingPlayer == null)
        {
            existingPlayer = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
            existingPlayer.name = "Player_LinZhaoying";
        }

        existingPlayer.transform.position = new Vector3(-5.8f, -2.7f, 0f);
        existingPlayer.transform.rotation = Quaternion.identity;

        EnsureGroundCollider();
        RemoveCameraFollow();
        EnsureMarker();

        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Phase 1 movement scene setup complete: " + ScenePath);
    }

    private static void EnsureGroundCollider()
    {
        GameObject ground = GameObject.Find("GroundCollider");
        if (ground == null)
        {
            ground = new GameObject("GroundCollider");
        }

        ground.transform.position = new Vector3(0f, -3.05f, 0f);
        ground.transform.localScale = Vector3.one;

        BoxCollider2D collider = ground.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = ground.AddComponent<BoxCollider2D>();
        }

        collider.isTrigger = false;
        collider.offset = Vector2.zero;
        collider.size = new Vector2(18f, 0.55f);
    }

    private static void RemoveCameraFollow()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = GameObject.Find("Main Camera");
            if (cameraObject != null)
            {
                camera = cameraObject.GetComponent<Camera>();
            }
        }

        if (camera == null)
        {
            return;
        }

        CameraFollow follow = camera.GetComponent<CameraFollow>();
        if (follow != null)
        {
            Object.DestroyImmediate(follow);
        }
    }

    private static void EnsureMarker()
    {
        if (GameObject.Find("Phase1MovementSetupMarker") != null)
        {
            return;
        }

        GameObject marker = new GameObject("Phase1MovementSetupMarker");
        marker.hideFlags = HideFlags.HideInHierarchy;
    }
}
