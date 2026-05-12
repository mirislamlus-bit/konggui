using JianDeng;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ChapterOneRuntimeBootstrap
{
    // Runtime fallback for the early chapter-one MVP scene.
    private static Sprite blockSprite;
    private static Sprite playerSprite;
    private static Sprite lampSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BuildIfNeeded()
    {
        if (SceneManager.GetActiveScene().name != "Chapter1_GuiZhen_MVP")
        {
            return;
        }

        if (Object.FindObjectOfType<ChapterOneGame>() != null)
        {
            return;
        }

        CreateSprites();

        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
        }

        camera.orthographic = true;
        camera.orthographicSize = 4.5f;
        camera.backgroundColor = new Color32(18, 20, 18, 255);
        camera.transform.position = new Vector3(0f, 0f, -10f);

        GameObject scenes = new GameObject("Runtime_ChapterOne_Scenes");
        BuildScene(ChapterScene.TownGate, scenes.transform, "镇口", new Color32(66, 61, 52, 255));
        BuildTownGate(scenes.transform.Find("TownGate_镇口"));
        BuildScene(ChapterScene.StoneBridge, scenes.transform, "石桥", new Color32(52, 61, 61, 255));
        BuildStoneBridge(scenes.transform.Find("StoneBridge_石桥"));
        BuildScene(ChapterScene.GrandmaHouse, scenes.transform, "外婆家", new Color32(72, 59, 48, 255));
        BuildGrandmaHouse(scenes.transform.Find("GrandmaHouse_外婆家"));
        BuildScene(ChapterScene.MourningHall, scenes.transform, "灵堂", new Color32(48, 43, 41, 255));
        BuildMourningHall(scenes.transform.Find("MourningHall_灵堂"));
        BuildScene(ChapterScene.OldWell, scenes.transform, "老井", new Color32(39, 50, 48, 255));
        GameObject hiddenName = BuildOldWell(scenes.transform.Find("OldWell_老井"));

        Transform player = CreatePlayer();
        ChapterOneGame game = player.gameObject.AddComponent<ChapterOneGame>();
        Canvas canvas = CreateCanvas();
        game.player = player;
        game.mainCamera = camera;
        game.locationText = CreateText(canvas.transform, "Location", "", 24, TextAnchor.UpperLeft, new Vector2(24, -20), new Vector2(800, 48));
        game.promptText = CreateText(canvas.transform, "Prompt", "", 22, TextAnchor.MiddleCenter, new Vector2(0, 82), new Vector2(360, 46), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        game.dialogueText = CreateText(canvas.transform, "Dialogue", "", 22, TextAnchor.LowerLeft, new Vector2(32, 26), new Vector2(1160, 96), Vector2.zero, Vector2.zero);

        GameObject inventory = CreatePanel(canvas.transform, "InventoryPanel", new Color32(24, 22, 20, 232), new Vector2(-22, -72), new Vector2(320, 360), Vector2.one, Vector2.one);
        game.inventoryText = CreateText(inventory.transform, "InventoryText", "", 20, TextAnchor.UpperLeft, new Vector2(18, -18), new Vector2(282, 320));
        inventory.SetActive(false);
        game.inventoryPanel = inventory;

        GameObject overlay = CreatePanel(canvas.transform, "LampShadowOverlay", new Color32(8, 15, 13, 118), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
        overlay.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        overlay.SetActive(false);
        game.lampShadowOverlay = overlay;

        GameObject ending = CreatePanel(canvas.transform, "ChapterEndingPanel", new Color32(15, 12, 11, 244), Vector2.zero, new Vector2(620, 360), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        game.endingText = CreateText(ending.transform, "EndingText", "", 24, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(560, 300), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        ending.SetActive(false);
        game.endingPanel = ending;
        game.hiddenWellName = hiddenName;
        game.blackLampFlame = GameObject.Find("BlackLamp_Flame");
        if (game.blackLampFlame != null)
        {
            game.blackLampFlame.SetActive(false);
        }
    }

    private static Transform CreatePlayer()
    {
        GameObject player = new GameObject("Player_LinZhaoying");
        player.transform.position = new Vector3(-6.8f, -2.15f, 0f);
        SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
        renderer.sprite = playerSprite;
        renderer.color = new Color32(86, 65, 55, 255);
        renderer.sortingOrder = 30;
        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.55f, 1.8f);
        CreateSprite("HandLamp", player.transform, new Vector3(0.38f, -0.16f, 0f), new Vector3(0.34f, 0.34f, 1f), 31, new Color32(238, 177, 74, 255), lampSprite);
        return player.transform;
    }

    private static void BuildScene(ChapterScene scene, Transform parent, string title, Color32 color)
    {
        GameObject root = new GameObject(scene + "_" + title);
        root.transform.SetParent(parent);
        SceneRoot marker = root.AddComponent<SceneRoot>();
        marker.scene = scene;
        CreateSprite("Backdrop", root.transform, Vector3.zero, new Vector3(18f, 7.5f, 1f), 0, color);
        CreateSprite("Ground", root.transform, new Vector3(0f, -3.25f, 0f), new Vector3(18f, 1.2f, 1f), 3, new Color32(31, 36, 31, 255));
        CreateWorldText(title, root.transform, new Vector3(-7.3f, 2.65f, 0f), 28, new Color32(220, 207, 172, 255));
    }

    private static void BuildTownGate(Transform root)
    {
        CreateSprite("TownGate_Arch", root, new Vector3(-2.1f, -1.15f, 0f), new Vector3(2.8f, 2.3f, 1f), 8, new Color32(87, 61, 49, 255));
        DialogueInteractable sign = CreateInteractable<DialogueInteractable>("TownGate_Sign", root, new Vector3(-2.1f, -1.2f, 0f), new Vector2(1.8f, 2f));
        sign.prompt = "查看镇口灯牌";
        sign.normalLine = "渡灯镇三个字褪得发灰。灯牌背面刻着：名字会引魂。";
        sign.lampShadowLine = "灯影里多出一行小字：河灯上写着你的名字。";
        CreatePortal("To_StoneBridge", root, new Vector3(7.25f, -2.1f, 0f), ChapterScene.StoneBridge, new Vector3(-7f, -2.15f, 0f), "前往石桥", "石桥下水声很浅，像有人在纸后面呼吸。");
    }

    private static void BuildStoneBridge(Transform root)
    {
        CreateSprite("Bridge", root, new Vector3(0f, -1.85f, 0f), new Vector3(6.2f, 1.1f, 1f), 8, new Color32(82, 80, 72, 255));
        CreateSprite("River", root, new Vector3(0f, -2.75f, 0f), new Vector3(18f, 0.8f, 1f), 4, new Color32(23, 39, 41, 255));
        DialogueInteractable river = CreateInteractable<DialogueInteractable>("River_Lantern", root, new Vector3(1.6f, -2.1f, 0f), new Vector2(1.2f, 1f));
        river.prompt = "捞起河灯";
        river.normalLine = "河灯湿透了，灯面没有字。";
        river.lampShadowLine = "灯影映出你的名字：林照萤。字迹像刚干的血。";
        CreatePortal("Back_To_TownGate", root, new Vector3(-7.25f, -2.1f, 0f), ChapterScene.TownGate, new Vector3(6.8f, -2.15f, 0f), "返回镇口", "");
        CreatePortal("To_GrandmaHouse", root, new Vector3(7.25f, -2.1f, 0f), ChapterScene.GrandmaHouse, new Vector3(-7f, -2.15f, 0f), "前往外婆家", "外婆家门半掩，屋里摆着不该出现的白幡。");
    }

    private static void BuildGrandmaHouse(Transform root)
    {
        CreateSprite("House", root, new Vector3(-0.6f, -1.25f, 0f), new Vector3(5f, 3f, 1f), 8, new Color32(93, 68, 51, 255));
        DialogueInteractable room = CreateInteractable<DialogueInteractable>("Grandma_Room", root, new Vector3(0.4f, -1.7f, 0f), new Vector2(2f, 2f));
        room.prompt = "进入堂屋";
        room.normalLine = "桌上压着旧纸条：供品顺序为米、酒、香。";
        room.lampShadowLine = "灯影下，外婆的相框背面写着：点黑灯，照老井。";
        CreatePortal("Back_To_Bridge", root, new Vector3(-7.25f, -2.1f, 0f), ChapterScene.StoneBridge, new Vector3(6.8f, -2.15f, 0f), "返回石桥", "");
        CreatePortal("To_MourningHall", root, new Vector3(7.25f, -2.1f, 0f), ChapterScene.MourningHall, new Vector3(-7f, -2.15f, 0f), "前往灵堂", "灵堂里没有哭声，只有蜡烛烧纸芯的轻响。");
    }

    private static void BuildMourningHall(Transform root)
    {
        CreateSprite("Altar", root, new Vector3(0f, -1.85f, 0f), new Vector3(3.4f, 1.3f, 1f), 8, new Color32(81, 43, 38, 255));
        CreateWorldText("米", root, new Vector3(-2.1f, -2.48f, 0f), 22, Color.white);
        CreateWorldText("酒", root, new Vector3(-0.2f, -2.48f, 0f), 22, Color.white);
        CreateWorldText("香", root, new Vector3(1.7f, -2.48f, 0f), 22, Color.white);
        CreateInteractable<OfferingInteractable>("Offering_Rice", root, new Vector3(-1.9f, -2f, 0f), new Vector2(0.8f, 0.8f)).offering = Offering.Rice;
        CreateInteractable<OfferingInteractable>("Offering_Wine", root, new Vector3(0f, -2f, 0f), new Vector2(0.8f, 0.8f)).offering = Offering.Wine;
        CreateInteractable<OfferingInteractable>("Offering_Incense", root, new Vector3(1.9f, -2f, 0f), new Vector2(0.8f, 0.8f)).offering = Offering.Incense;
        CreateSprite("BlackLamp_Body", root, new Vector3(0f, -0.95f, 0f), new Vector3(0.72f, 0.72f, 1f), 13, new Color32(14, 13, 12, 255), lampSprite);
        CreateSprite("BlackLamp_Flame", root, new Vector3(0f, -0.53f, 0f), new Vector3(0.35f, 0.44f, 1f), 14, new Color32(186, 72, 43, 255), lampSprite);
        CreateInteractable<BlackLampInteractable>("BlackLamp", root, new Vector3(0f, -0.95f, 0f), new Vector2(1f, 1f));
        CreatePortal("Back_To_GrandmaHouse", root, new Vector3(-7.25f, -2.1f, 0f), ChapterScene.GrandmaHouse, new Vector3(6.8f, -2.15f, 0f), "返回外婆家", "");
        CreatePortal("To_OldWell", root, new Vector3(7.25f, -2.1f, 0f), ChapterScene.OldWell, new Vector3(-7f, -2.15f, 0f), "前往老井", "井边的纸灰没有落地，像停在一场倒放的雪里。");
    }

    private static GameObject BuildOldWell(Transform root)
    {
        CreateSprite("OldWell", root, new Vector3(0.2f, -1.7f, 0f), new Vector3(2f, 1.6f, 1f), 10, new Color32(58, 63, 58, 255));
        CreateInteractable<OldWellInteractable>("OldWell_Interact", root, new Vector3(0.2f, -1.7f, 0f), new Vector2(2f, 1.8f));
        GameObject hidden = CreateWorldText("林照萤 / 陈望月替死", root, new Vector3(-1.55f, -0.55f, 0f), 22, new Color32(151, 47, 39, 255));
        hidden.name = "Hidden_Well_Name_LampShadowOnly";
        hidden.SetActive(false);
        CreatePortal("Back_To_MourningHall", root, new Vector3(-7.25f, -2.1f, 0f), ChapterScene.MourningHall, new Vector3(6.8f, -2.15f, 0f), "返回灵堂", "");
        return hidden;
    }

    private static T CreateInteractable<T>(string name, Transform parent, Vector3 position, Vector2 size) where T : MonoBehaviour, JianDeng.IInteractable
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = size;
        go.AddComponent<InteractionZone>();
        return go.AddComponent<T>();
    }

    private static void CreatePortal(string name, Transform parent, Vector3 position, ChapterScene target, Vector3 spawn, string prompt, string line)
    {
        ScenePortal portal = CreateInteractable<ScenePortal>(name, parent, position, new Vector2(1f, 2.3f));
        portal.targetScene = target;
        portal.spawnPosition = spawn;
        portal.prompt = prompt;
        portal.arrivalLine = line;
        CreateSprite(name + "_Marker", parent, position + Vector3.up * 0.9f, new Vector3(0.3f, 0.55f, 1f), 18, new Color32(207, 187, 120, 255));
    }

    private static GameObject CreateSprite(string name, Transform parent, Vector3 position, Vector3 scale, int order, Color color)
    {
        return CreateSprite(name, parent, position, scale, order, color, blockSprite);
    }

    private static GameObject CreateSprite(string name, Transform parent, Vector3 position, Vector3 scale, int order, Color color, Sprite sprite)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = scale;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return go;
    }

    private static GameObject CreateWorldText(string text, Transform parent, Vector3 position, int fontSize, Color color)
    {
        GameObject go = new GameObject("Text_" + text);
        go.transform.SetParent(parent);
        go.transform.position = position;
        TextMesh mesh = go.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.fontSize = fontSize;
        mesh.characterSize = 0.08f;
        mesh.anchor = TextAnchor.MiddleLeft;
        mesh.color = color;
        go.GetComponent<MeshRenderer>().sortingOrder = 40;
        return go;
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasGo = new GameObject("UI_Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static GameObject CreatePanel(Transform parent, string name, Color32 color, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return panel;
    }

    private static Text CreateText(Transform parent, string name, string value, int size, TextAnchor alignment, Vector2 position, Vector2 rectSize)
    {
        return CreateText(parent, name, value, size, alignment, position, rectSize, new Vector2(0f, 1f), new Vector2(0f, 1f));
    }

    private static Text CreateText(Transform parent, string name, string value, int size, TextAnchor alignment, Vector2 position, Vector2 rectSize, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = size;
        text.alignment = alignment;
        text.color = new Color32(225, 215, 188, 255);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = rectSize;
        return text;
    }

    private static void CreateSprites()
    {
        blockSprite = MakeSprite(new Color32(255, 255, 255, 255), 16, 16);
        playerSprite = MakeSprite(new Color32(255, 255, 255, 255), 48, 96);
        lampSprite = MakeSprite(new Color32(255, 255, 255, 255), 48, 48);
    }

    private static Sprite MakeSprite(Color32 color, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 64f);
    }
}
