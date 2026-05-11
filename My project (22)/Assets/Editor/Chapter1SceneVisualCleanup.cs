using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Chapter1SceneVisualCleanup
{
    private const string ScenePath = "Assets/Scenes/Chapter1/Chapter1_TownGate.unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player_LinZhaoying.prefab";
    private const string OverlaySpritePath = "Assets/Art/VFX/LanternVision/45_vignette_overlay.png";

    private static readonly string[] SortingLayers =
    {
        "Background",
        "HiddenBackground",
        "Props",
        "Character",
        "Ghost",
        "VFX",
        "UI"
    };

    [InitializeOnLoadMethod]
    private static void AutoCleanup()
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

            if (!File.Exists(ScenePath) || File.ReadAllText(ScenePath).Contains("m_Name: Chapter1VisualCleanupMarker"))
            {
                return;
            }

            CleanupTownGateScene();
        };
    }

    [MenuItem("JianDeng/Cleanup Chapter 1 Scene Visuals")]
    public static void CleanupTownGateScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Cleanup Chapter 1 Scene Visuals can only run in Edit Mode.");
            return;
        }

        if (!File.Exists(ScenePath))
        {
            Debug.LogWarning("Missing scene: " + ScenePath);
            return;
        }

        EnsureSortingLayers();

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform sceneRoot = FindOrCreate("SceneRoot").transform;
        Transform background = FindOrCreateChild(sceneRoot, "Background");
        Transform hiddenObjects = FindOrCreateChild(sceneRoot, "HiddenObjects");
        Transform interactables = FindOrCreateChild(sceneRoot, "Interactables");
        Transform vfx = FindOrCreateChild(sceneRoot, "VFX");
        Transform sceneTransitions = FindOrCreateChild(sceneRoot, "SceneTransitions");
        Transform playerSpawn = FindOrCreateChild(sceneRoot, "PlayerSpawn");

        Camera camera = SetupCamera();
        SetupBackgrounds(background, sceneRoot, camera);
        GameObject player = SetupPlayer(playerSpawn);
        SetupGround(player);
        RemoveCameraFollow(camera);
        SetupOverlayCanvas(sceneRoot);
        SetupTitleCanvas(sceneRoot);
        CleanupVisibleTestBlocks(interactables, sceneTransitions);
        CleanupGeneratedSprites(hiddenObjects, interactables, vfx, player);
        SetupSceneBackgroundReferences(sceneRoot, background, hiddenObjects);
        EnsureMarker(sceneRoot);

        hiddenObjects.gameObject.SetActive(false);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Cleaned Chapter 1 TownGate scene visuals.");
    }

    private static void EnsureSortingLayers()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("m_SortingLayers");

        foreach (string layerName in SortingLayers)
        {
            if (SortingLayer.NameToID(layerName) != 0 || layerName == "Default")
            {
                continue;
            }

            bool exists = false;
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == layerName)
                {
                    exists = true;
                    break;
                }
            }

            if (exists)
            {
                continue;
            }

            layers.InsertArrayElementAtIndex(layers.arraySize);
            SerializedProperty layer = layers.GetArrayElementAtIndex(layers.arraySize - 1);
            layer.FindPropertyRelative("name").stringValue = layerName;
            layer.FindPropertyRelative("uniqueID").intValue = layerName.GetHashCode();
            layer.FindPropertyRelative("locked").boolValue = false;
        }

        tagManager.ApplyModifiedPropertiesWithoutUndo();
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
        camera.backgroundColor = new Color32(10, 10, 10, 255);
        return camera;
    }

    private static void SetupBackgrounds(Transform background, Transform sceneRoot, Camera camera)
    {
        Transform reality = background.Find("Reality_BG");
        Transform lantern = background.Find("LanternVision_BG");
        SetupBackgroundRenderer(reality, "Background", 0, camera, true);
        SetupBackgroundRenderer(lantern, "Background", 1, camera, false);

        SceneBackgroundSet set = sceneRoot.GetComponent<SceneBackgroundSet>() ?? sceneRoot.gameObject.AddComponent<SceneBackgroundSet>();
        set.realityBackground = reality != null ? reality.gameObject : null;
        set.lanternVisionBackground = lantern != null ? lantern.gameObject : null;
    }

    private static void SetupBackgroundRenderer(Transform target, string layer, int order, Camera camera, bool active)
    {
        if (target == null)
        {
            return;
        }

        target.gameObject.SetActive(active);
        target.localPosition = new Vector3(0f, 0f, 10f);
        SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null)
        {
            return;
        }

        renderer.sortingLayerName = layer;
        renderer.sortingOrder = order;

        float viewHeight = camera.orthographicSize * 2f;
        float viewWidth = viewHeight * (16f / 9f);
        Vector2 spriteSize = renderer.sprite.bounds.size;
        float scale = Mathf.Max(viewWidth / spriteSize.x, viewHeight / spriteSize.y) * 1.02f;
        target.localScale = new Vector3(scale, scale, 1f);
    }

    private static GameObject SetupPlayer(Transform playerSpawn)
    {
        GameObject player = GameObject.Find("Player_LinZhaoying");
        if (player == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            player = prefab != null ? PrefabUtility.InstantiatePrefab(prefab) as GameObject : new GameObject("Player_LinZhaoying");
            player.name = "Player_LinZhaoying";
        }

        player.transform.localScale = Vector3.one * 1.12f;
        float groundTop = -2.78f;
        CapsuleCollider2D capsule = player.GetComponent<CapsuleCollider2D>();
        float footOffset = capsule != null ? capsule.offset.y - capsule.size.y * 0.5f : 0f;
        player.transform.position = new Vector3(-5f, groundTop - footOffset * player.transform.localScale.y, 0f);
        playerSpawn.position = player.transform.position;

        SpriteRenderer renderer = player.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = "Character";
            renderer.sortingOrder = 50;
        }

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        return player;
    }

    private static void SetupGround(GameObject player)
    {
        GameObject ground = GameObject.Find("GroundCollider") ?? new GameObject("GroundCollider");
        ground.transform.position = new Vector3(0f, -3.05f, 0f);
        BoxCollider2D collider = ground.GetComponent<BoxCollider2D>() ?? ground.AddComponent<BoxCollider2D>();
        collider.isTrigger = false;
        collider.offset = Vector2.zero;
        collider.size = new Vector2(18f, 0.5f);

        SpriteRenderer renderer = ground.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }
    }

    private static void RemoveCameraFollow(Camera camera)
    {
        CameraFollow follow = camera.GetComponent<CameraFollow>();
        if (follow != null)
        {
            Object.DestroyImmediate(follow);
        }
    }

    private static void SetupOverlayCanvas(Transform sceneRoot)
    {
        GameObject canvas = GameObject.Find("Chapter1_UI_Canvas") ?? new GameObject("Chapter1_UI_Canvas");
        canvas.transform.SetParent(sceneRoot, false);
        Canvas canvasComponent = canvas.GetComponent<Canvas>() ?? canvas.AddComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasComponent.sortingLayerName = "UI";
        canvasComponent.sortingOrder = 0;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.AddComponent<GraphicRaycaster>();
        }

        GameObject overlay = FindOrCreateUiChild(canvas.transform, "LanternVisionOverlay");
        Image image = overlay.GetComponent<Image>() ?? overlay.AddComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(OverlaySpritePath);
        image.color = new Color(1f, 1f, 1f, 0.42f);
        image.raycastTarget = false;
        RectTransform rect = overlay.GetComponent<RectTransform>();
        StretchFullScreen(rect);
        overlay.SetActive(false);

        LanternVisionController controller = sceneRoot.GetComponent<LanternVisionController>() ?? sceneRoot.gameObject.AddComponent<LanternVisionController>();
        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("lanternVisionOverlay").objectReferenceValue = overlay;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        DisableSpriteOverlay(sceneRoot);
    }

    private static void SetupTitleCanvas(Transform sceneRoot)
    {
        GameObject canvas = GameObject.Find("Chapter1_UI_Canvas");
        if (canvas == null)
        {
            return;
        }

        GameObject title = FindOrCreateUiChild(canvas.transform, "ChapterTitle");
        TextMeshProUGUI text = title.GetComponent<TextMeshProUGUI>() ?? title.AddComponent<TextMeshProUGUI>();
        text.text = "第一章：归镇\n镇口";
        text.fontSize = 30f;
        text.color = new Color(0.82f, 0.84f, 0.82f, 0.8f);
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.TopLeft;

        RectTransform rect = title.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(40f, -34f);
        rect.sizeDelta = new Vector2(360f, 110f);
    }

    private static void CleanupVisibleTestBlocks(params Transform[] roots)
    {
        foreach (Transform root in roots)
        {
            if (root == null)
            {
                continue;
            }

            foreach (Transform child in root)
            {
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                if (renderer == null)
                {
                    continue;
                }

                if (renderer.sprite == null || child.name.Contains("Test") || child.name.Contains("Cube") || child.name.Contains("Door"))
                {
                    renderer.enabled = false;
                    if (!child.name.Contains("Interactable"))
                    {
                        child.name = "Test_Interactable";
                    }
                }
            }
        }
    }

    private static void CleanupGeneratedSprites(Transform hidden, Transform interactables, Transform vfx, GameObject player)
    {
        float playerHeight = 2.64f * player.transform.localScale.y;
        foreach (SpriteRenderer renderer in Object.FindObjectsOfType<SpriteRenderer>(true))
        {
            string name = renderer.gameObject.name;
            if (name == "Reality_BG" || name == "LanternVision_BG")
            {
                continue;
            }

            if (name.Contains("PaperEffigy_Normal"))
            {
                renderer.sortingLayerName = "Props";
                renderer.sortingOrder = 30;
                renderer.transform.SetParent(interactables, true);
                ScaleToHeight(renderer.transform, renderer, playerHeight * 1.02f);
                renderer.transform.position = new Vector3(-2.8f, -2.78f, 0f);
            }
            else if (name.Contains("Afterimage") || name.Contains("Ghost") || name.Contains("Altered"))
            {
                renderer.sortingLayerName = "Ghost";
                renderer.sortingOrder = 40;
                renderer.color = WithAlpha(renderer.color, 0.48f);
                renderer.transform.SetParent(hidden, true);
                ScaleToHeight(renderer.transform, renderer, playerHeight * 1.05f);
                if (renderer.transform.position.x < -4.2f)
                {
                    renderer.transform.position = new Vector3(-2.2f, -1.65f, 0f);
                }

                if (renderer.GetComponent<HiddenInLanternView>() == null)
                {
                    renderer.gameObject.AddComponent<HiddenInLanternView>();
                }
            }
            else if (name.Contains("HiddenText") || name.Contains("Footprints") || name.Contains("WaterReflection") || name.Contains("CommonVFX_"))
            {
                renderer.sortingLayerName = "VFX";
                renderer.sortingOrder = name.Contains("Vignette") ? 100 : 60;
                renderer.color = WithAlpha(renderer.color, name.Contains("Vignette") ? 0.35f : 0.55f);
                if (name.Contains("HiddenText") || name.Contains("Footprints") || name.Contains("WaterReflection") || name.Contains("AfterimageFlash"))
                {
                    renderer.transform.SetParent(hidden, true);
                }
            }
            else if (renderer.transform.IsChildOf(interactables))
            {
                renderer.sortingLayerName = "Props";
                renderer.sortingOrder = Mathf.Clamp(renderer.sortingOrder, 0, 30);
            }
        }

        if (hidden != null)
        {
            hidden.gameObject.SetActive(false);
        }
    }

    private static void SetupSceneBackgroundReferences(Transform sceneRoot, Transform background, Transform hiddenObjects)
    {
        SceneBackgroundSet set = sceneRoot.GetComponent<SceneBackgroundSet>() ?? sceneRoot.gameObject.AddComponent<SceneBackgroundSet>();
        set.realityBackground = background.Find("Reality_BG") != null ? background.Find("Reality_BG").gameObject : null;
        set.lanternVisionBackground = background.Find("LanternVision_BG") != null ? background.Find("LanternVision_BG").gameObject : null;
        set.hiddenObjects = hiddenObjects.gameObject;
        set.SetLanternVision(false);

        LanternVisionController controller = sceneRoot.GetComponent<LanternVisionController>() ?? sceneRoot.gameObject.AddComponent<LanternVisionController>();
        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("backgroundSet").objectReferenceValue = set;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void DisableSpriteOverlay(Transform sceneRoot)
    {
        foreach (Transform child in sceneRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != "LanternVisionOverlay")
            {
                continue;
            }

            if (child.GetComponent<Image>() != null)
            {
                continue;
            }

            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }

            child.gameObject.SetActive(false);
            child.name = "LanternVisionOverlay_Sprite_Disabled";
        }
    }

    private static void EnsureMarker(Transform sceneRoot)
    {
        if (GameObject.Find("Chapter1VisualCleanupMarker") != null)
        {
            return;
        }

        GameObject marker = new GameObject("Chapter1VisualCleanupMarker");
        marker.transform.SetParent(sceneRoot, false);
        marker.hideFlags = HideFlags.HideInHierarchy;
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

    private static GameObject FindOrCreate(string name)
    {
        return GameObject.Find(name) ?? new GameObject(name);
    }

    private static GameObject FindOrCreateUiChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            return child.gameObject;
        }

        GameObject created = new GameObject(name);
        created.transform.SetParent(parent, false);
        created.AddComponent<RectTransform>();
        return created;
    }

    private static void StretchFullScreen(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void ScaleToHeight(Transform transform, SpriteRenderer renderer, float targetHeight)
    {
        if (renderer.sprite == null || renderer.sprite.bounds.size.y <= 0f)
        {
            return;
        }

        float scale = targetHeight / renderer.sprite.bounds.size.y;
        transform.localScale = Vector3.one * scale;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
