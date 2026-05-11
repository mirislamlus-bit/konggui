using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Chapter1ContentSetup
{
    // Places chapter-one character, prop, clue, and lantern-vision sprites into the five scene files.
    private const string SceneDirectory = "Assets/Scenes/Chapter1";

    private static readonly string[] ImportPaths =
    {
        "Assets/Art/Characters/Ghosts/13_grandmother_afterimage.png",
        "Assets/Art/Characters/Ghosts/14_river_lantern_ghost.png",
        "Assets/Art/Characters/PaperEffigy/15_paper_effigy.png",
        "Assets/Art/Characters/PaperEffigy/16_paper_effigy_altered.png",
        "Assets/Art/Props/Lantern/17_black_lantern_unlit.png",
        "Assets/Art/Props/Lantern/18_black_lantern_lit.png",
        "Assets/Art/Props/RiverLantern/19_river_lantern_normal.png",
        "Assets/Art/Props/RiverLantern/20_river_lantern_named.png",
        "Assets/Art/Props/Offerings/21_offering_set_full.png",
        "Assets/Art/Props/Offerings/22_apple_single.png",
        "Assets/Art/Props/Offerings/23_pastry_single.png",
        "Assets/Art/Props/Offerings/24_wine_cup_single.png",
        "Assets/Art/Props/Offerings/25_incense_burner_single.png",
        "Assets/Art/Props/Offerings/26_white_candle_unlit.png",
        "Assets/Art/Props/Offerings/27_white_candle_lit.png",
        "Assets/Art/Props/Offerings/28_offering_tray.png",
        "Assets/Art/Props/OldWell/29_well_rope_and_bucket.png",
        "Assets/Art/Props/Clues/30_clue_paper_ritual_note.png",
        "Assets/Art/VFX/LanternVision/31_lantern_vision_filter.png",
        "Assets/Art/VFX/LanternVision/32_hidden_text_01.png",
        "Assets/Art/VFX/LanternVision/32_hidden_text_02.png",
        "Assets/Art/VFX/LanternVision/32_hidden_text_03.png",
        "Assets/Art/VFX/LanternVision/33_footprints_guide_01.png",
        "Assets/Art/VFX/LanternVision/33_footprints_guide_02.png",
        "Assets/Art/VFX/LanternVision/34_water_reflection_effect.png",
        "Assets/Art/VFX/LanternVision/35_name_in_well_effect.png",
        "Assets/Art/VFX/LanternVision/32_hidden_text_sheet_01.png",
        "Assets/Art/VFX/LanternVision/32_hidden_text_sheet_02.png",
        "Assets/Art/VFX/LanternVision/32_hidden_text_sheet_03.png",
        "Assets/Art/VFX/LanternVision/33_footprints_guide_sheet_01.png",
        "Assets/Art/VFX/LanternVision/33_footprints_guide_sheet_02.png"
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

            if (AnySceneNeedsContent())
            {
                ApplyContent();
            }
        };
    }

    [MenuItem("JianDeng/Apply Chapter 1 Content Assets")]
    public static void ApplyContent()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Apply Chapter 1 Content Assets can only run in Edit Mode.");
            return;
        }

        ConfigureImports();
        AssetDatabase.Refresh();

        ApplyTownGate();
        ApplyStoneBridge();
        ApplyGrandmaHouse();
        ApplyMourningHall();
        ApplyOldWell();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Applied Chapter 1 content assets to all five scenes.");
    }

    private static bool AnySceneNeedsContent()
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

            string text = File.ReadAllText(scenePath);
            if (!text.Contains("m_Name: Chapter1ContentScaleV2Marker"))
            {
                return true;
            }
        }

        return false;
    }

    private static void ConfigureImports()
    {
        foreach (string path in ImportPaths)
        {
            if (!File.Exists(path))
            {
                continue;
            }

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
    }

    private static void ApplyTownGate()
    {
        ApplyToScene("Chapter1_TownGate", sceneRoot =>
        {
            Transform hidden = sceneRoot.Find("HiddenObjects");
            Transform interactables = sceneRoot.Find("Interactables");
            Transform vfx = sceneRoot.Find("VFX");

            CreateSprite("PaperEffigy_Normal", "Assets/Art/Characters/PaperEffigy/15_paper_effigy.png", interactables, new Vector3(-3.2f, -2.1f, 0f), 20, 0.38f);
            CreateSprite("PaperEffigy_Altered_LanternOnly", "Assets/Art/Characters/PaperEffigy/16_paper_effigy_altered.png", hidden, new Vector3(-3.2f, -2.1f, 0f), 21, 0.38f);
            CreateSprite("HiddenText_TownGate", ResolveHiddenText(1), hidden, new Vector3(1.2f, -0.65f, 0f), 30, 0.45f);
            CreateSprite("FootprintsGuide_TownGate", ResolveFootprints(1), hidden, new Vector3(2.4f, -2.55f, 0f), 25, 0.38f);
            SetOverlaySprite(vfx);
        });
    }

    private static void ApplyStoneBridge()
    {
        ApplyToScene("Chapter1_StoneBridge", sceneRoot =>
        {
            Transform hidden = sceneRoot.Find("HiddenObjects");
            Transform interactables = sceneRoot.Find("Interactables");
            Transform vfx = sceneRoot.Find("VFX");

            CreateSprite("RiverLantern_Normal", "Assets/Art/Props/RiverLantern/19_river_lantern_normal.png", interactables, new Vector3(-1.6f, -2.35f, 0f), 20, 0.32f);
            CreateSprite("RiverLantern_Named_LanternOnly", "Assets/Art/Props/RiverLantern/20_river_lantern_named.png", hidden, new Vector3(-1.6f, -2.35f, 0f), 24, 0.32f);
            CreateSprite("RiverLantern_Ghost", "Assets/Art/Characters/Ghosts/14_river_lantern_ghost.png", hidden, new Vector3(1.1f, -1.25f, 0f), 26, 0.42f);
            CreateSprite("WaterReflection_LanternOnly", "Assets/Art/VFX/LanternVision/34_water_reflection_effect.png", hidden, new Vector3(0.4f, -2.45f, 0f), 23, 0.45f);
            CreateSprite("HiddenText_StoneBridge", ResolveHiddenText(2), hidden, new Vector3(2.1f, -0.8f, 0f), 30, 0.45f);
            SetOverlaySprite(vfx);
        });
    }

    private static void ApplyGrandmaHouse()
    {
        ApplyToScene("Chapter1_GrandmaHouse", sceneRoot =>
        {
            Transform hidden = sceneRoot.Find("HiddenObjects");
            Transform interactables = sceneRoot.Find("Interactables");
            Transform vfx = sceneRoot.Find("VFX");

            CreateSprite("Clue_RitualNote", "Assets/Art/Props/Clues/30_clue_paper_ritual_note.png", interactables, new Vector3(-2.2f, -1.95f, 0f), 20, 0.32f);
            CreateSprite("Grandmother_Afterimage", "Assets/Art/Characters/Ghosts/13_grandmother_afterimage.png", hidden, new Vector3(1.8f, -1.3f, 0f), 25, 0.42f);
            CreateSprite("FootprintsGuide_GrandmaHouse", ResolveFootprints(2), hidden, new Vector3(0.2f, -2.55f, 0f), 25, 0.38f);
            SetOverlaySprite(vfx);
        });
    }

    private static void ApplyMourningHall()
    {
        ApplyToScene("Chapter1_MourningHall", sceneRoot =>
        {
            Transform hidden = sceneRoot.Find("HiddenObjects");
            Transform interactables = sceneRoot.Find("Interactables");
            Transform vfx = sceneRoot.Find("VFX");

            CreateSprite("OfferingSet_Full", "Assets/Art/Props/Offerings/21_offering_set_full.png", interactables, new Vector3(-0.25f, -2.1f, 0f), 20, 0.42f);
            CreateSprite("OfferingTray", "Assets/Art/Props/Offerings/28_offering_tray.png", interactables, new Vector3(-0.25f, -2.45f, 0f), 19, 0.42f);
            CreateSprite("Offering_Apple", "Assets/Art/Props/Offerings/22_apple_single.png", interactables, new Vector3(-1.4f, -1.95f, 0f), 22, 0.26f);
            CreateSprite("Offering_Pastry", "Assets/Art/Props/Offerings/23_pastry_single.png", interactables, new Vector3(-0.65f, -1.95f, 0f), 22, 0.26f);
            CreateSprite("Offering_WineCup", "Assets/Art/Props/Offerings/24_wine_cup_single.png", interactables, new Vector3(0.1f, -1.95f, 0f), 22, 0.26f);
            CreateSprite("Offering_IncenseBurner", "Assets/Art/Props/Offerings/25_incense_burner_single.png", interactables, new Vector3(0.9f, -1.95f, 0f), 22, 0.3f);
            CreateSprite("WhiteCandle_Unlit", "Assets/Art/Props/Offerings/26_white_candle_unlit.png", interactables, new Vector3(-2.1f, -1.85f, 0f), 22, 0.3f);
            CreateSprite("WhiteCandle_Lit_LanternOnly", "Assets/Art/Props/Offerings/27_white_candle_lit.png", hidden, new Vector3(-2.1f, -1.85f, 0f), 23, 0.3f);
            CreateSprite("BlackLantern_Unlit", "Assets/Art/Props/Lantern/17_black_lantern_unlit.png", interactables, new Vector3(2.0f, -1.95f, 0f), 23, 0.34f);
            CreateSprite("BlackLantern_Lit_LanternOnly", "Assets/Art/Props/Lantern/18_black_lantern_lit.png", hidden, new Vector3(2.0f, -1.95f, 0f), 24, 0.34f);
            CreateSprite("PaperEffigy_Altered_MourningHall", "Assets/Art/Characters/PaperEffigy/16_paper_effigy_altered.png", hidden, new Vector3(2.9f, -1.55f, 0f), 21, 0.35f);
            CreateSprite("HiddenText_MourningHall", ResolveHiddenText(3), hidden, new Vector3(-0.4f, -0.6f, 0f), 30, 0.45f);
            SetOverlaySprite(vfx);
        });
    }

    private static void ApplyOldWell()
    {
        ApplyToScene("Chapter1_OldWell", sceneRoot =>
        {
            Transform hidden = sceneRoot.Find("HiddenObjects");
            Transform interactables = sceneRoot.Find("Interactables");
            Transform vfx = sceneRoot.Find("VFX");

            CreateSprite("WellRopeAndBucket", "Assets/Art/Props/OldWell/29_well_rope_and_bucket.png", interactables, new Vector3(-1.2f, -2.1f, 0f), 20, 0.28f);
            CreateSprite("BlackLantern_Lit_OldWell", "Assets/Art/Props/Lantern/18_black_lantern_lit.png", interactables, new Vector3(2.2f, -2.05f, 0f), 23, 0.34f);
            CreateSprite("WaterReflection_OldWell", "Assets/Art/VFX/LanternVision/34_water_reflection_effect.png", hidden, new Vector3(-0.2f, -2.2f, 0f), 24, 0.42f);
            CreateSprite("NameInWellEffect", "Assets/Art/VFX/LanternVision/35_name_in_well_effect.png", hidden, new Vector3(0.15f, -1.3f, 0f), 30, 0.45f);
            CreateSprite("Grandmother_Afterimage_OldWell", "Assets/Art/Characters/Ghosts/13_grandmother_afterimage.png", hidden, new Vector3(2.7f, -1.35f, 0f), 25, 0.38f);
            SetOverlaySprite(vfx);
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
        CollectGenerated(sceneRoot.Find("HiddenObjects"), toDestroy);
        CollectGenerated(sceneRoot.Find("Interactables"), toDestroy);
        CollectGenerated(sceneRoot.Find("VFX"), toDestroy);

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
            if (child.name.StartsWith("Content_") || child.name == "Chapter1ContentAppliedMarker" || child.name == "Chapter1ContentScaleV2Marker")
            {
                toDestroy.Add(child.gameObject);
            }
        }
    }

    private static GameObject CreateSprite(string name, string spritePath, Transform parent, Vector3 position, int sortingOrder, float scale)
    {
        if (parent == null || string.IsNullOrEmpty(spritePath) || !File.Exists(spritePath))
        {
            return null;
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            return null;
        }

        GameObject item = new GameObject("Content_" + name);
        item.transform.SetParent(parent);
        item.transform.localPosition = position;
        item.transform.localScale = Vector3.one * scale;

        SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        return item;
    }

    private static void SetOverlaySprite(Transform vfx)
    {
        if (vfx == null)
        {
            return;
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/VFX/LanternVision/31_lantern_vision_filter.png");
        if (sprite == null)
        {
            return;
        }

        Transform overlay = vfx.Find("LanternVisionOverlay");
        if (overlay == null)
        {
            GameObject overlayObject = new GameObject("LanternVisionOverlay");
            overlayObject.transform.SetParent(vfx);
            overlayObject.transform.localPosition = Vector3.zero;
            overlay = overlayObject.transform;
        }

        SpriteRenderer renderer = overlay.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = overlay.gameObject.AddComponent<SpriteRenderer>();
        }

        renderer.sprite = sprite;
        renderer.sortingOrder = 100;
        renderer.color = Color.white;
        overlay.localScale = Vector3.one;
        overlay.gameObject.SetActive(false);
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

        GameObject marker = new GameObject("Chapter1ContentScaleV2Marker");
        marker.transform.SetParent(vfx);
        marker.transform.localPosition = Vector3.zero;
    }

    private static string ResolveHiddenText(int index)
    {
        string exact = "Assets/Art/VFX/LanternVision/32_hidden_text_" + index.ToString("00") + ".png";
        if (File.Exists(exact))
        {
            return exact;
        }

        string sheet = "Assets/Art/VFX/LanternVision/32_hidden_text_sheet_" + index.ToString("00") + ".png";
        return File.Exists(sheet) ? sheet : string.Empty;
    }

    private static string ResolveFootprints(int index)
    {
        string exact = "Assets/Art/VFX/LanternVision/33_footprints_guide_" + index.ToString("00") + ".png";
        if (File.Exists(exact))
        {
            return exact;
        }

        string sheet = "Assets/Art/VFX/LanternVision/33_footprints_guide_sheet_" + index.ToString("00") + ".png";
        return File.Exists(sheet) ? sheet : string.Empty;
    }
}
