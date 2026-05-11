using System.IO;
using JianDeng;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ChapterOneSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Chapter1_GuiZhen_MVP.unity";
    private const string PlaceholderPath = "Assets/Art/Placeholders";

    [MenuItem("JianDeng/Build Chapter 1 MVP Scene")]
    public static void BuildChapterOneMvpScene()
    {
        EnsurePlaceholderSprites();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Chapter1_GuiZhen_MVP";

        Camera camera = CreateCamera();
        GameObject gameRoot = new GameObject("ChapterOne_MVP");
        GameObject sceneRootParent = new GameObject("Scenes");

        Transform player = CreatePlayer();
        ChapterOneGame game = player.gameObject.AddComponent<ChapterOneGame>();
        game.player = player;
        game.mainCamera = camera;

        BuildScene(ChapterScene.TownGate, sceneRootParent.transform, "镇口", new Color32(68, 64, 54, 255), new Color32(37, 55, 45, 255));
        BuildTownGate(sceneRootParent.transform);
        BuildScene(ChapterScene.StoneBridge, sceneRootParent.transform, "石桥", new Color32(55, 62, 61, 255), new Color32(34, 45, 47, 255));
        BuildStoneBridge(sceneRootParent.transform);
        BuildScene(ChapterScene.GrandmaHouse, sceneRootParent.transform, "外婆家", new Color32(73, 61, 49, 255), new Color32(43, 38, 35, 255));
        BuildGrandmaHouse(sceneRootParent.transform);
        BuildScene(ChapterScene.MourningHall, sceneRootParent.transform, "灵堂", new Color32(51, 46, 43, 255), new Color32(61, 37, 35, 255));
        BuildMourningHall(sceneRootParent.transform);
        BuildScene(ChapterScene.OldWell, sceneRootParent.transform, "老井", new Color32(40, 52, 48, 255), new Color32(24, 34, 35, 255));
        GameObject hiddenName = BuildOldWell(sceneRootParent.transform);

        Canvas canvas = CreateCanvas();
        game.locationText = CreateText(canvas.transform, "Location", "第一章：归镇", 24, TextAnchor.UpperLeft, new Vector2(24, -20), new Vector2(760, 44));
        game.promptText = CreateText(canvas.transform, "Prompt", "", 22, TextAnchor.MiddleCenter, new Vector2(0, 86), new Vector2(360, 46), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        game.dialogueText = CreateText(canvas.transform, "Dialogue", "", 22, TextAnchor.LowerLeft, new Vector2(32, 26), new Vector2(1160, 96), new Vector2(0f, 0f), new Vector2(0f, 0f));

        GameObject inventoryPanel = CreatePanel(canvas.transform, "InventoryPanel", new Color32(25, 24, 22, 226), new Vector2(-22, -72), new Vector2(310, 360), new Vector2(1f, 1f), new Vector2(1f, 1f));
        Text inventoryText = CreateText(inventoryPanel.transform, "InventoryText", "背包", 20, TextAnchor.UpperLeft, new Vector2(18, -18), new Vector2(274, 320), new Vector2(0f, 1f), new Vector2(0f, 1f));
        inventoryPanel.SetActive(false);
        game.inventoryPanel = inventoryPanel;
        game.inventoryText = inventoryText;

        GameObject overlay = CreatePanel(canvas.transform, "LampShadowOverlay", new Color32(12, 18, 16, 116), Vector2.zero, Vector2.zero);
        overlay.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        overlay.GetComponent<RectTransform>().anchorMax = Vector2.one;
        overlay.SetActive(false);
        game.lampShadowOverlay = overlay;

        GameObject endingPanel = CreatePanel(canvas.transform, "ChapterEndingPanel", new Color32(16, 13, 12, 242), Vector2.zero, new Vector2(620, 360), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        game.endingText = CreateText(endingPanel.transform, "EndingText", "", 24, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(560, 300), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        endingPanel.SetActive(false);
        game.endingPanel = endingPanel;
        game.hiddenWellName = hiddenName;
        game.blackLampFlame = GameObject.Find("BlackLamp_Flame");
        if (game.blackLampFlame != null)
        {
            game.blackLampFlame.SetActive(false);
        }

        gameRoot.transform.SetAsFirstSibling();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        Selection.activeGameObject = player.gameObject;
        Debug.Log("Built playable Chapter 1 MVP scene at " + ScenePath);
    }

    private static Camera CreateCamera()
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        Camera camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.5f;
        camera.backgroundColor = new Color32(18, 20, 18, 255);
        go.transform.position = new Vector3(0f, 0f, -10f);
        return camera;
    }

    private static Transform CreatePlayer()
    {
        GameObject player = new GameObject("Player_LinZhaoying");
        player.transform.position = new Vector3(-6.8f, -2.15f, 0f);
        SpriteRenderer body = player.AddComponent<SpriteRenderer>();
        body.sprite = LoadSprite("player");
        body.sortingOrder = 20;
        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.55f, 1.8f);

        CreateSprite("HandLamp", "lamp", player.transform, new Vector3(0.38f, -0.16f, 0f), new Vector3(0.34f, 0.34f, 1f), 22);
        return player.transform;
    }

    private static void BuildScene(ChapterScene scene, Transform parent, string title, Color32 sky, Color32 ground)
    {
        GameObject root = new GameObject(scene.ToString() + "_" + title);
        root.transform.SetParent(parent);
        SceneRoot marker = root.AddComponent<SceneRoot>();
        marker.scene = scene;

        CreateSprite("Backdrop", "block", root.transform, Vector3.zero, new Vector3(18f, 7.5f, 1f), 0, sky);
        CreateSprite("Ground", "block", root.transform, new Vector3(0f, -3.25f, 0f), new Vector3(18f, 1.2f, 1f), 3, ground);
        CreateTextMesh(title, root.transform, new Vector3(-7.3f, 2.65f, 0f), 28, new Color32(179, 169, 145, 255), 10);
    }

    private static void BuildTownGate(Transform parent)
    {
        Transform root = parent.Find("TownGate_镇口");
        CreateSprite("TownGate_Arch", "gate", root, new Vector3(-2.1f, -1.15f, 0f), new Vector3(2.8f, 2.3f, 1f), 8, new Color32(79, 58, 48, 255));
        CreateSprite("PaperAsh", "paper", root, new Vector3(2.2f, -2.35f, 0f), new Vector3(0.8f, 0.5f, 1f), 9, new Color32(133, 128, 112, 255));
        DialogueInteractable sign = CreateInteractable<DialogueInteractable>("TownGate_Sign", root, new Vector3(-2.1f, -1.2f, 0f), new Vector2(1.8f, 2f));
        sign.prompt = "查看镇口灯牌";
        sign.normalLine = "渡灯镇三个字褪得发灰。灯牌背面刻着：名字会引魂。";
        sign.lampShadowLine = "灯影里多出一行小字：河灯上写着你的名字。";
        CreatePortal("To_StoneBridge", root, new Vector3(7.25f, -2.1f, 0f), ChapterScene.StoneBridge, new Vector3(-7f, -2.15f, 0f), "前往石桥", "石桥下水声很浅，像有人在纸后面呼吸。");
    }

    private static void BuildStoneBridge(Transform parent)
    {
        Transform root = parent.Find("StoneBridge_石桥");
        CreateSprite("Bridge", "bridge", root, new Vector3(0f, -1.85f, 0f), new Vector3(6.2f, 1.1f, 1f), 8, new Color32(74, 73, 66, 255));
        CreateSprite("River", "block", root, new Vector3(0f, -2.75f, 0f), new Vector3(18f, 0.8f, 1f), 4, new Color32(28, 43, 44, 255));
        DialogueInteractable river = CreateInteractable<DialogueInteractable>("River_Lantern", root, new Vector3(1.6f, -2.1f, 0f), new Vector2(1.2f, 1f));
        river.prompt = "捞起河灯";
        river.normalLine = "河灯湿透了，灯面没有字。";
        river.lampShadowLine = "灯影映出你的名字：林照萤。字迹像刚干的血。";
        CreatePortal("Back_To_TownGate", root, new Vector3(-7.25f, -2.1f, 0f), ChapterScene.TownGate, new Vector3(6.8f, -2.15f, 0f), "返回镇口", "");
        CreatePortal("To_GrandmaHouse", root, new Vector3(7.25f, -2.1f, 0f), ChapterScene.GrandmaHouse, new Vector3(-7f, -2.15f, 0f), "前往外婆家", "外婆家门半掩，屋里摆着不该出现的白幡。");
    }

    private static void BuildGrandmaHouse(Transform parent)
    {
        Transform root = parent.Find("GrandmaHouse_外婆家");
        CreateSprite("House", "house", root, new Vector3(-0.6f, -1.25f, 0f), new Vector3(5f, 3f, 1f), 8, new Color32(84, 63, 49, 255));
        CreateSprite("Door", "block", root, new Vector3(0.5f, -1.85f, 0f), new Vector3(0.9f, 1.8f, 1f), 9, new Color32(34, 29, 27, 255));
        DialogueInteractable granny = CreateInteractable<DialogueInteractable>("Grandma_Room", root, new Vector3(0.4f, -1.7f, 0f), new Vector2(2f, 2f));
        granny.prompt = "进入堂屋";
        granny.normalLine = "桌上压着旧纸条：供品顺序为米、酒、香。";
        granny.lampShadowLine = "灯影下，外婆的相框背面写着：点黑灯，照老井。";
        CreatePortal("Back_To_Bridge", root, new Vector3(-7.25f, -2.1f, 0f), ChapterScene.StoneBridge, new Vector3(6.8f, -2.15f, 0f), "返回石桥", "");
        CreatePortal("To_MourningHall", root, new Vector3(7.25f, -2.1f, 0f), ChapterScene.MourningHall, new Vector3(-7f, -2.15f, 0f), "前往灵堂", "灵堂里没有哭声，只有蜡烛烧纸芯的轻响。");
    }

    private static void BuildMourningHall(Transform parent)
    {
        Transform root = parent.Find("MourningHall_灵堂");
        CreateSprite("Altar", "altar", root, new Vector3(0f, -1.85f, 0f), new Vector3(3.4f, 1.3f, 1f), 8, new Color32(75, 45, 41, 255));
        CreateSprite("Portrait", "paper", root, new Vector3(0f, -0.45f, 0f), new Vector3(1.1f, 1.35f, 1f), 9, new Color32(196, 188, 164, 255));
        CreateInteractable<OfferingInteractable>("Offering_Rice", root, new Vector3(-1.9f, -2f, 0f), new Vector2(0.8f, 0.8f)).offering = Offering.Rice;
        CreateInteractable<OfferingInteractable>("Offering_Wine", root, new Vector3(0f, -2f, 0f), new Vector2(0.8f, 0.8f)).offering = Offering.Wine;
        CreateInteractable<OfferingInteractable>("Offering_Incense", root, new Vector3(1.9f, -2f, 0f), new Vector2(0.8f, 0.8f)).offering = Offering.Incense;
        CreateSprite("Rice_Visual", "bowl", root, new Vector3(-1.9f, -2f, 0f), new Vector3(0.48f, 0.48f, 1f), 12, new Color32(188, 181, 152, 255));
        CreateSprite("Wine_Visual", "jar", root, new Vector3(0f, -2f, 0f), new Vector3(0.5f, 0.62f, 1f), 12, new Color32(92, 30, 30, 255));
        CreateSprite("Incense_Visual", "incense", root, new Vector3(1.9f, -2f, 0f), new Vector3(0.58f, 0.7f, 1f), 12, new Color32(117, 95, 74, 255));
        CreateTextMesh("米", root, new Vector3(-2.13f, -2.65f, 0f), 22, Color.white, 30);
        CreateTextMesh("酒", root, new Vector3(-0.23f, -2.65f, 0f), 22, Color.white, 30);
        CreateTextMesh("香", root, new Vector3(1.67f, -2.65f, 0f), 22, Color.white, 30);
        BlackLampInteractable blackLamp = CreateInteractable<BlackLampInteractable>("BlackLamp", root, new Vector3(0f, -0.95f, 0f), new Vector2(1f, 1f));
        CreateSprite("BlackLamp_Body", "lamp", root, new Vector3(0f, -0.95f, 0f), new Vector3(0.72f, 0.72f, 1f), 13, new Color32(14, 13, 12, 255));
        CreateSprite("BlackLamp_Flame", "flame", root, new Vector3(0f, -0.53f, 0f), new Vector3(0.35f, 0.44f, 1f), 14, new Color32(186, 72, 43, 255));
        CreatePortal("Back_To_GrandmaHouse", root, new Vector3(-7.25f, -2.1f, 0f), ChapterScene.GrandmaHouse, new Vector3(6.8f, -2.15f, 0f), "返回外婆家", "");
        CreatePortal("To_OldWell", root, new Vector3(7.25f, -2.1f, 0f), ChapterScene.OldWell, new Vector3(-7f, -2.15f, 0f), "前往老井", "井边的纸灰没有落地，像停在一场倒放的雪里。");
    }

    private static GameObject BuildOldWell(Transform parent)
    {
        Transform root = parent.Find("OldWell_老井");
        CreateSprite("OldWell", "well", root, new Vector3(0.2f, -1.7f, 0f), new Vector3(2f, 1.6f, 1f), 10, new Color32(55, 60, 55, 255));
        OldWellInteractable well = CreateInteractable<OldWellInteractable>("OldWell_Interact", root, new Vector3(0.2f, -1.7f, 0f), new Vector2(2f, 1.8f));
        GameObject hiddenName = CreateTextMesh("林照萤 / 陈望月替死", root, new Vector3(-1.55f, -0.55f, 0f), 22, new Color32(151, 47, 39, 255), 30);
        hiddenName.name = "Hidden_Well_Name_LampShadowOnly";
        hiddenName.SetActive(false);
        CreatePortal("Back_To_MourningHall", root, new Vector3(-7.25f, -2.1f, 0f), ChapterScene.MourningHall, new Vector3(6.8f, -2.15f, 0f), "返回灵堂", "");
        return hiddenName;
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

    private static ScenePortal CreatePortal(string name, Transform parent, Vector3 position, ChapterScene target, Vector3 spawn, string prompt, string line)
    {
        ScenePortal portal = CreateInteractable<ScenePortal>(name, parent, position, new Vector2(1f, 2.3f));
        portal.targetScene = target;
        portal.spawnPosition = spawn;
        portal.prompt = prompt;
        portal.arrivalLine = line;
        CreateSprite(name + "_Marker", "paper", parent, position + Vector3.up * 0.9f, new Vector3(0.35f, 0.6f, 1f), 18, new Color32(207, 187, 120, 255));
        return portal;
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

    private static GameObject CreatePanel(Transform parent, string name, Color32 color, Vector2 anchoredPosition, Vector2 size, Vector2? anchorMin = null, Vector2? anchorMax = null)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin ?? new Vector2(0.5f, 0.5f);
        rect.anchorMax = anchorMax ?? new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return panel;
    }

    private static Text CreateText(Transform parent, string name, string text, int size, TextAnchor alignment, Vector2 position, Vector2 rectSize, Vector2? anchorMin = null, Vector2? anchorMax = null)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text label = go.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = size;
        label.alignment = alignment;
        label.color = new Color32(225, 215, 188, 255);
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin ?? new Vector2(0f, 1f);
        rect.anchorMax = anchorMax ?? new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = rectSize;
        return label;
    }

    private static GameObject CreateTextMesh(string text, Transform parent, Vector3 position, int size, Color color, int order)
    {
        GameObject go = new GameObject("Text_" + text);
        go.transform.SetParent(parent);
        go.transform.position = position;
        TextMesh mesh = go.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.fontSize = size;
        mesh.characterSize = 0.08f;
        mesh.anchor = TextAnchor.MiddleLeft;
        mesh.color = color;
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sortingOrder = order;
        return go;
    }

    private static GameObject CreateSprite(string name, string spriteName, Transform parent, Vector3 position, Vector3 scale, int order)
    {
        return CreateSprite(name, spriteName, parent, position, scale, order, Color.white);
    }

    private static GameObject CreateSprite(string name, string spriteName, Transform parent, Vector3 position, Vector3 scale, int order, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = scale;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite(spriteName);
        renderer.color = color;
        renderer.sortingOrder = order;
        return go;
    }

    private static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderPath + "/" + name + ".png");
    }

    private static void EnsurePlaceholderSprites()
    {
        Directory.CreateDirectory(PlaceholderPath);
        CreateTexture("block", 16, 16, new Color32(255, 255, 255, 255));
        CreateTexture("player", 48, 96, new Color32(68, 54, 47, 255));
        CreateTexture("lamp", 48, 48, new Color32(242, 178, 78, 255));
        CreateTexture("flame", 32, 48, new Color32(249, 177, 74, 255));
        CreateTexture("gate", 96, 80, new Color32(105, 78, 62, 255));
        CreateTexture("bridge", 128, 32, new Color32(105, 102, 92, 255));
        CreateTexture("house", 128, 96, new Color32(112, 84, 63, 255));
        CreateTexture("altar", 96, 48, new Color32(94, 45, 39, 255));
        CreateTexture("paper", 48, 64, new Color32(197, 190, 163, 255));
        CreateTexture("bowl", 48, 32, new Color32(180, 174, 149, 255));
        CreateTexture("jar", 42, 58, new Color32(82, 31, 31, 255));
        CreateTexture("incense", 42, 58, new Color32(126, 96, 72, 255));
        CreateTexture("well", 96, 76, new Color32(68, 74, 68, 255));
        AssetDatabase.Refresh();

        string[] paths = Directory.GetFiles(PlaceholderPath, "*.png");
        foreach (string path in paths)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace("\\", "/"));
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 64;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }

    private static void CreateTexture(string name, int width, int height, Color32 color)
    {
        string path = PlaceholderPath + "/" + name + ".png";
        if (File.Exists(path))
        {
            return;
        }

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }
}
