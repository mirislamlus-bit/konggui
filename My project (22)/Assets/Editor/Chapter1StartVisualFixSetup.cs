using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class Chapter1StartVisualFixSetup
{
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/Chapter1/Chapter1_TownGate.unity",
        "Assets/Scenes/Chapter1/Chapter1_StoneBridge.unity",
        "Assets/Scenes/Chapter1/Chapter1_GrandmaHouse.unity",
        "Assets/Scenes/Chapter1/Chapter1_MourningHall.unity",
        "Assets/Scenes/Chapter1/Chapter1_OldWell.unity"
    };

    [MenuItem("JianDeng/Fix Chapter 1 Start Visual State")]
    public static void FixAllScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Fix Chapter 1 Start Visual State can only run in Edit Mode.");
            return;
        }

        foreach (string path in ScenePaths)
        {
            if (!System.IO.File.Exists(path))
            {
                continue;
            }

            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            FixOpenScene();
            EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log("Chapter 1 start visual state fixed.");
    }

    private static void FixOpenScene()
    {
        GameObject sceneRoot = GameObject.Find("SceneRoot");
        if (sceneRoot == null)
        {
            return;
        }

        Transform background = sceneRoot.transform.Find("Background");
        Transform hidden = sceneRoot.transform.Find("HiddenObjects");
        Transform vfx = sceneRoot.transform.Find("VFX");

        SetActive(background != null ? background.Find("Reality_BG") : null, true);
        SetActive(background != null ? background.Find("LanternVision_BG") : null, false);
        SetActive(hidden, false);

        foreach (SpriteRenderer renderer in Object.FindObjectsOfType<SpriteRenderer>(true))
        {
            string objectName = renderer.gameObject.name;
            if (objectName.Contains("LanternVisionOverlay"))
            {
                renderer.enabled = false;
                renderer.gameObject.SetActive(false);
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
                renderer.gameObject.SetActive(false);
                continue;
            }

            if (objectName.Contains("ThinMist") || objectName.Contains("Mist"))
            {
                SetAlpha(renderer, 0.2f);
            }
            else if (objectName.Contains("FallingPaperAsh") || objectName.Contains("PaperAsh"))
            {
                SetAlpha(renderer, 0.16f);
            }
            else if (objectName.Contains("FloatingDust") || objectName.Contains("Dust"))
            {
                SetAlpha(renderer, 0.16f);
            }
        }

        EnsureCanvasOverlay(sceneRoot.transform);
        EnsurePlayerVisible();

        SceneBackgroundSet set = sceneRoot.GetComponent<SceneBackgroundSet>() ?? sceneRoot.AddComponent<SceneBackgroundSet>();
        set.realityBackground = background != null && background.Find("Reality_BG") != null ? background.Find("Reality_BG").gameObject : null;
        set.lanternVisionBackground = background != null && background.Find("LanternVision_BG") != null ? background.Find("LanternVision_BG").gameObject : null;
        set.hiddenObjects = hidden != null ? hidden.gameObject : null;

        LanternVisionController controller = sceneRoot.GetComponent<LanternVisionController>() ?? sceneRoot.AddComponent<LanternVisionController>();
        controller.SetLanternVision(false);
    }

    private static void EnsureCanvasOverlay(Transform sceneRoot)
    {
        GameObject canvasObject = GameObject.Find("Chapter1_UI_Canvas") ?? new GameObject("Chapter1_UI_Canvas");
        canvasObject.transform.SetParent(sceneRoot, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>() ?? canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        if (canvasObject.GetComponent<CanvasScaler>() == null)
        {
            canvasObject.AddComponent<CanvasScaler>();
        }
        if (canvasObject.GetComponent<GraphicRaycaster>() == null)
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject overlay = FindOrCreateChild(canvasObject.transform, "LanternVisionOverlay_UI");
        RectTransform rect = overlay.GetComponent<RectTransform>() ?? overlay.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = overlay.GetComponent<Image>() ?? overlay.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.32f);
        image.raycastTarget = false;
        overlay.SetActive(false);
    }

    private static void EnsurePlayerVisible()
    {
        GameObject player = GameObject.Find("Player_LinZhaoying");
        if (player == null)
        {
            return;
        }

        player.transform.position = new Vector3(-5f, -2.78f, 0f);
        player.transform.localScale = Vector3.one * 1.12f;
        SpriteRenderer renderer = player.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
            renderer.sortingLayerName = "Character";
            renderer.sortingOrder = 50;
            SetAlpha(renderer, 1f);
        }
    }

    private static GameObject FindOrCreateChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            return child.gameObject;
        }

        GameObject created = new GameObject(name);
        created.transform.SetParent(parent, false);
        return created;
    }

    private static void SetActive(Transform target, bool active)
    {
        if (target != null)
        {
            target.gameObject.SetActive(active);
        }
    }

    private static void SetAlpha(SpriteRenderer renderer, float alpha)
    {
        Color color = renderer.color;
        color.a = alpha;
        renderer.color = color;
    }
}
