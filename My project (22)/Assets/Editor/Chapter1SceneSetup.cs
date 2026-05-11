using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Chapter1SceneSetup
{
    private const string SceneDirectory = "Assets/Scenes/Chapter1";

    private static readonly ChapterSceneAsset[] Scenes =
    {
        new ChapterSceneAsset("TownGate", "Chapter1_TownGate", "Assets/Art/Scenes/Chapter1/GrandmaHouse/GrandmaHouse_Reality.png", "Assets/Art/Scenes/Chapter1/GrandmaHouse/GrandmaHouse_LanternVision.png"),
        new ChapterSceneAsset("StoneBridge", "Chapter1_StoneBridge", "Assets/Art/Scenes/Chapter1/StoneBridge/StoneBridge_Reality.png", "Assets/Art/Scenes/Chapter1/StoneBridge/StoneBridge_LanternVision.png"),
        new ChapterSceneAsset("GrandmaHouse", "Chapter1_GrandmaHouse", "Assets/Art/Scenes/Chapter1/TownGate/TownGate_Reality.png", "Assets/Art/Scenes/Chapter1/TownGate/TownGate_LanternVision.png"),
        new ChapterSceneAsset("MourningHall", "Chapter1_MourningHall", "Assets/Art/Scenes/Chapter1/MourningHall/MourningHall_Reality.png", "Assets/Art/Scenes/Chapter1/MourningHall/MourningHall_LanternVision.png"),
        new ChapterSceneAsset("OldWell", "Chapter1_OldWell", "Assets/Art/Scenes/Chapter1/OldWell/OldWell_Reality.png", "Assets/Art/Scenes/Chapter1/OldWell/OldWell_LanternVision.png")
    };

    [InitializeOnLoadMethod]
    private static void BuildMissingScenesAfterImport()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (!AllBackgroundsExist())
            {
                return;
            }

            foreach (ChapterSceneAsset sceneAsset in Scenes)
            {
                string scenePath = SceneDirectory + "/" + sceneAsset.SceneFileName + ".unity";
                if (!File.Exists(scenePath))
                {
                    BuildScenes();
                    return;
                }
            }
        };
    }

    [MenuItem("JianDeng/Build Chapter 1 Background Scenes")]
    public static void BuildScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Build Chapter 1 Background Scenes can only run in Edit Mode.");
            return;
        }

        Directory.CreateDirectory(SceneDirectory);

        foreach (ChapterSceneAsset sceneAsset in Scenes)
        {
            ConfigureSpriteImport(sceneAsset.RealityPath);
            ConfigureSpriteImport(sceneAsset.LanternVisionPath);
        }

        AssetDatabase.Refresh();

        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
        foreach (ChapterSceneAsset sceneAsset in Scenes)
        {
            string scenePath = SceneDirectory + "/" + sceneAsset.SceneFileName + ".unity";
            BuildSingleScene(sceneAsset, scenePath);
            buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Built Chapter 1 background scenes in " + SceneDirectory);
    }

    private static bool AllBackgroundsExist()
    {
        foreach (ChapterSceneAsset sceneAsset in Scenes)
        {
            if (!File.Exists(sceneAsset.RealityPath) || !File.Exists(sceneAsset.LanternVisionPath))
            {
                return false;
            }
        }

        return true;
    }

    private static void ConfigureSpriteImport(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("Missing chapter background: " + assetPath);
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static void BuildSingleScene(ChapterSceneAsset sceneAsset, string scenePath)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = sceneAsset.SceneFileName;

        Camera camera = CreateCamera();

        GameObject sceneRoot = new GameObject("SceneRoot");
        GameObject background = CreateChild(sceneRoot.transform, "Background");
        GameObject hiddenObjects = CreateChild(sceneRoot.transform, "HiddenObjects");
        GameObject interactables = CreateChild(sceneRoot.transform, "Interactables");
        GameObject vfx = CreateChild(sceneRoot.transform, "VFX");
        GameObject spawnPoints = CreateChild(sceneRoot.transform, "SpawnPoints");
        GameObject sceneTransitions = CreateChild(sceneRoot.transform, "SceneTransitions");

        GameObject realityBg = CreateBackground("Reality_BG", background.transform, sceneAsset.RealityPath, true);
        GameObject lanternBg = CreateBackground("LanternVision_BG", background.transform, sceneAsset.LanternVisionPath, false);
        hiddenObjects.SetActive(false);

        GameObject overlay = CreateOverlay(vfx.transform);
        overlay.SetActive(false);

        SceneBackgroundSet backgroundSet = sceneRoot.AddComponent<SceneBackgroundSet>();
        backgroundSet.realityBackground = realityBg;
        backgroundSet.lanternVisionBackground = lanternBg;
        backgroundSet.hiddenObjects = hiddenObjects;

        LanternVisionController controller = sceneRoot.AddComponent<LanternVisionController>();
        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("backgroundSet").objectReferenceValue = backgroundSet;
        serializedController.FindProperty("lanternVisionOverlay").objectReferenceValue = overlay;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        GameObject playerSpawn = CreateChild(spawnPoints.transform, "PlayerSpawn");
        playerSpawn.transform.position = new Vector3(-6f, -2.5f, 0f);
        CreateChild(sceneTransitions.transform, "ToPrevious");
        CreateChild(sceneTransitions.transform, "ToNext");
        interactables.transform.position = Vector3.zero;

        EditorSceneManager.SaveScene(scene, scenePath);
        EditorSceneManager.CloseScene(scene, true);
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.backgroundColor = new Color32(12, 13, 12, 255);
        return camera;
    }

    private static GameObject CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent);
        child.transform.localPosition = Vector3.zero;
        return child;
    }

    private static GameObject CreateBackground(string name, Transform parent, string spritePath, bool active)
    {
        GameObject background = CreateChild(parent, name);
        SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        renderer.sortingOrder = -100;
        background.SetActive(active);
        return background;
    }

    private static GameObject CreateOverlay(Transform parent)
    {
        GameObject overlay = CreateChild(parent, "LanternVisionOverlay");
        SpriteRenderer renderer = overlay.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateOverlaySprite();
        renderer.color = new Color32(22, 42, 36, 82);
        renderer.sortingOrder = 100;
        overlay.transform.localScale = new Vector3(40f, 24f, 1f);
        return overlay;
    }

    private static Sprite CreateOverlaySprite()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.name = "Runtime_LanternVisionOverlay";
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    private readonly struct ChapterSceneAsset
    {
        public readonly string DisplayName;
        public readonly string SceneFileName;
        public readonly string RealityPath;
        public readonly string LanternVisionPath;

        public ChapterSceneAsset(string displayName, string sceneFileName, string realityPath, string lanternVisionPath)
        {
            DisplayName = displayName;
            SceneFileName = sceneFileName;
            RealityPath = realityPath;
            LanternVisionPath = lanternVisionPath;
        }
    }
}
