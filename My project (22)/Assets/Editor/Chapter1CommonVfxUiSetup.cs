using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Chapter1CommonVfxUiSetup
{
    private const string SceneDirectory = "Assets/Scenes/Chapter1";
    private const string MarkerName = "Chapter1CommonVfxAppliedMarker";

    private static readonly string[] FrameFolders =
    {
        "Assets/Art/VFX/Atmosphere/36_candle_flame",
        "Assets/Art/VFX/Atmosphere/38_thin_mist",
        "Assets/Art/VFX/Atmosphere/39_floating_dust",
        "Assets/Art/VFX/Atmosphere/40_falling_paper_ash",
        "Assets/Art/VFX/Atmosphere/41_incense_smoke",
        "Assets/Art/VFX/Water/42_river_reflection",
        "Assets/Art/VFX/Water/43_water_ripples",
        "Assets/Art/VFX/LanternVision/44_afterimage_flash"
    };

    private static readonly string[] SingleSpritePaths =
    {
        "Assets/Art/VFX/LanternVision/37_black_lantern_glow.png",
        "Assets/Art/VFX/LanternVision/45_vignette_overlay.png",
        "Assets/Art/UI/46_paper_dialog_box.png",
        "Assets/Art/UI/47_character_name_box.png",
        "Assets/Art/UI/48_e_interaction_prompt.png",
        "Assets/Art/UI/49_q_lantern_view_prompt.png",
        "Assets/Art/UI/50_investigate_icon.png",
        "Assets/Art/UI/51_inventory_base_ui.png",
        "Assets/Art/UI/52_item_slot.png",
        "Assets/Art/UI/53_item_description_box.png",
        "Assets/Art/UI/54_seal_button.png"
    };

    private static readonly VfxClipDefinition[] ClipDefinitions =
    {
        new VfxClipDefinition("CandleFlame", "Assets/Art/VFX/Atmosphere/36_candle_flame", "Assets/Art/VFX/Atmosphere/Animations/36_candle_flame.anim", 10f),
        new VfxClipDefinition("ThinMist", "Assets/Art/VFX/Atmosphere/38_thin_mist", "Assets/Art/VFX/Atmosphere/Animations/38_thin_mist.anim", 6f),
        new VfxClipDefinition("FloatingDust", "Assets/Art/VFX/Atmosphere/39_floating_dust", "Assets/Art/VFX/Atmosphere/Animations/39_floating_dust.anim", 6f),
        new VfxClipDefinition("FallingPaperAsh", "Assets/Art/VFX/Atmosphere/40_falling_paper_ash", "Assets/Art/VFX/Atmosphere/Animations/40_falling_paper_ash.anim", 7f),
        new VfxClipDefinition("IncenseSmoke", "Assets/Art/VFX/Atmosphere/41_incense_smoke", "Assets/Art/VFX/Atmosphere/Animations/41_incense_smoke.anim", 7f),
        new VfxClipDefinition("RiverReflection", "Assets/Art/VFX/Water/42_river_reflection", "Assets/Art/VFX/Water/Animations/42_river_reflection.anim", 6f),
        new VfxClipDefinition("WaterRipples", "Assets/Art/VFX/Water/43_water_ripples", "Assets/Art/VFX/Water/Animations/43_water_ripples.anim", 8f),
        new VfxClipDefinition("AfterimageFlash", "Assets/Art/VFX/LanternVision/44_afterimage_flash", "Assets/Art/VFX/LanternVision/Animations/44_afterimage_flash.anim", 8f)
    };

    [InitializeOnLoadMethod]
    private static void AutoApplyAfterImport()
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

            if (!File.Exists(SceneDirectory + "/Chapter1_TownGate.unity"))
            {
                return;
            }

            if (AnySceneNeedsCommonVfx())
            {
                ApplyCommonVfxAndUi();
                return;
            }

            if (AllUiSpritesExist() && !File.Exists("Assets/Prefabs/UI/Chapter1_HUD.prefab"))
            {
                ConfigureImports();
                TryCreateHudPrefab();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        };
    }

    [MenuItem("JianDeng/Apply Chapter 1 Common VFX and UI")]
    public static void ApplyCommonVfxAndUi()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Apply Chapter 1 Common VFX and UI can only run in Edit Mode.");
            return;
        }

        ConfigureImports();
        CreateAnimationClips();
        AssetDatabase.Refresh();

        ApplyTownGate();
        ApplyStoneBridge();
        ApplyGrandmaHouse();
        ApplyMourningHall();
        ApplyOldWell();
        TryCreateHudPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Applied Chapter 1 common VFX. UI prefab is created only when UI PNG assets exist.");
    }

    private static bool AnySceneNeedsCommonVfx()
    {
        string[] scenes =
        {
            "Chapter1_TownGate",
            "Chapter1_StoneBridge",
            "Chapter1_GrandmaHouse",
            "Chapter1_MourningHall",
            "Chapter1_OldWell"
        };

        foreach (string sceneName in scenes)
        {
            string scenePath = SceneDirectory + "/" + sceneName + ".unity";
            if (!File.Exists(scenePath))
            {
                continue;
            }

            if (!File.ReadAllText(scenePath).Contains("m_Name: " + MarkerName))
            {
                return true;
            }
        }

        return false;
    }

    private static void ConfigureImports()
    {
        foreach (string path in EnumerateRequestedPngs())
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
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

        foreach (string path in SingleSpritePaths)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning("Missing requested UI/VFX sprite: " + path);
            }
        }
    }

    private static IEnumerable<string> EnumerateRequestedPngs()
    {
        foreach (string folder in FrameFolders)
        {
            if (!Directory.Exists(folder))
            {
                Debug.LogWarning("Missing requested VFX folder: " + folder);
                continue;
            }

            foreach (string path in Directory.GetFiles(folder, "*.png").OrderBy(path => path))
            {
                yield return NormalizeAssetPath(path);
            }
        }

        foreach (string path in SingleSpritePaths)
        {
            if (File.Exists(path))
            {
                yield return path;
            }
        }
    }

    private static void CreateAnimationClips()
    {
        foreach (VfxClipDefinition definition in ClipDefinitions)
        {
            string[] framePaths = GetFramePaths(definition.FrameFolder);
            if (framePaths.Length == 0)
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(definition.ClipPath));

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(definition.ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, definition.ClipPath);
            }

            clip.name = Path.GetFileNameWithoutExtension(definition.ClipPath);
            clip.frameRate = definition.FrameRate;

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[framePaths.Length + 1];
            for (int i = 0; i < framePaths.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / definition.FrameRate,
                    value = AssetDatabase.LoadAssetAtPath<Sprite>(framePaths[i])
                };
            }

            keyframes[keyframes.Length - 1] = new ObjectReferenceKeyframe
            {
                time = framePaths.Length / definition.FrameRate,
                value = AssetDatabase.LoadAssetAtPath<Sprite>(framePaths[0])
            };

            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = string.Empty,
                propertyName = "m_Sprite"
            };

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

            SerializedObject serializedClip = new SerializedObject(clip);
            SerializedProperty legacy = serializedClip.FindProperty("m_Legacy");
            if (legacy != null)
            {
                legacy.boolValue = true;
                serializedClip.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(clip);
        }
    }

    private static void ApplyTownGate()
    {
        ApplyToScene("Chapter1_TownGate", sceneRoot =>
        {
            Transform vfx = sceneRoot.Find("VFX");
            Transform hidden = sceneRoot.Find("HiddenObjects");

            CreateAnimatedVfx("ThinMist_TownGate", "ThinMist", vfx, new Vector3(0f, -0.25f, 0f), 80, 1.05f, new Color(1f, 1f, 1f, 0.55f));
            CreateAnimatedVfx("FloatingDust_TownGate", "FloatingDust", vfx, new Vector3(0f, -0.1f, 0f), 82, 0.95f, new Color(1f, 1f, 1f, 0.4f));
            CreateAnimatedVfx("FallingPaperAsh_TownGate", "FallingPaperAsh", vfx, new Vector3(1.6f, -0.2f, 0f), 84, 0.8f, new Color(1f, 1f, 1f, 0.55f));
            CreateAnimatedVfx("AfterimageFlash_TownGate", "AfterimageFlash", hidden, new Vector3(-3.2f, -1.45f, 0f), 34, 0.45f, new Color(1f, 1f, 1f, 0.8f));
            CreateVignette(vfx);
        });
    }

    private static void ApplyStoneBridge()
    {
        ApplyToScene("Chapter1_StoneBridge", sceneRoot =>
        {
            Transform vfx = sceneRoot.Find("VFX");
            Transform hidden = sceneRoot.Find("HiddenObjects");

            CreateAnimatedVfx("ThinMist_StoneBridge", "ThinMist", vfx, new Vector3(0f, -0.15f, 0f), 80, 1.05f, new Color(1f, 1f, 1f, 0.45f));
            CreateAnimatedVfx("RiverReflection_StoneBridge", "RiverReflection", vfx, new Vector3(0f, -2.3f, 0f), 18, 0.62f, new Color(1f, 1f, 1f, 0.65f));
            CreateAnimatedVfx("WaterRipples_StoneBridge", "WaterRipples", vfx, new Vector3(0.2f, -2.35f, 0f), 19, 0.58f, new Color(1f, 1f, 1f, 0.6f));
            CreateAnimatedVfx("AfterimageFlash_StoneBridge", "AfterimageFlash", hidden, new Vector3(1.1f, -1.25f, 0f), 35, 0.48f, new Color(1f, 1f, 1f, 0.85f));
            CreateVignette(vfx);
        });
    }

    private static void ApplyGrandmaHouse()
    {
        ApplyToScene("Chapter1_GrandmaHouse", sceneRoot =>
        {
            Transform vfx = sceneRoot.Find("VFX");
            Transform hidden = sceneRoot.Find("HiddenObjects");

            CreateAnimatedVfx("CandleFlame_GrandmaHouse", "CandleFlame", vfx, new Vector3(-3.1f, -1.05f, 0f), 32, 0.2f, Color.white);
            CreateAnimatedVfx("ThinMist_GrandmaHouse", "ThinMist", vfx, new Vector3(0f, -0.2f, 0f), 80, 0.95f, new Color(1f, 1f, 1f, 0.42f));
            CreateAnimatedVfx("FloatingDust_GrandmaHouse", "FloatingDust", vfx, new Vector3(0.1f, -0.05f, 0f), 82, 0.8f, new Color(1f, 1f, 1f, 0.38f));
            CreateAnimatedVfx("AfterimageFlash_GrandmaHouse", "AfterimageFlash", hidden, new Vector3(1.8f, -1.3f, 0f), 35, 0.46f, new Color(1f, 1f, 1f, 0.8f));
            CreateVignette(vfx);
        });
    }

    private static void ApplyMourningHall()
    {
        ApplyToScene("Chapter1_MourningHall", sceneRoot =>
        {
            Transform vfx = sceneRoot.Find("VFX");
            Transform hidden = sceneRoot.Find("HiddenObjects");

            CreateAnimatedVfx("CandleFlame_Left", "CandleFlame", vfx, new Vector3(-2.1f, -1.28f, 0f), 34, 0.18f, Color.white);
            CreateAnimatedVfx("IncenseSmoke_MourningHall", "IncenseSmoke", vfx, new Vector3(0.9f, -1.3f, 0f), 36, 0.35f, new Color(1f, 1f, 1f, 0.62f));
            CreateAnimatedVfx("FallingPaperAsh_MourningHall", "FallingPaperAsh", vfx, new Vector3(0f, -0.15f, 0f), 84, 0.85f, new Color(1f, 1f, 1f, 0.48f));
            CreateSpriteVfx("BlackLanternGlow_MourningHall", "Assets/Art/VFX/LanternVision/37_black_lantern_glow.png", hidden, new Vector3(2f, -1.95f, 0f), 35, 0.38f, new Color(1f, 1f, 1f, 0.78f));
            CreateAnimatedVfx("AfterimageFlash_MourningHall", "AfterimageFlash", hidden, new Vector3(2.65f, -1.45f, 0f), 36, 0.45f, new Color(1f, 1f, 1f, 0.8f));
            CreateVignette(vfx);
        });
    }

    private static void ApplyOldWell()
    {
        ApplyToScene("Chapter1_OldWell", sceneRoot =>
        {
            Transform vfx = sceneRoot.Find("VFX");
            Transform hidden = sceneRoot.Find("HiddenObjects");

            CreateAnimatedVfx("ThinMist_OldWell", "ThinMist", vfx, new Vector3(0f, -0.15f, 0f), 80, 1f, new Color(1f, 1f, 1f, 0.5f));
            CreateAnimatedVfx("WaterRipples_OldWell", "WaterRipples", hidden, new Vector3(-0.2f, -2.05f, 0f), 32, 0.48f, new Color(1f, 1f, 1f, 0.68f));
            CreateAnimatedVfx("RiverReflection_OldWell", "RiverReflection", hidden, new Vector3(-0.15f, -2.12f, 0f), 31, 0.46f, new Color(1f, 1f, 1f, 0.58f));
            CreateSpriteVfx("BlackLanternGlow_OldWell", "Assets/Art/VFX/LanternVision/37_black_lantern_glow.png", hidden, new Vector3(2.2f, -2.05f, 0f), 35, 0.38f, new Color(1f, 1f, 1f, 0.78f));
            CreateAnimatedVfx("AfterimageFlash_OldWell", "AfterimageFlash", hidden, new Vector3(2.7f, -1.35f, 0f), 36, 0.45f, new Color(1f, 1f, 1f, 0.8f));
            CreateVignette(vfx);
        });
    }

    private static void ApplyToScene(string sceneName, System.Action<Transform> apply)
    {
        string scenePath = SceneDirectory + "/" + sceneName + ".unity";
        if (!File.Exists(scenePath))
        {
            Debug.LogWarning("Missing scene: " + scenePath);
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject rootObject = GameObject.Find("SceneRoot");
        if (rootObject == null)
        {
            Debug.LogWarning("SceneRoot not found in " + scenePath);
            return;
        }

        RemoveExisting(rootObject.transform);
        apply(rootObject.transform);
        EnsureHiddenObjectsDefault(rootObject.transform);
        CreateMarker(rootObject.transform);
        EditorSceneManager.SaveScene(scene);
    }

    private static void RemoveExisting(Transform sceneRoot)
    {
        List<GameObject> toDestroy = new List<GameObject>();
        CollectGenerated(sceneRoot.Find("VFX"), toDestroy);
        CollectGenerated(sceneRoot.Find("HiddenObjects"), toDestroy);

        foreach (GameObject item in toDestroy)
        {
            Object.DestroyImmediate(item);
        }
    }

    private static void CollectGenerated(Transform parent, List<GameObject> toDestroy)
    {
        if (parent == null)
        {
            return;
        }

        foreach (Transform child in parent)
        {
            if (child.name.StartsWith("CommonVFX_") || child.name == MarkerName)
            {
                toDestroy.Add(child.gameObject);
            }
        }
    }

    private static GameObject CreateAnimatedVfx(string name, string clipName, Transform parent, Vector3 position, int sortingOrder, float scale, Color color)
    {
        VfxClipDefinition definition = ClipDefinitions.FirstOrDefault(item => item.Name == clipName);
        if (string.IsNullOrEmpty(definition.Name))
        {
            return null;
        }

        string[] framePaths = GetFramePaths(definition.FrameFolder);
        if (framePaths.Length == 0)
        {
            return null;
        }

        GameObject item = CreateSpriteVfx(name, framePaths[0], parent, position, sortingOrder, scale, color);
        if (item == null)
        {
            return null;
        }

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(definition.ClipPath);
        if (clip != null)
        {
            Animation animation = item.AddComponent<Animation>();
            animation.clip = clip;
            animation.AddClip(clip, clip.name);
            animation.playAutomatically = true;
        }

        return item;
    }

    private static GameObject CreateSpriteVfx(string name, string spritePath, Transform parent, Vector3 position, int sortingOrder, float scale, Color color)
    {
        if (parent == null || !File.Exists(spritePath))
        {
            return null;
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            return null;
        }

        GameObject item = new GameObject("CommonVFX_" + name);
        item.transform.SetParent(parent);
        item.transform.localPosition = position;
        item.transform.localScale = Vector3.one * scale;

        SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        renderer.color = color;
        return item;
    }

    private static void CreateVignette(Transform vfx)
    {
        CreateSpriteVfx("VignetteOverlay", "Assets/Art/VFX/LanternVision/45_vignette_overlay.png", vfx, Vector3.zero, 130, 1f, new Color(1f, 1f, 1f, 0.82f));
    }

    private static void EnsureHiddenObjectsDefault(Transform sceneRoot)
    {
        Transform hidden = sceneRoot.Find("HiddenObjects");
        if (hidden != null)
        {
            hidden.gameObject.SetActive(false);
        }
    }

    private static void CreateMarker(Transform sceneRoot)
    {
        Transform vfx = sceneRoot.Find("VFX");
        if (vfx == null)
        {
            return;
        }

        GameObject marker = new GameObject(MarkerName);
        marker.transform.SetParent(vfx);
        marker.transform.localPosition = Vector3.zero;
    }

    private static string[] GetFramePaths(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return new string[0];
        }

        return Directory.GetFiles(folder, "*.png")
            .Select(NormalizeAssetPath)
            .OrderBy(path => path)
            .ToArray();
    }

    private static string NormalizeAssetPath(string path)
    {
        return path.Replace("\\", "/");
    }

    private static void TryCreateHudPrefab()
    {
        if (!AllUiSpritesExist())
        {
            Debug.LogWarning("Chapter 1 UI prefab skipped because one or more UI sprites under Assets/Art/UI are missing.");
            return;
        }

        Directory.CreateDirectory("Assets/Prefabs/UI");

        GameObject canvasObject = new GameObject("Chapter1_HUD");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        UnityEngine.UI.CanvasScaler scaler = canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        CreateUiImage("DialogueBox", "Assets/Art/UI/46_paper_dialog_box.png", canvasObject.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 92f), new Vector2(920f, 184f), true);
        CreateUiImage("NameBox", "Assets/Art/UI/47_character_name_box.png", canvasObject.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(170f, 210f), new Vector2(260f, 72f), true);
        CreateUiImage("InteractionPrompt", "Assets/Art/UI/48_e_interaction_prompt.png", canvasObject.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 245f), new Vector2(128f, 64f), false);
        CreateUiImage("LanternPrompt", "Assets/Art/UI/49_q_lantern_view_prompt.png", canvasObject.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-128f, -72f), new Vector2(192f, 64f), true);
        CreateUiImage("InvestigateIcon", "Assets/Art/UI/50_investigate_icon.png", canvasObject.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 64f), false);
        GameObject inventoryPanel = CreateUiImage("InventoryPanel", "Assets/Art/UI/51_inventory_base_ui.png", canvasObject.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-260f, 0f), new Vector2(460f, 560f), false);
        CreateInventoryChildren(inventoryPanel.transform);

        PrefabUtility.SaveAsPrefabAsset(canvasObject, "Assets/Prefabs/UI/Chapter1_HUD.prefab");
        Object.DestroyImmediate(canvasObject);
    }

    private static bool AllUiSpritesExist()
    {
        return SingleSpritePaths
            .Where(path => path.StartsWith("Assets/Art/UI/"))
            .All(File.Exists);
    }

    private static void CreateInventoryChildren(Transform inventoryPanel)
    {
        const string slotPath = "Assets/Art/UI/52_item_slot.png";
        const string descriptionPath = "Assets/Art/UI/53_item_description_box.png";
        const string sealButtonPath = "Assets/Art/UI/54_seal_button.png";

        for (int row = 0; row < 2; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                Vector2 position = new Vector2(-150f + column * 100f, 130f - row * 100f);
                CreateUiImage("ItemSlot_" + (row * 4 + column + 1).ToString("00"), slotPath, inventoryPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(78f, 78f), true);
            }
        }

        CreateUiImage("ItemDescriptionBox", descriptionPath, inventoryPanel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 88f), new Vector2(360f, 132f), true);
        CreateUiImage("SealButton", sealButtonPath, inventoryPanel, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-72f, 52f), new Vector2(96f, 72f), true);
    }

    private static GameObject CreateUiImage(string name, string spritePath, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, bool active)
    {
        GameObject item = new GameObject(name);
        item.transform.SetParent(parent, false);

        RectTransform rectTransform = item.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        UnityEngine.UI.Image image = item.AddComponent<UnityEngine.UI.Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        image.preserveAspect = true;
        item.SetActive(active);
        return item;
    }

    private readonly struct VfxClipDefinition
    {
        public readonly string Name;
        public readonly string FrameFolder;
        public readonly string ClipPath;
        public readonly float FrameRate;

        public VfxClipDefinition(string name, string frameFolder, string clipPath, float frameRate)
        {
            Name = name;
            FrameFolder = frameFolder;
            ClipPath = clipPath;
            FrameRate = frameRate;
        }
    }
}
