using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class ChapterSceneInitializer : MonoBehaviour
{
    private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player_LinZhaoying.prefab";
    private const float VignetteEntryAlpha = 0.16f;

    private static ChapterSceneInitializer instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Ensure()
    {
        if (instance != null)
        {
            return;
        }

        GameObject runner = new GameObject("ChapterSceneInitializer");
        DontDestroyOnLoad(runner);
        instance = runner.AddComponent<ChapterSceneInitializer>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(InitializeAfterSceneObjectsExist(scene));
    }

    private IEnumerator InitializeAfterSceneObjectsExist(Scene scene)
    {
        yield return null;

        Debug.Log("[SceneInit] Loaded scene: " + scene.name);

        RemoveDuplicateRuntimeObjects(scene);
        EnsureSceneSpawnPoints(scene.name);
        SceneBackgroundSet backgroundSet = ResolveSceneBackgroundSet();
        if (backgroundSet != null)
        {
            backgroundSet.SetLanternVision(false);
        }

        LanternVisionController controller = FindObjectOfType<LanternVisionController>(true);
        if (controller != null)
        {
            controller.ForceRealityMode();
        }

        ForceRealityBackgroundObjects();
        ResetSceneUi();
        GameObject player = EnsureSinglePlayer();
        string spawnPointId = SceneTransition2D.ConsumePendingSpawnPointId();
        if (string.IsNullOrEmpty(spawnPointId))
        {
            spawnPointId = GetDefaultSpawnPointId(scene.name);
        }

        Transform spawnPoint = FindSpawnPoint(spawnPointId);
        if (player != null && spawnPoint != null)
        {
            MovePlayerToSpawn(player, spawnPoint);
            Debug.Log("[SceneInit] Player moved to " + spawnPointId);
        }

        Debug.Log("[SceneInit] Reality mode forced");

        GameObject overlay = FindObjectIncludingInactive("LanternVisionOverlay_UI") ?? FindObjectIncludingInactive("LanternVisionOverlay");
        Debug.Log("LanternVisionOverlay active = " + (overlay != null && overlay.activeInHierarchy));
    }

    private static SceneBackgroundSet ResolveSceneBackgroundSet()
    {
        SceneBackgroundSet set = FindObjectOfType<SceneBackgroundSet>(true);
        if (set != null)
        {
            return set;
        }

        GameObject sceneRoot = GameObject.Find("SceneRoot");
        if (sceneRoot == null)
        {
            return null;
        }

        Transform background = sceneRoot.transform.Find("Background");
        Transform hidden = sceneRoot.transform.Find("HiddenObjects");
        set = sceneRoot.GetComponent<SceneBackgroundSet>() ?? sceneRoot.AddComponent<SceneBackgroundSet>();
        set.realityBackground = background != null && background.Find("Reality_BG") != null ? background.Find("Reality_BG").gameObject : null;
        set.lanternVisionBackground = background != null && background.Find("LanternVision_BG") != null ? background.Find("LanternVision_BG").gameObject : null;
        set.hiddenObjects = hidden != null ? hidden.gameObject : null;
        return set;
    }

    private static void ForceRealityBackgroundObjects()
    {
        SetActiveIfFound("Reality_BG", true);
        SetActiveIfFound("LanternVision_BG", false);
        SetActiveIfFound("HiddenObjects", false);
        SetActiveIfFound("LanternVisionOverlay", false);
        SetActiveIfFound("CommonVFX_VignetteOverlay", false);
    }

    private static void ResetSceneUi()
    {
        SetActiveIfFound("LanternVisionOverlay_UI", false);
        SetActiveIfFound("LanternVisionOverlay", false);
        SetActiveIfFound("DialogueCanvas", false);
        SetActiveIfFound("DialogueBox", false);
        SetActiveIfFound("InteractionPrompt", false);

        SetActiveIfFound("VignetteOverlay", false);
        SetActiveIfFound("VignetteOverlay_UI", false);
    }

    private static GameObject EnsureSinglePlayer()
    {
        PlayerController[] players = FindObjectsOfType<PlayerController>(true);
        GameObject result = players.Length > 0 ? players[0].gameObject : null;

#if UNITY_EDITOR
        if (result == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab != null)
            {
                result = Instantiate(prefab);
                result.name = "Player_LinZhaoying";
            }
        }
#endif

        if (result == null)
        {
            return null;
        }

        foreach (PlayerController player in players)
        {
            if (player != null && player.gameObject != result)
            {
                Destroy(player.gameObject);
            }
        }

        result.name = "Player_LinZhaoying";
        result.SetActive(true);
        DontDestroyOnLoad(result);
        return result;
    }

    private static void MovePlayerToSpawn(GameObject player, Transform spawnPoint)
    {
        player.transform.position = spawnPoint.position;

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        SpriteRenderer renderer = player.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
            renderer.sortingLayerName = "Character";
            renderer.sortingOrder = 50;
            Color color = renderer.color;
            color.a = 1f;
            renderer.color = color;
        }
    }

    private static void EnsureSceneSpawnPoints(string sceneName)
    {
        GameObject sceneRoot = GameObject.Find("SceneRoot");
        if (sceneRoot == null)
        {
            return;
        }

        Transform spawnRoot = sceneRoot.transform.Find("SpawnPoints");
        if (spawnRoot == null)
        {
            GameObject created = new GameObject("SpawnPoints");
            created.transform.SetParent(sceneRoot.transform, false);
            spawnRoot = created.transform;
        }

        switch (sceneName)
        {
            case "Chapter1_TownGate":
                EnsureSpawnPoint(spawnRoot, "Spawn_FromStoneBridge", new Vector3(6f, -2.78f, 0f));
                EnsureSpawnPoint(spawnRoot, "Spawn_Default", new Vector3(-4.5f, -2.78f, 0f));
                break;
            case "Chapter1_StoneBridge":
                EnsureSpawnPoint(spawnRoot, "Spawn_FromTownGate", new Vector3(-6f, -2.78f, 0f));
                EnsureSpawnPoint(spawnRoot, "Spawn_FromGrandmaHouse", new Vector3(6f, -2.78f, 0f));
                break;
            case "Chapter1_GrandmaHouse":
                EnsureSpawnPoint(spawnRoot, "Spawn_FromStoneBridge", new Vector3(-6f, -2.78f, 0f));
                EnsureSpawnPoint(spawnRoot, "Spawn_FromMourningHall", new Vector3(6f, -2.78f, 0f));
                break;
            case "Chapter1_MourningHall":
                EnsureSpawnPoint(spawnRoot, "Spawn_FromGrandmaHouse", new Vector3(-6f, -2.78f, 0f));
                EnsureSpawnPoint(spawnRoot, "Spawn_FromOldWell", new Vector3(6f, -2.78f, 0f));
                break;
            case "Chapter1_OldWell":
                EnsureSpawnPoint(spawnRoot, "Spawn_FromMourningHall", new Vector3(-6f, -2.78f, 0f));
                break;
        }
    }

    private static void EnsureSpawnPoint(Transform parent, string id, Vector3 position)
    {
        Transform spawn = parent.Find(id);
        if (spawn == null)
        {
            GameObject created = new GameObject(id);
            created.transform.SetParent(parent, false);
            spawn = created.transform;
        }

        spawn.position = position;
        SpawnPoint spawnPoint = spawn.GetComponent<SpawnPoint>() ?? spawn.gameObject.AddComponent<SpawnPoint>();
        spawnPoint.Configure(id);
    }

    private static Transform FindSpawnPoint(string spawnPointId)
    {
        if (string.IsNullOrEmpty(spawnPointId))
        {
            return null;
        }

        foreach (SpawnPoint spawnPoint in FindObjectsOfType<SpawnPoint>(true))
        {
            if (spawnPoint.SpawnPointId == spawnPointId)
            {
                return spawnPoint.transform;
            }
        }

        GameObject found = GameObject.Find(spawnPointId);
        return found != null ? found.transform : null;
    }

    private static string GetDefaultSpawnPointId(string sceneName)
    {
        return sceneName == "Chapter1_TownGate" ? "Spawn_Default" : string.Empty;
    }

    private static void RemoveDuplicateRuntimeObjects(Scene activeScene)
    {
        KeepOne<GameStateManager>(activeScene);
        KeepOne<UIManager>(activeScene);
        KeepOne<LanternVisionController>(activeScene);

        Camera[] cameras = FindObjectsOfType<Camera>(true);
        Camera keptMain = null;
        foreach (Camera camera in cameras)
        {
            if (camera != null && camera.CompareTag("MainCamera"))
            {
                keptMain = camera;
                break;
            }
        }

        foreach (Camera camera in cameras)
        {
            if (camera != null && camera.CompareTag("MainCamera") && keptMain != null && camera != keptMain)
            {
                Destroy(camera.gameObject);
            }
        }

        if (keptMain != null)
        {
            foreach (CameraFollow follow in keptMain.GetComponents<CameraFollow>())
            {
                Destroy(follow);
            }

            keptMain.transform.position = new Vector3(0f, 0f, -10f);
            keptMain.transform.rotation = Quaternion.identity;
        }
    }

    private static void KeepOne<T>(Scene activeScene) where T : Component
    {
        T[] items = FindObjectsOfType<T>(true);
        if (items.Length <= 1)
        {
            return;
        }

        T keep = items[0];
        foreach (T item in items)
        {
            if (item != null && item.gameObject.scene == activeScene)
            {
                keep = item;
                break;
            }
        }

        foreach (T item in items)
        {
            if (item != null && item != keep)
            {
                Destroy(item.gameObject);
            }
        }
    }

    private static void SetActiveIfFound(string objectName, bool active)
    {
        GameObject target = FindObjectIncludingInactive(objectName);
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private static GameObject FindObjectIncludingInactive(string objectName)
    {
        foreach (GameObject item in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (item.name == objectName && item.hideFlags == HideFlags.None && item.scene.IsValid())
            {
                return item;
            }
        }

        return null;
    }
}
