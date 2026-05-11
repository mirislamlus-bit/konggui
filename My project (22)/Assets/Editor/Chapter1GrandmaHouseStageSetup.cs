using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Chapter1GrandmaHouseStageSetup
{
    private const string ScenePath = "Assets/Scenes/Chapter1/Chapter1_GrandmaHouse.unity";
    private const string RealityBgPath = "Assets/Art/Scenes/Chapter1/GrandmaHouse/GrandmaHouse_Reality.png";
    private const string LanternBgPath = "Assets/Art/Scenes/Chapter1/GrandmaHouse/GrandmaHouse_LanternVision.png";
    private const string FallbackHiddenTextPath = "Assets/Art/VFX/LanternVision/32_hidden_text_sheet_01.png";
    private const string FallbackFootprintsPath = "Assets/Art/VFX/LanternVision/33_footprints_guide_sheet_01.png";

    [MenuItem("JianDeng/Setup Chapter 1 GrandmaHouse Stage")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Setup Chapter 1 GrandmaHouse Stage can only run in Edit Mode.");
            return;
        }

        Directory.CreateDirectory("Assets/Scenes/Chapter1");

        Scene scene = File.Exists(ScenePath)
            ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject sceneRoot = FindOrCreate("SceneRoot");
        Transform background = FindOrCreateChild(sceneRoot.transform, "Background");
        Transform hiddenObjects = FindOrCreateChild(sceneRoot.transform, "HiddenObjects");
        Transform interactables = FindOrCreateChild(sceneRoot.transform, "Interactables");
        Transform vfx = FindOrCreateChild(sceneRoot.transform, "VFX");
        FindOrCreateChild(sceneRoot.transform, "SpawnPoints");
        FindOrCreateChild(sceneRoot.transform, "SceneTransitions");

        Camera camera = SetupCamera();
        GameObject reality = SetupBackground(background, "Reality_BG", RealityBgPath, true, 0, camera);
        GameObject lantern = File.Exists(LanternBgPath)
            ? SetupBackground(background, "LanternVision_BG", LanternBgPath, false, 1, camera)
            : SetupBackground(background, "LanternVision_BG", RealityBgPath, false, 1, camera);

        if (!File.Exists(LanternBgPath) && lantern != null)
        {
            SpriteRenderer renderer = lantern.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = new Color(0.55f, 0.65f, 0.6f, 1f);
            }
        }

        SetupSceneControllers(sceneRoot, reality, lantern, hiddenObjects.gameObject);
        SetupCanvas(sceneRoot.transform);
        SetupManagers();
        SetupPlayer();
        SetupInteractables(interactables);
        SetupHiddenObjects(hiddenObjects);
        SetupVfx(vfx);
        hiddenObjects.gameObject.SetActive(false);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("GrandmaHouse stage setup complete: " + ScenePath);
    }

    private static Camera SetupCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.GetComponent<Camera>() ?? cameraObject.AddComponent<Camera>();
        }

        camera.orthographic = true;
        camera.orthographicSize = 5.2f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        return camera;
    }

    private static GameObject SetupBackground(Transform parent, string name, string spritePath, bool active, int order, Camera camera)
    {
        GameObject item = FindOrCreateChild(parent, name).gameObject;
        item.SetActive(active);
        item.transform.position = new Vector3(0f, 0f, 10f);

        SpriteRenderer renderer = item.GetComponent<SpriteRenderer>() ?? item.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = order;
        renderer.color = Color.white;

        if (renderer.sprite != null)
        {
            float viewHeight = camera.orthographicSize * 2f;
            float viewWidth = viewHeight * (16f / 9f);
            Vector2 spriteSize = renderer.sprite.bounds.size;
            float scale = Mathf.Max(viewWidth / spriteSize.x, viewHeight / spriteSize.y) * 1.02f;
            item.transform.localScale = Vector3.one * scale;
        }

        return item;
    }

    private static void SetupSceneControllers(GameObject root, GameObject reality, GameObject lantern, GameObject hiddenObjects)
    {
        SceneBackgroundSet backgroundSet = root.GetComponent<SceneBackgroundSet>() ?? root.AddComponent<SceneBackgroundSet>();
        backgroundSet.realityBackground = reality;
        backgroundSet.lanternVisionBackground = lantern;
        backgroundSet.hiddenObjects = hiddenObjects;

        LanternVisionController controller = root.GetComponent<LanternVisionController>() ?? root.AddComponent<LanternVisionController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("backgroundSet").objectReferenceValue = backgroundSet;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        backgroundSet.SetLanternVision(false);
    }

    private static void SetupCanvas(Transform sceneRoot)
    {
        GameObject canvasObject = FindOrCreate("Chapter1_UI_Canvas");
        canvasObject.transform.SetParent(sceneRoot, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>() ?? canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingLayerName = "UI";
        canvas.sortingOrder = 0;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>() ?? canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        if (canvasObject.GetComponent<GraphicRaycaster>() == null)
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject lanternOverlay = CreateUiImage(canvasObject.transform, "LanternVisionOverlay", "Assets/Art/VFX/LanternVision/31_lantern_vision_filter.png", 0.45f, false);
        GameObject vignetteOverlay = CreateUiImage(canvasObject.transform, "VignetteOverlay", "Assets/Art/VFX/LanternVision/45_vignette_overlay.png", 0.18f, true);

        LanternVisionController controller = sceneRoot.GetComponent<LanternVisionController>();
        if (controller != null)
        {
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("lanternVisionOverlay").objectReferenceValue = lanternOverlay;
            serialized.FindProperty("vignetteOverlay").objectReferenceValue = vignetteOverlay.GetComponent<Image>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static GameObject CreateUiImage(Transform parent, string name, string spritePath, float alpha, bool active)
    {
        GameObject item = FindOrCreateChild(parent, name).gameObject;
        RectTransform rect = item.GetComponent<RectTransform>() ?? item.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = item.GetComponent<Image>() ?? item.AddComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        image.color = new Color(1f, 1f, 1f, alpha);
        image.raycastTarget = false;
        item.SetActive(active);
        return item;
    }

    private static void SetupPlayer()
    {
        GameObject player = GameObject.Find("Player_LinZhaoying");
        if (player == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player_LinZhaoying.prefab");
            if (prefab != null)
            {
                player = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                player.name = "Player_LinZhaoying";
            }
        }

        if (player == null)
        {
            return;
        }

        player.transform.position = new Vector3(-5f, -2.7f, 0f);
        SpriteRenderer renderer = player.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = "Character";
            renderer.sortingOrder = 50;
        }

        if (player.GetComponent<InteractionDetector>() == null)
        {
            player.AddComponent<InteractionDetector>();
        }
    }

    private static void SetupManagers()
    {
        if (Object.FindObjectOfType<GameStateManager>() == null)
        {
            new GameObject("GameStateManager").AddComponent<GameStateManager>();
        }

        if (Object.FindObjectOfType<InventoryManager>() == null)
        {
            new GameObject("InventoryManager").AddComponent<InventoryManager>();
        }

        if (Object.FindObjectOfType<DialogueManager>() == null)
        {
            new GameObject("DialogueManager").AddComponent<DialogueManager>();
        }
    }

    private static void SetupInteractables(Transform parent)
    {
        CreateTriggerOnly(parent, "Door_Interactable", new Vector3(-6.2f, -1.65f, 0f), new Vector2(1.2f, 2.2f));
        CreateTriggerOnly(parent, "OldChair_Interactable", new Vector3(-2.4f, -2.2f, 0f), new Vector2(1.1f, 1.1f));
        CreateTriggerOnly(parent, "IncenseBurner_Interactable", new Vector3(0.6f, -2.2f, 0f), new Vector2(1.1f, 1.1f));

        GameObject lantern = CreateSprite(parent, "BlackLantern_Interactable", "Assets/Art/Props/Lantern/17_black_lantern_unlit.png", new Vector3(2.4f, -2.15f, 0f), "Props", 30, 0.32f, 1f);
        BlackLanternInteractable blackLantern = lantern.GetComponent<BlackLanternInteractable>() ?? lantern.AddComponent<BlackLanternInteractable>();
        SerializedObject serializedLantern = new SerializedObject(blackLantern);
        serializedLantern.FindProperty("blackLanternItem").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/ScriptableObjects/Items/BlackLantern_Unlit.asset");
        serializedLantern.ApplyModifiedPropertiesWithoutUndo();

        BoxCollider2D collider = lantern.GetComponent<BoxCollider2D>() ?? lantern.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1.2f, 1.8f);
    }

    private static void SetupHiddenObjects(Transform parent)
    {
        CreateHidden(parent, "HiddenText_Test", ResolveExisting("Assets/Art/VFX/LanternVision/32_hidden_text_01.png", FallbackHiddenTextPath), new Vector3(0f, -0.6f, 0f), "VFX", 60, 0.45f, 0.55f);
        CreateHidden(parent, "GrandmaAfterimage_Test", "Assets/Art/Characters/Ghosts/13_grandmother_afterimage.png", new Vector3(2.0f, -1.45f, 0f), "Ghost", 40, 0.42f, 0.48f);
        CreateHidden(parent, "Footprints_Test", ResolveExisting("Assets/Art/VFX/LanternVision/33_footprints_guide_01.png", FallbackFootprintsPath), new Vector3(-1.2f, -2.55f, 0f), "VFX", 60, 0.38f, 0.55f);

        CreateHidden(parent, "GrandmaAfterimage", "Assets/Art/Characters/Ghosts/13_grandmother_afterimage.png", new Vector3(2.5f, -1.45f, 0f), "Ghost", 40, 0.4f, 0.48f);
        CreateHidden(parent, "HiddenText_GrandmaHouse", ResolveExisting("Assets/Art/VFX/LanternVision/32_hidden_text_01.png", FallbackHiddenTextPath), new Vector3(0.9f, -0.8f, 0f), "VFX", 60, 0.45f, 0.55f);
        CreateHidden(parent, "GuideLine_GrandmaHouse", ResolveExisting("Assets/Art/VFX/LanternVision/33_footprints_guide_01.png", FallbackFootprintsPath), new Vector3(-0.9f, -2.55f, 0f), "VFX", 60, 0.38f, 0.55f);
    }

    private static void SetupVfx(Transform parent)
    {
        CreateLoopVfx(parent, "Dust_GrandmaHouse", "Assets/Art/VFX/Atmosphere/39_floating_dust", new Vector3(0f, -0.1f, 0f), 0.8f, 0.4f, 6f);
        CreateLoopVfx(parent, "IncenseSmoke_GrandmaHouse", "Assets/Art/VFX/Atmosphere/41_incense_smoke", new Vector3(0.6f, -1.55f, 0f), 0.35f, 0.62f, 7f);
    }

    private static GameObject CreateHidden(Transform parent, string name, string path, Vector3 position, string layer, int order, float scale, float alpha)
    {
        GameObject item = CreateSprite(parent, name, path, position, layer, order, scale, alpha);
        if (item.GetComponent<HiddenInLanternView>() == null)
        {
            item.AddComponent<HiddenInLanternView>();
        }
        item.SetActive(false);
        return item;
    }

    private static GameObject CreateSprite(Transform parent, string name, string path, Vector3 position, string layer, int order, float scale, float alpha)
    {
        Transform old = parent.Find(name);
        GameObject item = old != null ? old.gameObject : new GameObject(name);
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localScale = Vector3.one * scale;
        SpriteRenderer renderer = item.GetComponent<SpriteRenderer>() ?? item.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        renderer.sortingLayerName = layer;
        renderer.sortingOrder = order;
        renderer.color = new Color(1f, 1f, 1f, alpha);
        return item;
    }

    private static void CreateTriggerOnly(Transform parent, string name, Vector3 position, Vector2 size)
    {
        GameObject item = FindOrCreateChild(parent, name).gameObject;
        item.transform.localPosition = position;
        SpriteRenderer renderer = item.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }
        BoxCollider2D collider = item.GetComponent<BoxCollider2D>() ?? item.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = size;
    }

    private static void CreateLoopVfx(Transform parent, string name, string folder, Vector3 position, float scale, float alpha, float fps)
    {
        string[] paths = Directory.Exists(folder)
            ? Directory.GetFiles(folder, "*.png").OrderBy(path => path).Select(path => path.Replace("\\", "/")).ToArray()
            : new string[0];

        if (paths.Length == 0)
        {
            return;
        }

        GameObject item = CreateSprite(parent, name, paths[0], position, "VFX", 60, scale, alpha);
        SimpleLoopVFX loop = item.GetComponent<SimpleLoopVFX>() ?? item.AddComponent<SimpleLoopVFX>();
        SerializedObject serialized = new SerializedObject(loop);
        SerializedProperty frames = serialized.FindProperty("frames");
        frames.arraySize = paths.Length;
        for (int i = 0; i < paths.Length; i++)
        {
            frames.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]);
        }
        serialized.FindProperty("framesPerSecond").floatValue = fps;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject FindOrCreate(string name)
    {
        return GameObject.Find(name) ?? new GameObject(name);
    }

    private static Transform FindOrCreateChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            return child;
        }
        GameObject created = new GameObject(name);
        created.transform.SetParent(parent, false);
        return created.transform;
    }

    private static string ResolveExisting(string preferred, string fallback)
    {
        return File.Exists(preferred) ? preferred : fallback;
    }
}
