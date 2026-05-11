using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public sealed class SceneTransition2D : MonoBehaviour, IInteractable, IInteractionPrompt
{
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnPointId;
    [SerializeField] private string promptText = "[E] 前往";

    private static bool subscribed;
    private static string pendingSpawnPointId;
    private static GameObject persistentPlayer;
    private static SceneTransitionPostLoadRunner runner;
    private bool loading;

    public static string PendingSpawnPointId => pendingSpawnPointId;
    public string PromptText => string.IsNullOrEmpty(promptText) ? "[E] 前往" : promptText;
    public bool CanShowPrompt => !string.IsNullOrEmpty(targetSceneName);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SubscribeSceneLoaded()
    {
        if (subscribed)
        {
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        subscribed = true;
    }

    public void SetTargetScene(string sceneName)
    {
        SetTarget(sceneName, string.Empty);
    }

    public void SetTarget(string sceneName, string spawnPointId)
    {
        targetSceneName = sceneName;
        targetSpawnPointId = spawnPointId;
        promptText = GetPromptForScene(sceneName);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Scene transitions are E interactions. InteractionDetector handles the prompt and input.
    }

    public void Interact(PlayerController player)
    {
        if (loading || string.IsNullOrEmpty(targetSceneName))
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        persistentPlayer = player.gameObject;
        persistentPlayer.name = "Player_LinZhaoying";
        persistentPlayer.SetActive(true);
        Object.DontDestroyOnLoad(persistentPlayer);

        pendingSpawnPointId = targetSpawnPointId;
        loading = true;
        Debug.Log("Scene transition to: " + targetSceneName + ", spawn: " + pendingSpawnPointId);
        EnsureRunner();
        SceneManager.LoadScene(targetSceneName);
    }

    private static string GetPromptForScene(string sceneName)
    {
        switch (sceneName)
        {
            case "Chapter1_StoneBridge":
            case "Scene_Ch01_StoneBridge":
                return "[E] 前往石桥";
            case "Chapter1_GrandmaHouse":
            case "Scene_Ch01_GrandmaHouseGate":
                return "[E] 前往外婆家";
            case "Chapter1_TownGate":
                return "[E] 返回镇口";
            case "Chapter1_MourningHall":
                return "[E] 前往灵堂";
            case "Chapter1_OldWell":
                return "[E] 前往老井";
            default:
                return "[E] 前往";
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ChapterSceneInitializer owns all post-load positioning and reality-mode reset.
        // Keeping this callback passive prevents two scene-load systems from fighting.
    }

    private static IEnumerator ApplyAfterSceneRuntimeFix(Scene scene)
    {
        yield return null;

        GameObject player = ResolvePlayer();
        Transform spawnPoint = FindSpawnPoint(pendingSpawnPointId);

        if (player != null)
        {
            player.SetActive(true);
            if (spawnPoint != null)
            {
                player.transform.position = spawnPoint.position;
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

            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
        }

        ResetLanternVision();
        LogPostLoad(scene, player, spawnPoint);
    }

    public static string ConsumePendingSpawnPointId()
    {
        string result = pendingSpawnPointId;
        pendingSpawnPointId = string.Empty;
        return result;
    }

    public static void SetPendingSpawnPoint(string spawnPointId)
    {
        pendingSpawnPointId = spawnPointId;
    }

    private static void EnsureRunner()
    {
        if (runner != null)
        {
            return;
        }

        GameObject runnerObject = new GameObject("SceneTransitionPostLoadRunner");
        Object.DontDestroyOnLoad(runnerObject);
        runner = runnerObject.AddComponent<SceneTransitionPostLoadRunner>();
    }

    private static GameObject ResolvePlayer()
    {
        PlayerController[] players = Object.FindObjectsOfType<PlayerController>(true);
        GameObject result = persistentPlayer;

        if (result == null && players.Length > 0)
        {
            result = players[0].gameObject;
            persistentPlayer = result;
            Object.DontDestroyOnLoad(result);
        }

        foreach (PlayerController player in players)
        {
            if (result != null && player.gameObject != result)
            {
                Object.Destroy(player.gameObject);
            }
        }

        return result;
    }

    private static Transform FindSpawnPoint(string spawnPointId)
    {
        if (string.IsNullOrEmpty(spawnPointId))
        {
            return null;
        }

        foreach (SpawnPoint spawnPoint in Object.FindObjectsOfType<SpawnPoint>(true))
        {
            if (spawnPoint.SpawnPointId == spawnPointId)
            {
                return spawnPoint.transform;
            }
        }

        GameObject found = GameObject.Find(spawnPointId);
        return found != null ? found.transform : null;
    }

    private static void ResetLanternVision()
    {
        LanternVisionController controller = Object.FindObjectOfType<LanternVisionController>();
        if (controller != null)
        {
            controller.ForceRealityMode();
        }

        GameObject overlay = FindObjectIncludingInactive("LanternVisionOverlay_UI");
        if (overlay != null)
        {
            overlay.SetActive(false);
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

    private static void LogPostLoad(Scene scene, GameObject player, Transform spawnPoint)
    {
        SpriteRenderer renderer = player != null ? player.GetComponent<SpriteRenderer>() : null;

        Debug.Log("Scene loaded: " + scene.name);
        Debug.Log("Found Player: " + (player != null));
        Debug.Log("Player position: " + (player != null ? player.transform.position.ToString() : "null"));
        Debug.Log("Found SpawnPoint: " + (spawnPoint != null));
        Debug.Log("SpawnPoint position: " + (spawnPoint != null ? spawnPoint.position.ToString() : "null"));
        Debug.Log("Player SpriteRenderer enabled: " + (renderer != null && renderer.enabled));
        Debug.Log("Player activeSelf / activeInHierarchy: " + (player != null ? player.activeSelf + " / " + player.activeInHierarchy : "null"));
    }
}

public sealed class SceneTransitionPostLoadRunner : MonoBehaviour
{
}
