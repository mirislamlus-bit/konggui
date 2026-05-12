using TMPro;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class Chapter1VisualRuntimeFix
{
    private static bool subscribed;
    private static int lastAppliedFrame = -1;

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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Apply()
    {
        ApplyCurrentScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyCurrentScene();
    }

    private static void ApplyCurrentScene()
    {
        if (lastAppliedFrame == Time.frameCount)
        {
            return;
        }

        lastAppliedFrame = Time.frameCount;

        GameObject sceneRoot = GameObject.Find("SceneRoot");
        if (sceneRoot == null)
        {
            return;
        }

        DisableLegacyChapterOneMvp();

        Camera camera = SetupCamera();
        Transform background = sceneRoot.transform.Find("Background");
        Transform hidden = sceneRoot.transform.Find("HiddenObjects");
        Transform interactables = sceneRoot.transform.Find("Interactables");
        Transform vfx = sceneRoot.transform.Find("VFX");
        LogCriticalSceneObjects(SceneManager.GetActiveScene().name, sceneRoot.transform, background, hidden, interactables);
        CleanupPaperEffigiesOutsideMourningHall();

        SetupBackground(background != null ? background.Find("Reality_BG") : null, 0, true, camera);
        SetupBackground(background != null ? background.Find("LanternVision_BG") : null, 1, false, camera);

        GameObject player = SetupPlayer(camera);
        SetupSpawnPoints(sceneRoot.transform);
        SetupGround();
        SetupSpriteLayers(hidden, interactables, vfx, player);
        SetupCanvas(sceneRoot.transform);
        SetupManagers();
        SetupTownGateFlow(sceneRoot.transform, interactables, player);
        SetupStoneBridgeFlow(sceneRoot.transform, interactables, hidden, player);
        SetupGrandmaHouseFlow(sceneRoot.transform, interactables, hidden, player);
        SetupMourningHallFlow(sceneRoot.transform, interactables, hidden, player);
        SetupOldWellFlow(sceneRoot.transform, interactables, hidden, player);
        SetupBackgroundSet(sceneRoot, background, hidden);
        DisableFullScreenLanternVisionArtifacts();
        ForceRealityView(sceneRoot, background, hidden);
    }

    private static Camera SetupCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return null;
        }

        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(8, 8, 8, 255);
        camera.transform.rotation = Quaternion.identity;
        camera.transform.position = new Vector3(0f, 0f, -10f);

        foreach (CameraFollow follow in camera.GetComponents<CameraFollow>())
        {
            Object.Destroy(follow);
        }

        return camera;
    }

    private static void DisableLegacyChapterOneMvp()
    {
        foreach (MonoBehaviour behaviour in Object.FindObjectsOfType<MonoBehaviour>(true))
        {
            if (behaviour == null || behaviour.GetType().FullName != "JianDeng.ChapterOneGame")
            {
                continue;
            }

            HideLegacyPrompt(behaviour);
            behaviour.enabled = false;
            Debug.Log("Disabled legacy ChapterOneGame to keep fixed-camera Chapter 1 flow.");
        }

        GameObject legacyPrompt = GameObject.Find("Prompt");
        if (legacyPrompt != null)
        {
            legacyPrompt.SetActive(false);
        }
    }

    private static void HideLegacyPrompt(MonoBehaviour behaviour)
    {
        FieldInfo promptField = behaviour.GetType().GetField("promptText");
        if (promptField == null)
        {
            return;
        }

        Text promptText = promptField.GetValue(behaviour) as Text;
        if (promptText != null)
        {
            promptText.text = string.Empty;
            promptText.gameObject.SetActive(false);
        }
    }

    private static GameObject SetupPlayer(Camera camera)
    {
        GameObject player = GameObject.Find("Player_LinZhaoying");
        if (player == null)
        {
            PlayerController controller = Object.FindObjectOfType<PlayerController>();
            if (controller != null)
            {
                player = controller.gameObject;
            }
        }

#if UNITY_EDITOR
        if (player == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player_LinZhaoying.prefab");
            if (prefab != null)
            {
                player = Object.Instantiate(prefab);
                player.name = "Player_LinZhaoying";
            }
        }
#endif

        if (player == null)
        {
            return null;
        }

        EnsurePlayerComponents(player);

        player.SetActive(true);
        player.transform.localScale = Vector3.one * 1.12f;
        CapsuleCollider2D capsule = player.GetComponent<CapsuleCollider2D>();
        if (string.IsNullOrEmpty(SceneTransition2D.PendingSpawnPointId) && SceneManager.GetActiveScene().name == "Chapter1_TownGate")
        {
            float footOffset = capsule != null ? capsule.offset.y - capsule.size.y * 0.5f : 0f;
            player.transform.position = new Vector3(-4.5f, -2.78f - footOffset * player.transform.localScale.y, 0f);
        }

        SpriteRenderer renderer = player.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
            renderer.sortingLayerName = "Character";
            renderer.sortingOrder = 50;
            SetAlpha(renderer, 1f);
        }

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }

        if (player.GetComponent<PlayerBoundsLimiter>() == null)
        {
            player.AddComponent<PlayerBoundsLimiter>();
        }

        return player;
    }

    private static void EnsurePlayerComponents(GameObject player)
    {
        if (player.GetComponent<SpriteRenderer>() == null)
        {
            player.AddComponent<SpriteRenderer>();
        }

        if (player.GetComponent<Animator>() == null)
        {
            player.AddComponent<Animator>();
        }

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = player.AddComponent<Rigidbody2D>();
        }
        body.gravityScale = 0f;
        body.freezeRotation = true;

        Collider2D bodyCollider = null;
        foreach (Collider2D collider in player.GetComponents<Collider2D>())
        {
            if (collider != null && !collider.isTrigger)
            {
                bodyCollider = collider;
                break;
            }
        }

        if (bodyCollider == null)
        {
            CapsuleCollider2D capsule = player.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.55f, 1.65f);
            capsule.offset = new Vector2(0f, 0.82f);
        }

        if (player.GetComponent<PlayerController>() == null)
        {
            player.AddComponent<PlayerController>();
        }

        if (player.GetComponent<InteractionDetector>() == null)
        {
            player.AddComponent<InteractionDetector>();
        }
    }

    private static void SetupSpawnPoints(Transform sceneRoot)
    {
        Transform spawnRoot = sceneRoot.Find("SpawnPoints");
        if (spawnRoot == null)
        {
            GameObject created = new GameObject("SpawnPoints");
            created.transform.SetParent(sceneRoot, false);
            spawnRoot = created.transform;
        }

        string sceneName = SceneManager.GetActiveScene().name;
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

    private static void EnsureSpawnPoint(Transform parent, string name, Vector3 position)
    {
        Transform spawn = parent.Find(name);
        if (spawn == null)
        {
            GameObject created = new GameObject(name);
            created.transform.SetParent(parent, false);
            spawn = created.transform;
        }

        spawn.position = position;
        SpawnPoint spawnPoint = spawn.GetComponent<SpawnPoint>() ?? spawn.gameObject.AddComponent<SpawnPoint>();
        spawnPoint.Configure(name);
    }

    private static void SetupBackground(Transform target, int order, bool active, Camera camera)
    {
        if (target == null)
        {
            return;
        }

        target.gameObject.SetActive(active);
        target.position = new Vector3(0f, 0f, 10f);

        SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null)
        {
            return;
        }

        Sprite sceneSprite = LoadSceneBackgroundSprite(SceneManager.GetActiveScene().name, target.name);
        if (sceneSprite != null)
        {
            renderer.sprite = sceneSprite;
        }

        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = order;

        FitBackgroundToCamera fitter = target.GetComponent<FitBackgroundToCamera>() ?? target.gameObject.AddComponent<FitBackgroundToCamera>();
        fitter.Apply();
        Debug.Log(target.name + " cover scale: " + fitter.LastAppliedScale.ToString("0.###"));
    }

    private static Sprite LoadSceneBackgroundSprite(string sceneName, string objectName)
    {
        string path = null;
        bool lantern = objectName == "LanternVision_BG";

        switch (sceneName)
        {
            case "Chapter1_TownGate":
                path = lantern
                    ? "Assets/Art/Scenes/Chapter1/GrandmaHouse/GrandmaHouse_LanternVision.png"
                    : "Assets/Art/Scenes/Chapter1/GrandmaHouse/GrandmaHouse_Reality.png";
                break;
            case "Chapter1_StoneBridge":
                path = lantern
                    ? "Assets/Art/Scenes/Chapter1/StoneBridge/StoneBridge_LanternVision.png"
                    : "Assets/Art/Scenes/Chapter1/StoneBridge/StoneBridge_Reality.png";
                break;
            case "Chapter1_GrandmaHouse":
                path = lantern
                    ? "Assets/Art/Scenes/Chapter1/TownGate/TownGate_LanternVision.png"
                    : "Assets/Art/Scenes/Chapter1/TownGate/TownGate_Reality.png";
                break;
            case "Chapter1_MourningHall":
                path = lantern
                    ? "Assets/Art/Scenes/Chapter1/MourningHall/MourningHall_LanternVision.png"
                    : "Assets/Art/Scenes/Chapter1/MourningHall/MourningHall_Reality.png";
                break;
            case "Chapter1_OldWell":
                path = lantern
                    ? "Assets/Art/Scenes/Chapter1/OldWell/OldWell_LanternVision.png"
                    : "Assets/Art/Scenes/Chapter1/OldWell/OldWell_Reality.png";
                break;
        }

        return string.IsNullOrEmpty(path) ? null : LoadSprite(path);
    }

    private static void SetupGround()
    {
        GameObject ground = GameObject.Find("GroundCollider") ?? new GameObject("GroundCollider");
        ground.transform.position = new Vector3(0f, -3.05f, 0f);
        BoxCollider2D collider = ground.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = ground.AddComponent<BoxCollider2D>();
        }

        if (collider == null)
        {
            Debug.LogWarning("GroundCollider is missing BoxCollider2D and Unity failed to add one.");
            return;
        }

        collider.isTrigger = false;
        collider.size = new Vector2(18f, 0.5f);

        SpriteRenderer renderer = ground.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }
    }

    private static void SetupSpriteLayers(Transform hidden, Transform interactables, Transform vfx, GameObject player)
    {
        float playerHeight = player != null ? 2.64f * player.transform.localScale.y : 2.9f;
        bool mourningHall = SceneManager.GetActiveScene().name == "Chapter1_MourningHall";
        foreach (SpriteRenderer renderer in Object.FindObjectsOfType<SpriteRenderer>(true))
        {
            string objectName = renderer.gameObject.name;
            if (objectName == "Reality_BG" || objectName == "LanternVision_BG")
            {
                continue;
            }

            if (objectName.Contains("LanternVisionOverlay") || objectName.Contains("lantern_vision_filter"))
            {
                renderer.enabled = false;
                renderer.gameObject.SetActive(false);
                continue;
            }

            if (objectName.Contains("PaperEffigy_Normal") || objectName == "PaperEffigy")
            {
                if (!mourningHall)
                {
                    renderer.gameObject.SetActive(false);
                    continue;
                }

                renderer.sortingLayerName = "Props";
                renderer.sortingOrder = 30;
                SetAlpha(renderer, 1f);
                if (interactables != null)
                {
                    renderer.transform.SetParent(interactables, true);
                }
                ScaleToHeight(renderer, playerHeight);
                renderer.transform.position = new Vector3(-2.8f, -2.78f, 0f);
                continue;
            }

            if (objectName.Contains("Afterimage") || objectName.Contains("Ghost") || objectName.Contains("Altered"))
            {
                renderer.sortingLayerName = "Ghost";
                renderer.sortingOrder = 40;
                SetAlpha(renderer, 0.45f);
                if (hidden != null)
                {
                    renderer.transform.SetParent(hidden, true);
                }
                ScaleToHeight(renderer, playerHeight * 1.05f);
                if (!IsFullScreenLanternVisionArtifact(objectName) && renderer.GetComponent<HiddenInLanternView>() == null)
                {
                    renderer.gameObject.AddComponent<HiddenInLanternView>();
                }
                renderer.gameObject.SetActive(false);
                continue;
            }

            if (objectName.Contains("HiddenText") || objectName.Contains("Footprints") || objectName.Contains("WaterReflection") || objectName.Contains("NameInWellEffect"))
            {
                renderer.sortingLayerName = "VFX";
                renderer.sortingOrder = 60;
                SetAlpha(renderer, objectName.Contains("NameInWellEffect") ? 0.85f : 0.5f);
                if (hidden != null)
                {
                    renderer.transform.SetParent(hidden, true);
                }
                renderer.gameObject.SetActive(false);
                continue;
            }

            if (objectName.Contains("CommonVFX_") || objectName.Contains("ThinMist") || objectName.Contains("FloatingDust") || objectName.Contains("FallingPaperAsh"))
            {
                renderer.sortingLayerName = "VFX";
                renderer.sortingOrder = 60;
                SetAlpha(renderer, GetVfxAlpha(objectName));
            }
        }
    }

    private static void SetupCanvas(Transform sceneRoot)
    {
        GameObject canvas = GameObject.Find("Chapter1_UI_Canvas") ?? new GameObject("Chapter1_UI_Canvas");
        canvas.transform.SetParent(sceneRoot, false);

        Canvas canvasComponent = canvas.GetComponent<Canvas>();
        if (canvasComponent == null)
        {
            canvasComponent = canvas.AddComponent<Canvas>();
        }
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasComponent.sortingLayerName = "UI";
        canvasComponent.sortingOrder = 0;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.AddComponent<GraphicRaycaster>();
        }

        GameObject overlay = FindOrCreateUi(canvas.transform, "LanternVisionOverlay_UI");
        DisableLanternVisionOverlayUi(overlay);

        GameObject vignette = FindOrCreateUi(canvas.transform, "VignetteOverlay");
        vignette.SetActive(false);

        GameObject oldWorldTitle = GameObject.Find("ChapterTitle");
        if (oldWorldTitle != null && oldWorldTitle.transform.parent != canvas.transform)
        {
            oldWorldTitle.SetActive(false);
        }

        GameObject title = FindOrCreateUi(canvas.transform, "ChapterTitleText");
        TextMeshProUGUI titleText = title.GetComponent<TextMeshProUGUI>() ?? title.AddComponent<TextMeshProUGUI>();
        titleText.text = GetChapterTitle(SceneManager.GetActiveScene().name);
        titleText.fontSize = 30f;
        titleText.color = new Color(0.82f, 0.84f, 0.82f, 0.82f);
        titleText.alignment = TextAlignmentOptions.TopLeft;
        titleText.raycastTarget = false;

        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(40f, -34f);
        titleRect.sizeDelta = new Vector2(520f, 80f);

        if (title.GetComponent<ChapterTitleFade>() == null)
        {
            title.AddComponent<ChapterTitleFade>();
        }

        GameObject tutorial = FindOrCreateUi(canvas.transform, "TutorialPrompt");
        Text tutorialText = tutorial.GetComponent<Text>() ?? tutorial.AddComponent<Text>();
        tutorialText.text = "A / D 移动\nE 互动";
        tutorialText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        tutorialText.fontSize = 24;
        tutorialText.alignment = TextAnchor.UpperLeft;
        tutorialText.color = new Color(0.86f, 0.86f, 0.8f, 0.88f);
        tutorialText.raycastTarget = false;
        RectTransform tutorialRect = tutorial.GetComponent<RectTransform>();
        tutorialRect.anchorMin = new Vector2(0f, 1f);
        tutorialRect.anchorMax = new Vector2(0f, 1f);
        tutorialRect.pivot = new Vector2(0f, 1f);
        tutorialRect.anchoredPosition = new Vector2(42f, -118f);
        tutorialRect.sizeDelta = new Vector2(260f, 90f);

        GameObject prompt = FindOrCreateUi(canvas.transform, "InteractionPrompt");
        Image promptImage = prompt.GetComponent<Image>() ?? prompt.AddComponent<Image>();
        promptImage.color = new Color(0.12f, 0.1f, 0.08f, 0.72f);
        promptImage.raycastTarget = false;
        RectTransform promptRect = prompt.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0f);
        promptRect.anchorMax = new Vector2(0.5f, 0f);
        promptRect.pivot = new Vector2(0.5f, 0.5f);
        promptRect.anchoredPosition = new Vector2(0f, 245f);
        promptRect.sizeDelta = new Vector2(178f, 56f);

        GameObject promptLabel = FindOrCreateUi(prompt.transform, "InteractionPromptText");
        Text promptText = promptLabel.GetComponent<Text>() ?? promptLabel.AddComponent<Text>();
        promptText.text = "[E] \u4e92\u52a8";
        promptText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        promptText.fontSize = 24;
        promptText.fontStyle = FontStyle.Bold;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.color = new Color(0.95f, 0.9f, 0.76f, 1f);
        promptText.raycastTarget = false;
        Stretch(promptLabel.GetComponent<RectTransform>());
        prompt.SetActive(false);

        GameObject qHint = FindOrCreateUi(canvas.transform, "QHintText");
        Text qHintText = qHint.GetComponent<Text>() ?? qHint.AddComponent<Text>();
        qHintText.text = string.Empty;
        qHintText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        qHintText.fontSize = 24;
        qHintText.alignment = TextAnchor.UpperRight;
        qHintText.color = new Color(0.86f, 0.86f, 0.8f, 0.86f);
        qHintText.raycastTarget = false;
        RectTransform qHintRect = qHint.GetComponent<RectTransform>();
        qHintRect.anchorMin = new Vector2(1f, 1f);
        qHintRect.anchorMax = new Vector2(1f, 1f);
        qHintRect.pivot = new Vector2(1f, 1f);
        qHintRect.anchoredPosition = new Vector2(-42f, -42f);
        qHintRect.sizeDelta = new Vector2(360f, 60f);
        qHint.SetActive(false);

        GameObject dialogueRoot = FindOrCreateUi(canvas.transform, "DialogueBox");
        Image dialogueImage = dialogueRoot.GetComponent<Image>() ?? dialogueRoot.AddComponent<Image>();
        dialogueImage.color = new Color(0.08f, 0.07f, 0.055f, 0.84f);
        dialogueImage.raycastTarget = false;
        RectTransform dialogueRect = dialogueRoot.GetComponent<RectTransform>();
        dialogueRect.anchorMin = new Vector2(0.5f, 0f);
        dialogueRect.anchorMax = new Vector2(0.5f, 0f);
        dialogueRect.pivot = new Vector2(0.5f, 0.5f);
        dialogueRect.anchoredPosition = new Vector2(0f, 92f);
        dialogueRect.sizeDelta = new Vector2(920f, 184f);

        GameObject dialogueNameObject = FindOrCreateUi(dialogueRoot.transform, "DialogueNameText");
        Text dialogueNameText = dialogueNameObject.GetComponent<Text>() ?? dialogueNameObject.AddComponent<Text>();
        dialogueNameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        dialogueNameText.fontSize = 24;
        dialogueNameText.fontStyle = FontStyle.Bold;
        dialogueNameText.alignment = TextAnchor.UpperLeft;
        dialogueNameText.color = new Color(0.96f, 0.88f, 0.66f, 1f);
        dialogueNameText.raycastTarget = false;
        RectTransform dialogueNameRect = dialogueNameObject.GetComponent<RectTransform>();
        dialogueNameRect.anchorMin = new Vector2(0f, 1f);
        dialogueNameRect.anchorMax = new Vector2(1f, 1f);
        dialogueNameRect.pivot = new Vector2(0.5f, 1f);
        dialogueNameRect.anchoredPosition = new Vector2(0f, -22f);
        dialogueNameRect.sizeDelta = new Vector2(-72f, 34f);

        GameObject dialogueTextObject = FindOrCreateUi(dialogueRoot.transform, "DialogueText");
        Text dialogueText = dialogueTextObject.GetComponent<Text>() ?? dialogueTextObject.AddComponent<Text>();
        dialogueText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        dialogueText.fontSize = 26;
        dialogueText.alignment = TextAnchor.UpperLeft;
        dialogueText.color = new Color(0.92f, 0.9f, 0.82f, 1f);
        dialogueText.raycastTarget = false;
        RectTransform dialogueTextRect = dialogueTextObject.GetComponent<RectTransform>();
        dialogueTextRect.anchorMin = Vector2.zero;
        dialogueTextRect.anchorMax = Vector2.one;
        dialogueTextRect.offsetMin = new Vector2(36f, 24f);
        dialogueTextRect.offsetMax = new Vector2(-36f, -62f);
        dialogueRoot.SetActive(false);

        GameObject endingPanel = FindOrCreateUi(canvas.transform, "ChapterEndingPanel");
        Image endingImage = endingPanel.GetComponent<Image>() ?? endingPanel.AddComponent<Image>();
        endingImage.color = new Color(0.02f, 0.018f, 0.014f, 0.82f);
        endingImage.raycastTarget = false;
        Stretch(endingPanel.GetComponent<RectTransform>());

        GameObject endingTextObject = FindOrCreateUi(endingPanel.transform, "ChapterEndingText");
        Text endingText = endingTextObject.GetComponent<Text>() ?? endingTextObject.AddComponent<Text>();
        endingText.text = "\u7b2c\u4e00\u7ae0\u7ed3\u675f\uff1a\u5f52\u9547";
        endingText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        endingText.fontSize = 42;
        endingText.fontStyle = FontStyle.Bold;
        endingText.alignment = TextAnchor.MiddleCenter;
        endingText.color = new Color(0.92f, 0.9f, 0.82f, 1f);
        endingText.raycastTarget = false;
        Stretch(endingTextObject.GetComponent<RectTransform>());
        endingPanel.SetActive(false);

        GameObject inventoryPanel = FindOrCreateUi(canvas.transform, "InventoryPanel");
        Image inventoryImage = inventoryPanel.GetComponent<Image>() ?? inventoryPanel.AddComponent<Image>();
        inventoryImage.color = new Color(0.08f, 0.07f, 0.055f, 0.9f);
        inventoryImage.raycastTarget = false;
        RectTransform inventoryRect = inventoryPanel.GetComponent<RectTransform>();
        inventoryRect.anchorMin = new Vector2(1f, 1f);
        inventoryRect.anchorMax = new Vector2(1f, 1f);
        inventoryRect.pivot = new Vector2(1f, 1f);
        inventoryRect.anchoredPosition = new Vector2(-42f, -112f);
        inventoryRect.sizeDelta = new Vector2(360f, 420f);

        Image[] inventorySlots = new Image[4];
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            GameObject slot = FindOrCreateUi(inventoryPanel.transform, "InventorySlot_" + (i + 1).ToString("00"));
            Image slotImage = slot.GetComponent<Image>() ?? slot.AddComponent<Image>();
            slotImage.color = new Color(1f, 1f, 1f, 0.18f);
            slotImage.raycastTarget = true;
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0f, 1f);
            slotRect.anchorMax = new Vector2(0f, 1f);
            slotRect.pivot = new Vector2(0f, 1f);
            slotRect.anchoredPosition = new Vector2(26f + i * 78f, -26f);
            slotRect.sizeDelta = new Vector2(62f, 62f);
            inventorySlots[i] = slotImage;
        }

        GameObject itemList = FindOrCreateUi(inventoryPanel.transform, "InventoryItemListText");
        Text itemListText = itemList.GetComponent<Text>() ?? itemList.AddComponent<Text>();
        itemListText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        itemListText.fontSize = 22;
        itemListText.alignment = TextAnchor.UpperLeft;
        itemListText.color = new Color(0.92f, 0.9f, 0.82f, 1f);
        itemListText.raycastTarget = false;
        RectTransform itemListRect = itemList.GetComponent<RectTransform>();
        itemListRect.anchorMin = new Vector2(0f, 1f);
        itemListRect.anchorMax = new Vector2(1f, 1f);
        itemListRect.pivot = new Vector2(0.5f, 1f);
        itemListRect.anchoredPosition = new Vector2(0f, -106f);
        itemListRect.sizeDelta = new Vector2(-52f, 116f);

        GameObject description = FindOrCreateUi(inventoryPanel.transform, "InventoryDescriptionText");
        Text descriptionText = description.GetComponent<Text>() ?? description.AddComponent<Text>();
        descriptionText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        descriptionText.fontSize = 20;
        descriptionText.alignment = TextAnchor.UpperLeft;
        descriptionText.color = new Color(0.82f, 0.84f, 0.78f, 1f);
        descriptionText.raycastTarget = false;
        RectTransform descriptionRect = description.GetComponent<RectTransform>();
        descriptionRect.anchorMin = new Vector2(0f, 0f);
        descriptionRect.anchorMax = new Vector2(1f, 0f);
        descriptionRect.pivot = new Vector2(0.5f, 0f);
        descriptionRect.anchoredPosition = new Vector2(0f, 28f);
        descriptionRect.sizeDelta = new Vector2(-52f, 160f);

        InventoryUI inventoryUi = canvas.GetComponent<InventoryUI>() ?? canvas.AddComponent<InventoryUI>();
        inventoryUi.Configure(inventoryPanel, inventorySlots, itemListText, descriptionText);
        inventoryPanel.SetActive(false);

        UIManager uiManager = canvas.GetComponent<UIManager>() ?? canvas.AddComponent<UIManager>();
        uiManager.Configure(prompt, null, dialogueRoot, inventoryPanel, endingPanel, qHintText);
    }

    private static string GetChapterTitle(string sceneName)
    {
        switch (sceneName)
        {
            case "Chapter1_StoneBridge":
                return "\u7b2c\u4e00\u7ae0\uff1a\u5f52\u9547 / \u77f3\u6865";
            case "Chapter1_GrandmaHouse":
                return "\u7b2c\u4e00\u7ae0\uff1a\u5f52\u9547 / \u5916\u5a46\u5bb6";
            case "Chapter1_MourningHall":
                return "\u7b2c\u4e00\u7ae0\uff1a\u5f52\u9547 / \u7075\u5802";
            case "Chapter1_OldWell":
                return "\u7b2c\u4e00\u7ae0\uff1a\u5f52\u9547 / \u8001\u4e95";
            default:
                return "\u7b2c\u4e00\u7ae0\uff1a\u5f52\u9547 / \u9547\u53e3";
        }
    }

    private static void SetupManagers()
    {
        if (Object.FindObjectOfType<GameStateManager>() == null)
        {
            new GameObject("GameStateManager").AddComponent<GameStateManager>();
        }

        if (Object.FindObjectOfType<DialogueManager>() == null)
        {
            new GameObject("DialogueManager").AddComponent<DialogueManager>();
        }
        else
        {
            Object.FindObjectOfType<DialogueManager>().ResolveReferences();
        }

        if (Object.FindObjectOfType<InventoryManager>() == null)
        {
            new GameObject("InventoryManager").AddComponent<InventoryManager>();
        }
    }

    private static void SetupTownGateFlow(Transform sceneRoot, Transform interactables, GameObject player)
    {
        if (SceneManager.GetActiveScene().name != "Chapter1_TownGate")
        {
            return;
        }

        Transform parent = interactables != null ? interactables : sceneRoot;
        string[] returnLines =
        {
            "林照萤：渡灯镇……我已经很多年没回来了。",
            "林照萤：这里和记忆里一样，却又像少了什么。"
        };

        CreateInteractable(parent, "TownGate_Arch_Interactable", new Vector3(-2.4f, -1.45f, 0f), new Vector2(2.1f, 2.4f), "town_gate_arch", returnLines);
        CreateInteractable(parent, "Gatehouse_Interactable", new Vector3(-4.7f, -1.55f, 0f), new Vector2(1.8f, 2.25f), "gatehouse", returnLines);
        CreateInteractable(parent, "PaperAsh_Interactable", new Vector3(0.15f, -2.55f, 0f), new Vector2(1.8f, 0.75f), "paper_ash", returnLines);

        SetupSceneTransition(sceneRoot, "ToNext", new Vector3(7.45f, -2.2f, 0f), "Chapter1_StoneBridge", "Spawn_FromTownGate");

        if (player != null)
        {
            Debug.Log("TownGate flow ready for player: " + player.name);
        }
    }

    private static void SetupStoneBridgeFlow(Transform sceneRoot, Transform interactables, Transform hidden, GameObject player)
    {
        if (SceneManager.GetActiveScene().name != "Chapter1_StoneBridge")
        {
            return;
        }

        Transform interactableParent = interactables != null ? interactables : sceneRoot;
        Transform hiddenParent = hidden != null ? hidden : sceneRoot;

        GameObject normalLantern = FindOrCreateSceneChild(interactableParent, "RiverLantern_Normal_Interactable");
        normalLantern.transform.position = new Vector3(-1.6f, -2.35f, 0f);
        BoxCollider2D normalCollider = EnsureBoxCollider2D(normalLantern);
        normalCollider.isTrigger = true;
        normalCollider.size = new Vector2(1.25f, 0.9f);
        RiverLanternInteractable normalInteractable = normalLantern.GetComponent<RiverLanternInteractable>() ?? normalLantern.AddComponent<RiverLanternInteractable>();
        normalInteractable.SetNamedLantern(false);
        if (normalLantern.GetComponent<HiddenInLanternView>() != null)
        {
            Object.Destroy(normalLantern.GetComponent<HiddenInLanternView>());
        }
        ConfigureStoneBridgeVisual("Content_RiverLantern_Normal", new Vector3(-1.6f, -2.35f, 0f), "Props", 20, 0.55f, true);

        Transform namedVisual = hiddenParent.Find("Content_RiverLantern_Named_LanternOnly");
        if (namedVisual != null)
        {
            namedVisual.gameObject.SetActive(false);
        }
        ConfigureStoneBridgeVisual("Content_RiverLantern_Named_LanternOnly", new Vector3(-1.6f, -2.35f, 0f), "Props", 24, 0.55f, false);
        namedVisual = hiddenParent.Find("Content_RiverLantern_Named_LanternOnly");
        if (namedVisual != null && namedVisual.GetComponent<HiddenInLanternView>() == null)
        {
            namedVisual.gameObject.AddComponent<HiddenInLanternView>();
        }

        GameObject namedLantern = FindOrCreateSceneChild(hiddenParent, "RiverLantern_Named_Interactable");
        namedLantern.transform.position = new Vector3(-1.6f, -2.35f, 0f);
        BoxCollider2D namedCollider = EnsureBoxCollider2D(namedLantern);
        namedCollider.isTrigger = true;
        namedCollider.size = new Vector2(1.25f, 0.9f);
        RiverLanternInteractable namedInteractable = namedLantern.GetComponent<RiverLanternInteractable>() ?? namedLantern.AddComponent<RiverLanternInteractable>();
        namedInteractable.SetNamedLantern(true);
        if (namedLantern.GetComponent<HiddenInLanternView>() == null)
        {
            namedLantern.AddComponent<HiddenInLanternView>();
        }
        namedLantern.SetActive(false);

        Transform ghost = hiddenParent.Find("Content_RiverLantern_Ghost");
        if (ghost != null)
        {
            ghost.gameObject.SetActive(false);
            if (ghost.GetComponent<HiddenInLanternView>() == null)
            {
                ghost.gameObject.AddComponent<HiddenInLanternView>();
            }
            ConfigureStoneBridgeVisual("Content_RiverLantern_Ghost", new Vector3(1.1f, -1.25f, 0f), "Ghost", 40, 1.8f, false);
        }
        Transform reflection = hiddenParent.Find("Content_WaterReflection_LanternOnly");
        if (reflection != null)
        {
            reflection.gameObject.SetActive(false);
            if (reflection.GetComponent<HiddenInLanternView>() == null)
            {
                reflection.gameObject.AddComponent<HiddenInLanternView>();
            }
            ConfigureStoneBridgeVisual("Content_WaterReflection_LanternOnly", new Vector3(0.4f, -2.45f, 0f), "VFX", 60, 1.1f, false);
        }

        ForceStoneBridgeOpeningVisualState();

        SetupSceneTransition(sceneRoot, "ToPrevious", new Vector3(-7.45f, -2.2f, 0f), "Chapter1_TownGate", "Spawn_FromStoneBridge");
        SetupSceneTransition(sceneRoot, "ToNext", new Vector3(7.45f, -2.2f, 0f), "Chapter1_GrandmaHouse", "Spawn_FromStoneBridge");
        SetupConditionalSceneExit(
            sceneRoot,
            "ToOldWell",
            new Vector3(0f, -2.2f, 0f),
            new Vector2(1.2f, 2.2f),
            "Chapter1_OldWell",
            "Spawn_FromMourningHall",
            "[E] 前往老井",
            SceneExit.SceneExitRequirement.HasSeenNamedRiverLantern,
            "河面上似乎还有什么没看清。");

        if (player != null)
        {
            Debug.Log("StoneBridge flow ready for player: " + player.name);
        }
    }

    private static void ForceStoneBridgeOpeningVisualState()
    {
        SetSceneObjectActive("Content_RiverLantern_Named_LanternOnly", false);
        SetSceneObjectActive("Content_RiverLantern_Ghost", false);
        SetSceneObjectActive("Content_WaterReflection_LanternOnly", false);
        SetSceneObjectActive("Content_HiddenText_StoneBridge", false);
        SetSceneObjectActive("CommonVFX_RiverReflection_StoneBridge", false);
        SetSceneObjectActive("CommonVFX_WaterRipples_StoneBridge", false);
        SetSceneObjectActive("CommonVFX_AfterimageFlash_StoneBridge", false);
        SetSceneObjectActive("CommonVFX_VignetteOverlay", false);
        SetSceneObjectActive("LanternVisionOverlay", false);
        SetSceneObjectActive("LanternVisionOverlay_UI", false);

        Transform normalLantern = FindSceneTransform("Content_RiverLantern_Normal");
        if (normalLantern == null)
        {
            return;
        }

        normalLantern.gameObject.SetActive(true);
        normalLantern.position = new Vector3(1.65f, -2.42f, 0f);
        SpriteRenderer renderer = normalLantern.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            normalLantern.localScale = Vector3.one * 0.08f;
            return;
        }

        renderer.sortingLayerName = "Props";
        renderer.sortingOrder = 18;
        SetAlpha(renderer, 0.72f);
        ScaleToHeight(renderer, 0.42f);
    }

    private static void SetSceneObjectActive(string objectName, bool active)
    {
        Transform target = FindSceneTransform(objectName);
        if (target != null)
        {
            target.gameObject.SetActive(active);
        }
    }

    private static void ConfigureStoneBridgeVisual(string objectName, Vector3 position, string sortingLayer, int order, float targetHeight, bool active)
    {
        Transform visual = FindSceneTransform(objectName);
        if (visual == null)
        {
            return;
        }

        visual.position = position;
        SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;
            SetAlpha(renderer, active ? 1f : 0.65f);
            ScaleToHeight(renderer, targetHeight);
        }

        visual.gameObject.SetActive(active);
    }

    private static void SetupGrandmaHouseFlow(Transform sceneRoot, Transform interactables, Transform hidden, GameObject player)
    {
        if (SceneManager.GetActiveScene().name != "Chapter1_GrandmaHouse")
        {
            return;
        }

        Transform interactableParent = interactables != null ? interactables : sceneRoot;
        Transform hiddenParent = hidden != null ? hidden : sceneRoot;

        CreateInteractable(
            interactableParent,
            "OldChair_Interactable",
            new Vector3(-2.4f, -2.2f, 0f),
            new Vector2(1.1f, 1.1f),
            "old_chair",
            new[] { "\u6797\u7167\u8424\uff1a\u85e4\u6905\u8fd8\u5728\uff0c\u53ea\u662f\u6ca1\u4eba\u518d\u5750\u4e86\u3002" });

        CreateInteractable(
            interactableParent,
            "IncenseBurner_Interactable",
            new Vector3(0.6f, -2.2f, 0f),
            new Vector2(1.1f, 1.1f),
            "incense_burner",
            new[] { "\u6797\u7167\u8424\uff1a\u9999\u7070\u8fd8\u6ca1\u6563\u2026\u2026\u6709\u4eba\u6765\u8fc7\uff1f" });

        CreateInteractable(
            interactableParent,
            "Door_Interactable",
            new Vector3(-6.2f, -1.65f, 0f),
            new Vector2(1.2f, 2.2f),
            "wooden_door",
            new[] { "\u6797\u7167\u8424\uff1a\u95e8\u4e0a\u7684\u6728\u7eb9\u50cf\u88ab\u4eba\u53cd\u590d\u6478\u8fc7\u3002" });

        GameObject blackLanternVisual = FindOrCreateLanternVisual(interactableParent, "BlackLantern_Unlit_Visual");
        SetupGrandmaBlackLanternVisual(blackLanternVisual);

        GameObject blackLantern = FindOrCreateSceneChild(interactableParent, "BlackLantern_Interactable");
        blackLantern.transform.position = new Vector3(2.4f, -2.15f, 0f);
        BoxCollider2D blackLanternCollider = EnsureBoxCollider2D(blackLantern);
        blackLanternCollider.isTrigger = true;
        blackLanternCollider.size = new Vector2(1.2f, 1.8f);
        if (blackLantern.GetComponent<BlackLanternPickupInteractable>() == null)
        {
            blackLantern.AddComponent<BlackLanternPickupInteractable>();
        }

        SetupGrandmaHiddenObjects(hiddenParent);
        SetupSceneTransition(sceneRoot, "ToPrevious", new Vector3(-7.45f, -2.2f, 0f), "Chapter1_StoneBridge", "Spawn_FromGrandmaHouse");
        SetupSceneTransition(sceneRoot, "ToNext", new Vector3(7.45f, -2.2f, 0f), "Chapter1_MourningHall", "Spawn_FromGrandmaHouse");

        if (player != null)
        {
            Debug.Log("GrandmaHouse flow ready for player: " + player.name);
        }
    }

    private static void SetupGrandmaBlackLanternVisual(GameObject blackLanternVisual)
    {
        if (blackLanternVisual == null)
        {
            return;
        }

        blackLanternVisual.transform.position = new Vector3(2.4f, -2.15f, 0f);
        SpriteRenderer renderer = blackLanternVisual.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = blackLanternVisual.AddComponent<SpriteRenderer>();
        }
        if (renderer.sprite == null)
        {
            renderer.sprite = LoadSprite("Assets/Art/Props/Lantern/17_black_lantern_unlit.png");
        }

        renderer.sortingLayerName = "Props";
        renderer.sortingOrder = 23;
        SetAlpha(renderer, 1f);
        ScaleToHeight(renderer, 0.52f);

        bool alreadyPicked = GameStateManager.Instance != null && GameStateManager.Instance.hasBlackLantern;
        blackLanternVisual.SetActive(!alreadyPicked);
    }

    private static GameObject FindOrCreateLanternVisual(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject visual = new GameObject(name);
        visual.transform.SetParent(parent, false);
        return visual;
    }

    private static void SetupGrandmaHiddenObjects(Transform hiddenParent)
    {
        Transform afterimage = hiddenParent.Find("GrandmaAfterimage") ?? hiddenParent.Find("GrandmaAfterimage_Test");
        if (afterimage != null)
        {
            afterimage.gameObject.SetActive(false);
            SpriteRenderer renderer = afterimage.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sortingLayerName = "Ghost";
                renderer.sortingOrder = 40;
                SetAlpha(renderer, 0.48f);
            }
            if (afterimage.GetComponent<HiddenInLanternView>() == null)
            {
                afterimage.gameObject.AddComponent<HiddenInLanternView>();
            }
        }

        Transform guideLine = hiddenParent.Find("GuideLine_GrandmaHouse") ?? hiddenParent.Find("Footprints_Test");
        if (guideLine != null)
        {
            guideLine.gameObject.SetActive(false);
            if (guideLine.GetComponent<HiddenInLanternView>() == null)
            {
                guideLine.gameObject.AddComponent<HiddenInLanternView>();
            }
        }

        Transform hiddenText = hiddenParent.Find("HiddenText_GrandmaHouse") ?? hiddenParent.Find("HiddenText_Test");
        if (hiddenText != null)
        {
            hiddenText.gameObject.SetActive(false);
            if (hiddenText.GetComponent<HiddenInLanternView>() == null)
            {
                hiddenText.gameObject.AddComponent<HiddenInLanternView>();
            }
        }

        GameObject wallText = FindOrCreateSceneChild(hiddenParent, "WallText_LampUnlit_NameUnreturned");
        wallText.transform.position = new Vector3(0.8f, -0.7f, 0f);
        TextMeshPro text = wallText.GetComponent<TextMeshPro>() ?? wallText.AddComponent<TextMeshPro>();
        text.text = "\u706f\u672a\u660e\uff0c\u540d\u672a\u5f52\u3002";
        text.fontSize = 2.4f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.75f, 0.86f, 0.78f, 0.42f);
        if (wallText.GetComponent<HiddenInLanternView>() == null)
        {
            wallText.AddComponent<HiddenInLanternView>();
        }
        wallText.SetActive(false);
    }

    private static void SetupMourningHallFlow(Transform sceneRoot, Transform interactables, Transform hidden, GameObject player)
    {
        if (SceneManager.GetActiveScene().name != "Chapter1_MourningHall")
        {
            return;
        }

        Transform interactableParent = interactables != null ? interactables : sceneRoot;
        Transform hiddenParent = hidden != null ? hidden : sceneRoot;

        OfferingPuzzleManager puzzle = sceneRoot.GetComponent<OfferingPuzzleManager>() ?? sceneRoot.gameObject.AddComponent<OfferingPuzzleManager>();

        GameObject afterimageFlash = FindExistingOrCreate(hiddenParent, "CommonVFX_AfterimageFlash_MourningHall", "AfterimageFlash");
        GameObject grandmaAfterimage = FindExistingOrCreate(hiddenParent, "GrandmaGhost", "GrandmotherShadow");
        ConfigureGrandmaGhost(grandmaAfterimage);
        SpriteRenderer candleRenderer = FindSpriteRenderer("Content_WhiteCandle_Unlit");
        Sprite litCandleSprite = LoadSprite("Assets/Art/Props/Offerings/27_white_candle_lit.png");
        ItemData litItem = LoadItem("Assets/ScriptableObjects/Items/BlackLantern_Lit.asset");

        ConfigureMourningHallHiddenObject(afterimageFlash, "VFX", 85, 0.75f, false);
        ConfigureMourningHallHiddenObject(grandmaAfterimage, "Ghost", 40, 0.48f, false);
        if (grandmaAfterimage.GetComponent<HiddenInLanternView>() == null)
        {
            grandmaAfterimage.AddComponent<HiddenInLanternView>();
        }
        puzzle.Configure(litItem, afterimageFlash, grandmaAfterimage, candleRenderer, litCandleSprite);
        puzzle.ConfigureOfferingSprites(new[]
        {
            LoadSprite("Assets/Art/Props/Offerings/22_apple_single.png"),
            LoadSprite("Assets/Art/Props/Offerings/23_pastry_single.png"),
            LoadSprite("Assets/Art/Props/Offerings/24_wine_cup_single.png"),
            LoadSprite("Assets/Art/Props/Offerings/25_incense_burner_single.png"),
            LoadSprite("Assets/Art/Props/Offerings/26_white_candle_unlit.png")
        });

        EnsureMourningHallOfferingVisual(interactableParent, "Content_Offering_Apple", "Assets/Art/Props/Offerings/22_apple_single.png", new Vector3(-1.15f, -1.55f, 0f), 22, 0.28f);
        EnsureMourningHallOfferingVisual(interactableParent, "Content_Offering_Pastry", "Assets/Art/Props/Offerings/23_pastry_single.png", new Vector3(-0.35f, -1.55f, 0f), 22, 0.28f);
        EnsureMourningHallOfferingVisual(interactableParent, "Content_Offering_WineCup", "Assets/Art/Props/Offerings/24_wine_cup_single.png", new Vector3(0.45f, -1.55f, 0f), 22, 0.28f);
        EnsureMourningHallOfferingVisual(interactableParent, "Content_Offering_IncenseBurner", "Assets/Art/Props/Offerings/25_incense_burner_single.png", new Vector3(1.25f, -1.55f, 0f), 22, 0.34f);
        EnsureMourningHallOfferingVisual(interactableParent, "Content_WhiteCandle_Unlit", "Assets/Art/Props/Offerings/26_white_candle_unlit.png", new Vector3(2.05f, -1.55f, 0f), 22, 0.36f);

        puzzle.RegisterInitialOffering(0, "Offering_Apple");
        puzzle.RegisterInitialOffering(1, "Offering_Cake");
        puzzle.RegisterInitialOffering(2, "Offering_WineCup");
        puzzle.RegisterInitialOffering(3, "Offering_IncenseBurner");
        puzzle.RegisterInitialOffering(4, "Offering_Candle");

        GameObject applePickup = SetupOfferingPickUp(interactableParent, "Apple_Pickup", "Content_Offering_Apple", "Offering_Apple", 0, new Vector3(-1.15f, -1.55f, 0f), new Vector2(0.8f, 0.7f), puzzle, "Assets/Art/Props/Offerings/22_apple_single.png");
        GameObject cakePickup = SetupOfferingPickUp(interactableParent, "Cake_Pickup", "Content_Offering_Pastry", "Offering_Cake", 1, new Vector3(-0.35f, -1.55f, 0f), new Vector2(0.8f, 0.7f), puzzle, "Assets/Art/Props/Offerings/23_pastry_single.png");
        GameObject winePickup = SetupOfferingPickUp(interactableParent, "WineCup_Pickup", "Content_Offering_WineCup", "Offering_WineCup", 2, new Vector3(0.45f, -1.55f, 0f), new Vector2(0.8f, 0.7f), puzzle, "Assets/Art/Props/Offerings/24_wine_cup_single.png");
        GameObject incensePickup = SetupOfferingPickUp(interactableParent, "IncenseBurner_Pickup", "Content_Offering_IncenseBurner", "Offering_IncenseBurner", 3, new Vector3(1.25f, -1.55f, 0f), new Vector2(0.95f, 0.8f), puzzle, "Assets/Art/Props/Offerings/25_incense_burner_single.png");
        GameObject candlePickup = SetupOfferingPickUp(interactableParent, "Candle_Pickup", "Content_WhiteCandle_Unlit", "Offering_Candle", 4, new Vector3(2.05f, -1.55f, 0f), new Vector2(0.75f, 1f), puzzle, "Assets/Art/Props/Offerings/26_white_candle_unlit.png");

        GameObject applePlace = SetupOfferingPlace(interactableParent, "Apple_Place", "Placed_Offering_Apple", "Offering_Apple", 0, new Vector3(-1.15f, -1.55f, 0f), new Vector2(0.8f, 0.7f), puzzle, "Assets/Art/Props/Offerings/22_apple_single.png", 0.28f);
        GameObject cakePlace = SetupOfferingPlace(interactableParent, "Cake_Place", "Placed_Offering_Pastry", "Offering_Cake", 1, new Vector3(-0.35f, -1.55f, 0f), new Vector2(0.8f, 0.7f), puzzle, "Assets/Art/Props/Offerings/23_pastry_single.png", 0.28f);
        GameObject winePlace = SetupOfferingPlace(interactableParent, "WineCup_Place", "Placed_Offering_WineCup", "Offering_WineCup", 2, new Vector3(0.45f, -1.55f, 0f), new Vector2(0.8f, 0.7f), puzzle, "Assets/Art/Props/Offerings/24_wine_cup_single.png", 0.28f);
        GameObject incensePlace = SetupOfferingPlace(interactableParent, "IncenseBurner_Place", "Placed_Offering_IncenseBurner", "Offering_IncenseBurner", 3, new Vector3(1.25f, -1.55f, 0f), new Vector2(0.95f, 0.8f), puzzle, "Assets/Art/Props/Offerings/25_incense_burner_single.png", 0.34f);
        GameObject candlePlace = SetupOfferingPlace(interactableParent, "Candle_Place", "Placed_Offering_Candle", "Offering_Candle", 4, new Vector3(2.05f, -1.55f, 0f), new Vector2(0.75f, 1f), puzzle, "Assets/Art/Props/Offerings/26_white_candle_unlit.png", 0.36f);

        BindOfferingPair(applePickup, applePlace);
        BindOfferingPair(cakePickup, cakePlace);
        BindOfferingPair(winePickup, winePlace);
        BindOfferingPair(incensePickup, incensePlace);
        BindOfferingPair(candlePickup, candlePlace);
        SetLegacyOfferingInteractablesActive(false, applePickup, cakePickup, winePickup, incensePickup, candlePickup, applePlace, cakePlace, winePlace, incensePlace, candlePlace);

        GameObject offeringTable = FindOrCreateSceneChild(interactableParent, "OfferingTable_Interactable");
        offeringTable.transform.position = new Vector3(0f, -1.18f, 0f);
        BoxCollider2D offeringTableCollider = EnsureBoxCollider2D(offeringTable);
        offeringTableCollider.isTrigger = true;
        offeringTableCollider.size = new Vector2(3.8f, 0.85f);
        InteractableObject tableDialogue = offeringTable.GetComponent<InteractableObject>();
        if (tableDialogue != null)
        {
            Object.Destroy(tableDialogue);
        }
        OfferingPuzzleTableInteractable tableInteractable = offeringTable.GetComponent<OfferingPuzzleTableInteractable>() ?? offeringTable.AddComponent<OfferingPuzzleTableInteractable>();
        tableInteractable.Configure(puzzle);

        CreateInteractable(
            interactableParent,
            "PaperEffigy_Interactable",
            new Vector3(2.8f, -1.55f, 0f),
            new Vector2(1f, 2.3f),
            "paper_effigy",
            new[] { "\u6797\u7167\u8424\uff1a\u7eb8\u4eba\u7684\u8138\u2026\u2026\u597d\u50cf\u88ab\u4eba\u91cd\u65b0\u753b\u8fc7\u3002" });

        GameObject hiddenText = FindExistingOrCreate(hiddenParent, "HiddenText_MourningHall", "Content_HiddenText_MourningHall");
        if (hiddenText != null)
        {
            TextMeshPro text = hiddenText.GetComponent<TextMeshPro>();
            if (text == null)
            {
                text = hiddenText.AddComponent<TextMeshPro>();
            }

            if (text != null)
            {
                text.text = "\u679c \u2192 \u7cd5 \u2192 \u9152 \u2192 \u9999 \u2192 \u706b";
                text.fontSize = 1.18f;
                text.alignment = TextAlignmentOptions.Center;
                text.color = new Color(0.84f, 0.94f, 0.9f, 0.58f);
            }

            hiddenText.transform.position = new Vector3(0f, -0.78f, 0f);
            if (hiddenText.GetComponent<HiddenInLanternView>() == null)
            {
                hiddenText.AddComponent<HiddenInLanternView>();
            }
            hiddenText.SetActive(false);
        }

        SetupSceneTransition(sceneRoot, "ToPrevious", new Vector3(-7.45f, -2.2f, 0f), "Chapter1_GrandmaHouse", "Spawn_FromMourningHall");
        SetupSceneTransition(sceneRoot, "ToNext", new Vector3(7.45f, -2.2f, 0f), "Chapter1_StoneBridge", "Spawn_FromGrandmaHouse");

        if (player != null)
        {
            Debug.Log("MourningHall offering puzzle ready for player: " + player.name);
        }
    }

    private static void EnsureMourningHallOfferingVisual(Transform parent, string objectName, string spritePath, Vector3 position, int sortingOrder, float targetHeight)
    {
        GameObject visual = FindExistingOrCreate(parent, objectName, objectName);
        visual.transform.position = position;

        SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = visual.AddComponent<SpriteRenderer>();
        }

        if (renderer.sprite == null)
        {
            renderer.sprite = LoadSprite(spritePath);
        }

        renderer.sortingLayerName = "Props";
        renderer.sortingOrder = sortingOrder;
        SetAlpha(renderer, 1f);
        ScaleToHeight(renderer, targetHeight);
        visual.SetActive(true);
    }

    private static GameObject SetupOfferingPickUp(Transform parent, string interactableName, string visualName, string offeringId, int slotIndex, Vector3 fallbackPosition, Vector2 size, OfferingPuzzleManager puzzle, string iconPath)
    {
        GameObject item = FindOrCreateSceneChild(parent, interactableName);
        Transform visual = FindSceneTransform(visualName);
        item.transform.position = fallbackPosition;

        if (visual != null)
        {
            visual.position = fallbackPosition;

            SpriteRenderer visualRenderer = visual.GetComponent<SpriteRenderer>();
            if (visualRenderer != null)
            {
                visualRenderer.sortingLayerName = "Props";
                visualRenderer.sortingOrder = 22;
                SetAlpha(visualRenderer, 1f);
            }

            visual.gameObject.SetActive(true);
        }

        BoxCollider2D collider = EnsureBoxCollider2D(item);
        collider.isTrigger = true;
        collider.size = size;

        OfferingPuzzleInteractable interactable = item.GetComponent<OfferingPuzzleInteractable>() ?? item.AddComponent<OfferingPuzzleInteractable>();
        interactable.Configure(
            offeringId,
            slotIndex,
            OfferingPuzzleInteractable.OfferingInteractionMode.PickUp,
            puzzle,
            visual != null ? visual.gameObject : null,
            null,
            LoadSprite(iconPath));
        return item;
    }

    private static void SetLegacyOfferingInteractablesActive(bool active, params GameObject[] items)
    {
        if (items == null)
        {
            return;
        }

        foreach (GameObject item in items)
        {
            if (item != null)
            {
                item.SetActive(active);
            }
        }

        Debug.Log("[Chapter1Offering] Legacy per-item offering interactables active = " + active);
    }

    private static GameObject SetupOfferingPlace(
        Transform parent,
        string interactableName,
        string visualName,
        string offeringId,
        int slotIndex,
        Vector3 position,
        Vector2 size,
        OfferingPuzzleManager puzzle,
        string spritePath,
        float targetHeight)
    {
        GameObject placedVisual = FindOrCreateSceneChild(parent, visualName);
        placedVisual.transform.position = position;

        SpriteRenderer renderer = placedVisual.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = placedVisual.AddComponent<SpriteRenderer>();
        }

        if (renderer.sprite == null)
        {
            renderer.sprite = LoadSprite(spritePath);
        }

        renderer.sortingLayerName = "Props";
        renderer.sortingOrder = 24;
        SetAlpha(renderer, 1f);
        ScaleToHeight(renderer, targetHeight);
        placedVisual.SetActive(false);

        GameObject item = FindOrCreateSceneChild(parent, interactableName);
        item.transform.position = position;

        BoxCollider2D collider = EnsureBoxCollider2D(item);
        collider.isTrigger = true;
        collider.size = size;

        OfferingPuzzleInteractable interactable = item.GetComponent<OfferingPuzzleInteractable>() ?? item.AddComponent<OfferingPuzzleInteractable>();
        interactable.Configure(
            offeringId,
            slotIndex,
            OfferingPuzzleInteractable.OfferingInteractionMode.Place,
            puzzle,
            null,
            placedVisual);
        item.SetActive(false);
        return item;
    }

    private static void BindOfferingPair(GameObject pickup, GameObject place)
    {
        if (pickup != null)
        {
            OfferingPuzzleInteractable pickupInteractable = pickup.GetComponent<OfferingPuzzleInteractable>();
            if (pickupInteractable != null)
            {
                pickupInteractable.SetAlternateInteractable(place);
            }
        }

        if (place != null)
        {
            OfferingPuzzleInteractable placeInteractable = place.GetComponent<OfferingPuzzleInteractable>();
            if (placeInteractable != null)
            {
                placeInteractable.SetAlternateInteractable(pickup);
            }
        }
    }

    private static void SetupOldWellFlow(Transform sceneRoot, Transform interactables, Transform hidden, GameObject player)
    {
        if (SceneManager.GetActiveScene().name != "Chapter1_OldWell")
        {
            return;
        }

        Transform interactableParent = interactables != null ? interactables : sceneRoot;
        Transform hiddenParent = hidden != null ? hidden : sceneRoot;

        GameObject well = FindOrCreateSceneChild(interactableParent, "OldWell_Interactable");
        Transform wellVisual = FindSceneTransform("Content_WellRopeAndBucket") ?? FindSceneTransform("OldWell_Prop") ?? FindSceneTransform("WellRopeAndBucket");
        well.transform.position = wellVisual != null ? wellVisual.position : new Vector3(0f, -2.0f, 0f);
        BoxCollider2D wellCollider = EnsureBoxCollider2D(well);
        wellCollider.isTrigger = true;
        wellCollider.size = new Vector2(2.2f, 1.6f);

        GameObject nameInWell = FindExistingOrCreate(hiddenParent, "NameInWellEffect", "Content_NameInWellEffect");
        GameObject waterReflection = FindExistingOrCreate(hiddenParent, "WaterReflection_Effect", "Content_WaterReflection_OldWell");
        GameObject grandmaAfterimage = FindExistingOrCreate(hiddenParent, "GrandmaAfterimage_OldWell", "Content_Grandmother_Afterimage_OldWell");
        GameObject afterimageFlash = FindExistingOrCreate(hiddenParent, "AfterimageFlash_OldWell", "CommonVFX_AfterimageFlash_OldWell");

        ConfigureMourningHallHiddenObject(nameInWell, "VFX", 72, 0.85f, false);
        ConfigureMourningHallHiddenObject(waterReflection, "VFX", 62, 0.72f, false);
        ConfigureMourningHallHiddenObject(grandmaAfterimage, "Ghost", 42, 0.48f, false);
        ConfigureMourningHallHiddenObject(afterimageFlash, "VFX", 85, 0.78f, false);

        AddHiddenViewIfMissing(nameInWell);
        AddHiddenViewIfMissing(waterReflection);
        AddHiddenViewIfMissing(grandmaAfterimage);
        AddHiddenViewIfMissing(afterimageFlash);

        GameObject endingPanel = FindObjectIncludingInactive("ChapterEndingPanel");
        GameObject endingTextObject = FindObjectIncludingInactive("ChapterEndingText");
        Text endingText = endingTextObject != null ? endingTextObject.GetComponent<Text>() : null;

        OldWellPuzzle puzzle = well.GetComponent<OldWellPuzzle>() ?? well.AddComponent<OldWellPuzzle>();
        puzzle.Configure(nameInWell, waterReflection, grandmaAfterimage, afterimageFlash, endingPanel, endingText);

        SetupSceneTransition(sceneRoot, "ToPrevious", new Vector3(-7.45f, -2.2f, 0f), "Chapter1_MourningHall", "Spawn_FromOldWell");

        if (player != null)
        {
            Debug.Log("OldWell ending puzzle ready for player: " + player.name);
        }
    }

    private static void AddHiddenViewIfMissing(GameObject target)
    {
        if (target != null && target.GetComponent<HiddenInLanternView>() == null)
        {
            target.AddComponent<HiddenInLanternView>();
        }
    }

    private static void ConfigureGrandmaGhost(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        target.name = "GrandmaGhost";
        SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = target.AddComponent<SpriteRenderer>();
            Debug.Log("[Chapter1MourningHall] Added SpriteRenderer to GrandmaGhost.");
        }

        if (renderer.sprite == null)
        {
            renderer.sprite = LoadSprite("Assets/Art/Characters/Ghosts/13_grandmother_afterimage.png");
        }

        target.transform.position = new Vector3(0.15f, -1.18f, 0f);
        target.transform.localRotation = Quaternion.identity;
        renderer.sortingLayerName = "Ghost";
        renderer.sortingOrder = 43;
        SetAlpha(renderer, 0.54f);
        ScaleToHeight(renderer, 1.55f);

        if (target.GetComponent<HiddenInLanternView>() == null)
        {
            target.AddComponent<HiddenInLanternView>();
        }

        target.SetActive(false);
        Debug.Log("[Chapter1MourningHall] GrandmaGhost positioned at " + target.transform.position);
    }

    private static void ConfigureMourningHallHiddenObject(GameObject target, string sortingLayer, int order, float alpha, bool active)
    {
        if (target == null)
        {
            return;
        }

        SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;
            SetAlpha(renderer, alpha);
        }

        target.SetActive(active);
    }

    private static SpriteRenderer FindSpriteRenderer(string objectName)
    {
        Transform transform = FindSceneTransform(objectName);
        return transform != null ? transform.GetComponent<SpriteRenderer>() : null;
    }

    private static Transform FindSceneTransform(string objectName)
    {
        foreach (Transform transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform.name == objectName && transform.gameObject.scene.IsValid())
            {
                return transform;
            }
        }

        return null;
    }

    private static GameObject FindExistingOrCreate(Transform parent, string preferredName, string fallbackName)
    {
        Transform found = parent.Find(preferredName);
        if (found == null)
        {
            found = parent.Find(fallbackName);
        }
        if (found == null)
        {
            found = FindSceneTransform(preferredName) ?? FindSceneTransform(fallbackName);
        }

        if (found != null)
        {
            if (found.parent != parent)
            {
                found.SetParent(parent, true);
            }
            return found.gameObject;
        }

        return FindOrCreateSceneChild(parent, preferredName);
    }

    private static Sprite LoadSprite(string path)
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
        return null;
#endif
    }

    private static ItemData LoadItem(string path)
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<ItemData>(path);
#else
        return null;
#endif
    }

    private static void SetupSceneTransition(Transform sceneRoot, string transitionName, Vector3 position, string targetSceneName, string targetSpawnPointName)
    {
        Transform transitions = sceneRoot.Find("SceneTransitions");
        Transform existing = transitions != null ? transitions.Find(transitionName) : null;
        GameObject transition = existing != null ? existing.gameObject : new GameObject(transitionName);
        if (transitions != null)
        {
            transition.transform.SetParent(transitions, false);
        }

        transition.transform.position = position;
        BoxCollider2D transitionCollider = EnsureBoxCollider2D(transition);
        transitionCollider.isTrigger = true;
        transitionCollider.size = new Vector2(0.8f, 2.2f);
        SceneTransition2D transition2D = transition.GetComponent<SceneTransition2D>() ?? transition.AddComponent<SceneTransition2D>();
        transition2D.SetTarget(targetSceneName, targetSpawnPointName);
    }

    private static void SetupConditionalSceneExit(
        Transform sceneRoot,
        string transitionName,
        Vector3 position,
        Vector2 size,
        string targetSceneName,
        string targetSpawnPointName,
        string promptText,
        SceneExit.SceneExitRequirement requirement,
        string blockedMessage)
    {
        Transform transitions = sceneRoot.Find("SceneTransitions");
        Transform existing = transitions != null ? transitions.Find(transitionName) : null;
        GameObject transition = existing != null ? existing.gameObject : new GameObject(transitionName);
        if (transitions != null)
        {
            transition.transform.SetParent(transitions, false);
        }

        transition.transform.position = position;
        BoxCollider2D transitionCollider = EnsureBoxCollider2D(transition);
        transitionCollider.isTrigger = true;
        transitionCollider.size = size;

        SceneExit exit = transition.GetComponent<SceneExit>() ?? transition.AddComponent<SceneExit>();
        exit.Configure(targetSceneName, targetSpawnPointName, promptText, requirement, false, false, blockedMessage);
    }

    private static void LogCriticalSceneObjects(string sceneName, Transform sceneRoot, Transform background, Transform hidden, Transform interactables)
    {
        if (sceneRoot == null)
        {
            Debug.LogWarning("[Chapter1Check] Missing SceneRoot in scene: " + sceneName);
            return;
        }

        RequireObject(sceneName, sceneRoot.gameObject, "Background", background != null ? background.gameObject : null);
        RequireObject(sceneName, sceneRoot.gameObject, "HiddenObjects", hidden != null ? hidden.gameObject : null);
        RequireObject(sceneName, sceneRoot.gameObject, "Interactables", interactables != null ? interactables.gameObject : null);
        RequireSceneObject(sceneName, "Reality_BG");
        RequireSceneObject(sceneName, "LanternVision_BG");

        switch (sceneName)
        {
            case "Chapter1_TownGate":
                RequireSceneObject(sceneName, "Content_HiddenText_TownGate");
                break;
            case "Chapter1_StoneBridge":
                RequireSceneObject(sceneName, "Content_RiverLantern_Normal");
                RequireSceneObject(sceneName, "Content_RiverLantern_Named_LanternOnly");
                RequireSceneObject(sceneName, "Content_WaterReflection_LanternOnly");
                break;
            case "Chapter1_GrandmaHouse":
                RequireSceneObject(sceneName, "Content_Grandmother_Afterimage");
                break;
            case "Chapter1_MourningHall":
                RequireSceneObject(sceneName, "Content_Offering_Apple");
                RequireSceneObject(sceneName, "Content_Offering_Pastry");
                RequireSceneObject(sceneName, "Content_Offering_WineCup");
                RequireSceneObject(sceneName, "Content_Offering_IncenseBurner");
                RequireSceneObject(sceneName, "Content_WhiteCandle_Unlit");
                break;
            case "Chapter1_OldWell":
                RequireSceneObject(sceneName, "Content_WellRopeAndBucket");
                RequireSceneObject(sceneName, "Content_NameInWellEffect");
                RequireSceneObject(sceneName, "Content_WaterReflection_OldWell");
                break;
        }
    }

    private static void RequireObject(string sceneName, GameObject context, string objectName, GameObject found)
    {
        if (found == null)
        {
            Debug.LogWarning("[Chapter1Check] Missing child object '" + objectName + "' in scene: " + sceneName + " under " + context.name);
        }
    }

    private static void RequireSceneObject(string sceneName, string objectName)
    {
        if (FindSceneTransform(objectName) == null)
        {
            Debug.LogWarning("[Chapter1Check] Missing scene object '" + objectName + "' in scene: " + sceneName);
        }
    }

    private static GameObject FindOrCreateSceneChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject created = new GameObject(name);
        created.transform.SetParent(parent, false);
        return created;
    }

    private static void CreateInteractable(Transform parent, string name, Vector3 position, Vector2 size, string id, string[] lines)
    {
        GameObject item = FindOrCreateSceneChild(parent, name);
        item.transform.position = position;

        BoxCollider2D collider = EnsureBoxCollider2D(item);
        collider.isTrigger = true;
        collider.size = size;

        InteractableObject interactable = item.GetComponent<InteractableObject>() ?? item.AddComponent<InteractableObject>();
        interactable.interactionId = id;
        interactable.dialogueLines = lines;
    }

    private static BoxCollider2D EnsureBoxCollider2D(GameObject item)
    {
        BoxCollider2D collider = item.GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            return collider;
        }

        foreach (Collider2D existingCollider in item.GetComponents<Collider2D>())
        {
            if (existingCollider != null)
            {
                Object.Destroy(existingCollider);
            }
        }

        return item.AddComponent<BoxCollider2D>();
    }

    private static void SetupBackgroundSet(GameObject sceneRoot, Transform background, Transform hidden)
    {
        SceneBackgroundSet set = sceneRoot.GetComponent<SceneBackgroundSet>() ?? sceneRoot.AddComponent<SceneBackgroundSet>();
        set.realityBackground = background != null && background.Find("Reality_BG") != null ? background.Find("Reality_BG").gameObject : null;
        set.lanternVisionBackground = background != null && background.Find("LanternVision_BG") != null ? background.Find("LanternVision_BG").gameObject : null;
        set.hiddenObjects = hidden != null ? hidden.gameObject : null;

        LanternVisionController controller = sceneRoot.GetComponent<LanternVisionController>() ?? sceneRoot.AddComponent<LanternVisionController>();
        controller.SetLanternVision(false);
    }

    private static void ForceRealityView(GameObject sceneRoot, Transform background, Transform hidden)
    {
        if (background != null)
        {
            Transform reality = background.Find("Reality_BG");
            Transform lantern = background.Find("LanternVision_BG");
            if (reality != null)
            {
                reality.gameObject.SetActive(true);
            }
            if (lantern != null)
            {
                lantern.gameObject.SetActive(false);
            }
        }

        if (hidden != null)
        {
            hidden.gameObject.SetActive(false);
        }

        GameObject overlay = GameObject.Find("LanternVisionOverlay_UI");
        if (overlay != null)
        {
            DisableLanternVisionOverlayUi(overlay);
        }

        LanternVisionController controller = sceneRoot.GetComponent<LanternVisionController>();
        if (controller != null)
        {
            controller.SetLanternVision(false);
        }
    }

    private static void DisableFullScreenLanternVisionArtifacts()
    {
        foreach (Transform transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform == null || !transform.gameObject.scene.IsValid())
            {
                continue;
            }

            string objectName = transform.name;
            if (objectName.Contains("LanternOverlay") ||
                objectName.Contains("LanternVisionImage") ||
                objectName.Contains("FullScreenLantern") ||
                objectName.Contains("CrackFrame") ||
                objectName.Contains("GhostOverlay") ||
                objectName.Contains("LanternZoomImage") ||
                objectName.Contains("CommonVFX_VignetteOverlay") ||
                objectName.Contains("VignetteOverlay") ||
                objectName.Contains("lantern_vision_filter"))
            {
                transform.gameObject.SetActive(false);
                Debug.Log("[Chapter1LanternVision] Disabled full-screen artifact: " + objectName);
            }
        }
    }

    private static bool IsFullScreenLanternVisionArtifact(string objectName)
    {
        return objectName.Contains("LanternOverlay") ||
            objectName.Contains("LanternVisionImage") ||
            objectName.Contains("FullScreenLantern") ||
            objectName.Contains("CrackFrame") ||
            objectName.Contains("GhostOverlay") ||
            objectName.Contains("LanternZoomImage") ||
            objectName.Contains("CommonVFX_VignetteOverlay") ||
            objectName.Contains("VignetteOverlay") ||
            objectName.Contains("lantern_vision_filter");
    }

    private static void CleanupPaperEffigiesOutsideMourningHall()
    {
        bool mourningHall = SceneManager.GetActiveScene().name == "Chapter1_MourningHall";
        foreach (Transform transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform == null || !transform.gameObject.scene.IsValid())
            {
                continue;
            }

            string objectName = transform.name;
            bool paperEffigy = objectName.Contains("PaperEffigy") ||
                objectName.Contains("PaperMan") ||
                objectName == "PaperEffigy";
            if (!paperEffigy || mourningHall)
            {
                continue;
            }

            if (transform.gameObject.scene.name == "DontDestroyOnLoad")
            {
                Object.Destroy(transform.gameObject);
                Debug.Log("[Chapter1SceneCleanup] Destroyed DontDestroyOnLoad paper effigy: " + objectName);
                continue;
            }

            transform.gameObject.SetActive(false);
            Debug.Log("[Chapter1SceneCleanup] Disabled paper effigy outside MourningHall: " + objectName + " in " + transform.gameObject.scene.name);
        }
    }

    private static GameObject FindOrCreateUi(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject created = new GameObject(name);
        created.transform.SetParent(parent, false);
        created.AddComponent<RectTransform>();
        return created;
    }

    private static void DisableLanternVisionOverlayUi(GameObject overlay)
    {
        if (overlay == null)
        {
            return;
        }

        Image image = overlay.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = null;
            image.raycastTarget = false;
            image.color = Color.clear;
        }

        Button button = overlay.GetComponent<Button>();
        if (button != null)
        {
            Object.Destroy(button);
        }

        CanvasRenderer canvasRenderer = overlay.GetComponent<CanvasRenderer>();
        if (canvasRenderer != null)
        {
            canvasRenderer.SetAlpha(0f);
        }

        overlay.SetActive(false);
        Debug.Log("[Chapter1LanternVision] LanternVisionOverlay_UI disabled; LanternVision_BG is used instead.");
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

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void ScaleToHeight(SpriteRenderer renderer, float targetHeight)
    {
        if (renderer.sprite == null || renderer.sprite.bounds.size.y <= 0f)
        {
            return;
        }

        float scale = targetHeight / renderer.sprite.bounds.size.y;
        renderer.transform.localScale = Vector3.one * scale;
    }

    private static void SetAlpha(SpriteRenderer renderer, float alpha)
    {
        Color color = renderer.color;
        color.a = alpha;
        renderer.color = color;
    }

    private static float GetVfxAlpha(string objectName)
    {
        if (objectName.Contains("ThinMist") || objectName.Contains("Mist"))
        {
            return 0.2f;
        }

        if (objectName.Contains("FallingPaperAsh") || objectName.Contains("PaperAsh"))
        {
            return 0.16f;
        }

        if (objectName.Contains("FloatingDust") || objectName.Contains("Dust"))
        {
            return 0.16f;
        }

        return 0.22f;
    }
}
