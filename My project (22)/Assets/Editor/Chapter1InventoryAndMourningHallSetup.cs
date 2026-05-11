using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Chapter1InventoryAndMourningHallSetup
{
    private const string MourningHallScenePath = "Assets/Scenes/Chapter1/Chapter1_MourningHall.unity";
    private const string OldWellScenePath = "Assets/Scenes/Chapter1/Chapter1_OldWell.unity";
    private const string UnlitItemPath = "Assets/ScriptableObjects/Items/BlackLantern_Unlit.asset";
    private const string LitItemPath = "Assets/ScriptableObjects/Items/BlackLantern_Lit.asset";

    [MenuItem("JianDeng/Setup Inventory Items And MourningHall Puzzle")]
    [MenuItem("JianDeng/Setup Chapter 1 Puzzle Scenes")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Setup Inventory Items And MourningHall Puzzle can only run in Edit Mode.");
            return;
        }

        ItemData unlit = CreateItem(
            UnlitItemPath,
            "BlackLantern_Unlit",
            "\u9ed1\u706f\uff08\u672a\u70b9\u71c3\uff09",
            "\u4e00\u76cf\u6c89\u91cd\u7684\u9ed1\u706f\uff0c\u706f\u82af\u8fd8\u6ca1\u6709\u4eae\u3002",
            "Assets/Art/Props/Lantern/17_black_lantern_unlit.png");

        ItemData lit = CreateItem(
            LitItemPath,
            "BlackLantern_Lit",
            "\u9ed1\u706f\uff08\u5df2\u70b9\u71c3\uff09",
            "\u706f\u706b\u5f88\u4f4e\uff0c\u5374\u80fd\u7167\u51fa\u4e0d\u8be5\u88ab\u770b\u89c1\u7684\u4e1c\u897f\u3002",
            "Assets/Art/Props/Lantern/18_black_lantern_lit.png");

        SetupMourningHall(lit);
        SetupOldWell();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static ItemData CreateItem(string assetPath, string id, string itemName, string description, string iconPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(item, assetPath);
        }

        item.itemId = id;
        item.itemName = itemName;
        item.description = description;
        item.icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        item.itemType = ItemType.Lantern;
        EditorUtility.SetDirty(item);
        return item;
    }

    private static void SetupMourningHall(ItemData litItem)
    {
        Directory.CreateDirectory("Assets/Scenes/Chapter1");

        Scene scene = File.Exists(MourningHallScenePath)
            ? EditorSceneManager.OpenScene(MourningHallScenePath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = FindOrCreate("SceneRoot");
        Transform background = FindOrCreateChild(root.transform, "Background");
        Transform hidden = FindOrCreateChild(root.transform, "HiddenObjects");
        Transform interactables = FindOrCreateChild(root.transform, "Interactables");
        FindOrCreateChild(root.transform, "VFX");
        FindOrCreateChild(root.transform, "SpawnPoints");
        FindOrCreateChild(root.transform, "SceneTransitions");

        Camera camera = SetupCamera();
        GameObject reality = SetupBackground(background, "Reality_BG", "Assets/Art/Scenes/Chapter1/MourningHall/MourningHall_Reality.png", true, 0, camera);
        GameObject lantern = SetupBackground(background, "LanternVision_BG", "Assets/Art/Scenes/Chapter1/MourningHall/MourningHall_LanternVision.png", false, 1, camera);
        SetupSceneControllers(root, reality, lantern, hidden.gameObject);
        SetupManagers();
        SetupCanvas(root.transform);
        SetupPlayer();

        OfferingPuzzleManager puzzle = root.GetComponent<OfferingPuzzleManager>() ?? root.AddComponent<OfferingPuzzleManager>();
        CreateOffering(interactables, "Apple_Interactable", "Offering_Apple", "Assets/Art/Props/Offerings/22_apple_single.png", new Vector3(-1.8f, -2.0f, 0f), puzzle);
        CreateOffering(interactables, "Cake_Interactable", "Offering_Cake", "Assets/Art/Props/Offerings/23_pastry_single.png", new Vector3(-0.95f, -2.0f, 0f), puzzle);
        CreateOffering(interactables, "WineCup_Interactable", "Offering_WineCup", "Assets/Art/Props/Offerings/24_wine_cup_single.png", new Vector3(-0.1f, -2.0f, 0f), puzzle);
        CreateOffering(interactables, "IncenseBurner_Interactable", "Offering_IncenseBurner", "Assets/Art/Props/Offerings/25_incense_burner_single.png", new Vector3(0.8f, -2.0f, 0f), puzzle);
        SpriteRenderer candleRenderer = CreateOffering(interactables, "Candle_Interactable", "Offering_Candle", "Assets/Art/Props/Offerings/26_white_candle_unlit.png", new Vector3(1.65f, -2.0f, 0f), puzzle);

        GameObject afterimageFlash = CreateHidden(hidden, "AfterimageFlash", FirstPng("Assets/Art/VFX/LanternVision/44_afterimage_flash"), new Vector3(0.2f, -1.0f, 0f), "VFX", 80, 0.6f, 0.7f);
        GameObject grandmaAfterimage = CreateHidden(hidden, "GrandmaAfterimage_MourningHall", "Assets/Art/Characters/Ghosts/13_grandmother_afterimage.png", new Vector3(2.6f, -1.45f, 0f), "Ghost", 40, 0.42f, 0.5f);
        afterimageFlash.SetActive(false);
        grandmaAfterimage.SetActive(false);

        SerializedObject serializedPuzzle = new SerializedObject(puzzle);
        serializedPuzzle.FindProperty("litBlackLanternItem").objectReferenceValue = litItem;
        serializedPuzzle.FindProperty("afterimageFlash").objectReferenceValue = afterimageFlash;
        serializedPuzzle.FindProperty("grandmaAfterimage").objectReferenceValue = grandmaAfterimage;
        serializedPuzzle.FindProperty("candleRenderer").objectReferenceValue = candleRenderer;
        serializedPuzzle.FindProperty("litCandleSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Props/Offerings/27_white_candle_lit.png");
        serializedPuzzle.ApplyModifiedPropertiesWithoutUndo();

        hidden.gameObject.SetActive(false);
        EditorSceneManager.SaveScene(scene, MourningHallScenePath);
        Debug.Log("MourningHall inventory and offering puzzle setup complete: " + MourningHallScenePath);
    }

    private static void SetupOldWell()
    {
        Directory.CreateDirectory("Assets/Scenes/Chapter1");

        Scene scene = File.Exists(OldWellScenePath)
            ? EditorSceneManager.OpenScene(OldWellScenePath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = FindOrCreate("SceneRoot");
        Transform background = FindOrCreateChild(root.transform, "Background");
        Transform hidden = FindOrCreateChild(root.transform, "HiddenObjects");
        Transform interactables = FindOrCreateChild(root.transform, "Interactables");
        Transform vfx = FindOrCreateChild(root.transform, "VFX");
        FindOrCreateChild(root.transform, "SpawnPoints");
        FindOrCreateChild(root.transform, "SceneTransitions");

        Camera camera = SetupCamera();
        GameObject reality = SetupBackground(background, "Reality_BG", "Assets/Art/Scenes/Chapter1/OldWell/OldWell_Reality.png", true, 0, camera);
        GameObject lantern = SetupBackground(background, "LanternVision_BG", "Assets/Art/Scenes/Chapter1/OldWell/OldWell_LanternVision.png", false, 1, camera);
        SetupSceneControllers(root, reality, lantern, hidden.gameObject);
        SetupManagers();
        SetupCanvas(root.transform);
        SetupPlayer();

        GameObject wellProp = CreateSprite(interactables, "OldWell_Prop", "Assets/Art/Props/OldWell/29_well_rope_and_bucket.png", new Vector3(0f, -2.05f, 0f), "Props", 20, 0.32f, 1f);
        BoxCollider2D propCollider = wellProp.GetComponent<BoxCollider2D>();
        if (propCollider != null)
        {
            Object.DestroyImmediate(propCollider);
        }

        GameObject well = FindOrCreateChild(interactables, "OldWell_Interactable").gameObject;
        well.transform.localPosition = new Vector3(0f, -1.7f, 0f);
        BoxCollider2D collider = well.GetComponent<BoxCollider2D>() ?? well.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(2.3f, 2.0f);

        OldWellPuzzle puzzle = well.GetComponent<OldWellPuzzle>() ?? well.AddComponent<OldWellPuzzle>();

        GameObject nameInWell = CreateHidden(hidden, "NameInWellEffect", "Assets/Art/VFX/LanternVision/35_name_in_well_effect.png", new Vector3(0f, -1.75f, 0f), "VFX", 70, 0.5f, 0.85f);
        GameObject waterReflection = CreateHidden(hidden, "WaterReflection_Effect", "Assets/Art/VFX/LanternVision/34_water_reflection_effect.png", new Vector3(0f, -2.0f, 0f), "VFX", 62, 0.48f, 0.72f);
        GameObject afterimageFlash = CreateHidden(vfx, "AfterimageFlash", FirstPng("Assets/Art/VFX/LanternVision/44_afterimage_flash"), new Vector3(0.1f, -1.0f, 0f), "VFX", 85, 0.62f, 0.8f);
        nameInWell.SetActive(false);
        waterReflection.SetActive(false);
        afterimageFlash.SetActive(false);

        GameObject endingPanel = GameObject.Find("ChapterEndingPanel");
        GameObject endingTextObject = GameObject.Find("ChapterEndingText");
        SerializedObject serializedPuzzle = new SerializedObject(puzzle);
        serializedPuzzle.FindProperty("nameInWellEffect").objectReferenceValue = nameInWell;
        serializedPuzzle.FindProperty("waterReflectionEffect").objectReferenceValue = waterReflection;
        serializedPuzzle.FindProperty("afterimageFlash").objectReferenceValue = afterimageFlash;
        serializedPuzzle.FindProperty("chapterEndingRoot").objectReferenceValue = endingPanel;
        serializedPuzzle.FindProperty("chapterEndingText").objectReferenceValue = endingTextObject != null ? endingTextObject.GetComponent<Text>() : null;
        serializedPuzzle.ApplyModifiedPropertiesWithoutUndo();

        hidden.gameObject.SetActive(false);
        EditorSceneManager.SaveScene(scene, OldWellScenePath);
        Debug.Log("OldWell puzzle setup complete: " + OldWellScenePath);
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
            Vector2 size = renderer.sprite.bounds.size;
            float scale = Mathf.Max(viewWidth / size.x, viewHeight / size.y) * 1.02f;
            item.transform.localScale = Vector3.one * scale;
        }

        return item;
    }

    private static void SetupSceneControllers(GameObject root, GameObject reality, GameObject lantern, GameObject hidden)
    {
        SceneBackgroundSet set = root.GetComponent<SceneBackgroundSet>() ?? root.AddComponent<SceneBackgroundSet>();
        set.realityBackground = reality;
        set.lanternVisionBackground = lantern;
        set.hiddenObjects = hidden;

        LanternVisionController controller = root.GetComponent<LanternVisionController>() ?? root.AddComponent<LanternVisionController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("backgroundSet").objectReferenceValue = set;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        set.SetLanternVision(false);
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

    private static void SetupCanvas(Transform sceneRoot)
    {
        GameObject canvasObject = FindOrCreate("Chapter1_UI_Canvas");
        canvasObject.transform.SetParent(sceneRoot, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>() ?? canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>() ?? canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasObject.GetComponent<GraphicRaycaster>() == null)
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject panel = FindOrCreateChild(canvasObject.transform, "InventoryPanel").gameObject;
        RectTransform panelRect = panel.GetComponent<RectTransform>() ?? panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0.5f);
        panelRect.anchorMax = new Vector2(1f, 0.5f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.anchoredPosition = new Vector2(-60f, 0f);
        panelRect.sizeDelta = new Vector2(520f, 640f);

        Image panelImage = panel.GetComponent<Image>() ?? panel.AddComponent<Image>();
        panelImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/51_inventory_base_ui.png");
        panelImage.color = new Color(1f, 1f, 1f, 0.95f);

        Image[] slots = new Image[8];
        for (int i = 0; i < slots.Length; i++)
        {
            GameObject slot = FindOrCreateChild(panel.transform, "ItemSlot_" + i.ToString("00")).gameObject;
            RectTransform rect = slot.GetComponent<RectTransform>() ?? slot.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(80f + (i % 4) * 94f, -90f - (i / 4) * 94f);
            rect.sizeDelta = new Vector2(76f, 76f);

            slots[i] = slot.GetComponent<Image>() ?? slot.AddComponent<Image>();
            slots[i].sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/52_item_slot.png");
            slots[i].color = new Color(1f, 1f, 1f, 0.85f);
            if (slot.GetComponent<Button>() == null)
            {
                slot.AddComponent<Button>();
            }
        }

        GameObject description = FindOrCreateChild(panel.transform, "ItemDescription").gameObject;
        RectTransform descriptionRect = description.GetComponent<RectTransform>() ?? description.AddComponent<RectTransform>();
        descriptionRect.anchorMin = new Vector2(0f, 0f);
        descriptionRect.anchorMax = new Vector2(1f, 0f);
        descriptionRect.offsetMin = new Vector2(70f, 70f);
        descriptionRect.offsetMax = new Vector2(-70f, 210f);

        Text descriptionText = description.GetComponent<Text>() ?? description.AddComponent<Text>();
        descriptionText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        descriptionText.fontSize = 24;
        descriptionText.alignment = TextAnchor.UpperLeft;
        descriptionText.color = new Color(0.12f, 0.1f, 0.08f, 1f);
        descriptionText.text = string.Empty;

        InventoryUI inventoryUI = canvasObject.GetComponent<InventoryUI>() ?? canvasObject.AddComponent<InventoryUI>();
        SerializedObject serializedUi = new SerializedObject(inventoryUI);
        serializedUi.FindProperty("panel").objectReferenceValue = panel;
        SerializedProperty slotImages = serializedUi.FindProperty("slotImages");
        slotImages.arraySize = slots.Length;
        for (int i = 0; i < slots.Length; i++)
        {
            slotImages.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        }
        serializedUi.FindProperty("descriptionText").objectReferenceValue = descriptionText;
        serializedUi.ApplyModifiedPropertiesWithoutUndo();
        panel.SetActive(false);

        GameObject endingPanel = FindOrCreateChild(canvasObject.transform, "ChapterEndingPanel").gameObject;
        RectTransform endingRect = endingPanel.GetComponent<RectTransform>() ?? endingPanel.AddComponent<RectTransform>();
        endingRect.anchorMin = new Vector2(0.5f, 0.5f);
        endingRect.anchorMax = new Vector2(0.5f, 0.5f);
        endingRect.pivot = new Vector2(0.5f, 0.5f);
        endingRect.anchoredPosition = Vector2.zero;
        endingRect.sizeDelta = new Vector2(760f, 280f);

        Image endingImage = endingPanel.GetComponent<Image>() ?? endingPanel.AddComponent<Image>();
        endingImage.color = new Color(0.04f, 0.035f, 0.03f, 0.92f);
        endingImage.raycastTarget = false;

        GameObject endingTextObject = FindOrCreateChild(endingPanel.transform, "ChapterEndingText").gameObject;
        RectTransform endingTextRect = endingTextObject.GetComponent<RectTransform>() ?? endingTextObject.AddComponent<RectTransform>();
        endingTextRect.anchorMin = Vector2.zero;
        endingTextRect.anchorMax = Vector2.one;
        endingTextRect.offsetMin = new Vector2(40f, 30f);
        endingTextRect.offsetMax = new Vector2(-40f, -30f);

        Text endingText = endingTextObject.GetComponent<Text>() ?? endingTextObject.AddComponent<Text>();
        endingText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        endingText.fontSize = 42;
        endingText.alignment = TextAnchor.MiddleCenter;
        endingText.color = new Color(0.86f, 0.82f, 0.72f, 1f);
        endingText.text = "\u7b2c\u4e00\u7ae0\u7ed3\u675f\uff1a\u5f52\u9547";
        endingPanel.SetActive(false);
    }

    private static void SetupPlayer()
    {
        if (GameObject.Find("Player_LinZhaoying") != null)
        {
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player_LinZhaoying.prefab");
        if (prefab != null)
        {
            GameObject player = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            player.name = "Player_LinZhaoying";
            player.transform.position = new Vector3(-5f, -2.7f, 0f);
        }
    }

    private static SpriteRenderer CreateOffering(Transform parent, string name, string id, string spritePath, Vector3 position, OfferingPuzzleManager puzzle)
    {
        GameObject item = CreateSprite(parent, name, spritePath, position, "Props", 20, 0.28f, 1f);
        OfferingPuzzleInteractable interactable = item.GetComponent<OfferingPuzzleInteractable>() ?? item.AddComponent<OfferingPuzzleInteractable>();

        SerializedObject serialized = new SerializedObject(interactable);
        serialized.FindProperty("offeringId").stringValue = id;
        serialized.FindProperty("puzzleManager").objectReferenceValue = puzzle;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        BoxCollider2D collider = item.GetComponent<BoxCollider2D>() ?? item.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.9f, 1f);
        return item.GetComponent<SpriteRenderer>();
    }

    private static GameObject CreateHidden(Transform parent, string name, string spritePath, Vector3 position, string layer, int order, float scale, float alpha)
    {
        GameObject item = CreateSprite(parent, name, spritePath, position, layer, order, scale, alpha);
        if (item.GetComponent<HiddenInLanternView>() == null)
        {
            item.AddComponent<HiddenInLanternView>();
        }
        return item;
    }

    private static GameObject CreateSprite(Transform parent, string name, string spritePath, Vector3 position, string layer, int order, float scale, float alpha)
    {
        GameObject item = FindOrCreateChild(parent, name).gameObject;
        item.transform.localPosition = position;
        item.transform.localScale = Vector3.one * scale;

        SpriteRenderer renderer = item.GetComponent<SpriteRenderer>() ?? item.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        renderer.sortingLayerName = layer;
        renderer.sortingOrder = order;
        renderer.color = new Color(1f, 1f, 1f, alpha);
        return item;
    }

    private static string FirstPng(string folder)
    {
        return Directory.Exists(folder)
            ? Directory.GetFiles(folder, "*.png").OrderBy(path => path).Select(path => path.Replace("\\", "/")).FirstOrDefault()
            : string.Empty;
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
}
