using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class LinZhaoyingPlayerSetup
{
    // Generates the playable Lin Zhaoying player assets from the sprite sheet.
    private const string PreferredSheetPath = "Assets/Art/Characters/LinZhaoying/Lin_Zhaoying_SpriteSheet.png";
    private const string TransparentSheetPath = "Assets/Art/Characters/LinZhaoying/LinZhaoying_sprite_sheet_transparent.png";
    private const string ExistingSheetPath = "Assets/Art/Characters/LinZhaoying/Lin_Zhaoying_sprite_sheet_transparent_clean.png";
    private const string AnimationsDir = "Assets/Art/Characters/LinZhaoying/Animations";
    private const string ControllerPath = AnimationsDir + "/LinZhaoying_Player.controller";
    private const string PrefabPath = "Assets/Prefabs/Player/Player_LinZhaoying.prefab";

    private static readonly FrameGroup[] FrameGroups =
    {
        new FrameGroup("Idle", 0, 3, 6f, true),
        new FrameGroup("Walk", 1, 7, 10f, true),
        new FrameGroup("Interact", 2, 6, 12f, false),
        new FrameGroup("RaiseLantern", 3, 8, 12f, false)
    };

    [InitializeOnLoadMethod]
    private static void AutoSetupAfterImport()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null &&
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                return;
            }

            string sheetPath = ResolveSheetPath();
            if (!string.IsNullOrEmpty(sheetPath))
            {
                SetupPlayerAssets();
            }
        };
    }

    [MenuItem("JianDeng/Setup Lin Zhaoying Player")]
    public static void SetupPlayerAssets()
    {
        EnsureFolders();

        string sheetPath = ResolveSheetPath();
        if (string.IsNullOrEmpty(sheetPath))
        {
            Debug.LogWarning("Lin Zhaoying sprite sheet was not found. Expected " + PreferredSheetPath + ", " + TransparentSheetPath + ", or " + ExistingSheetPath);
            return;
        }

        if (!TrySliceSpriteSheet(sheetPath))
        {
            Debug.LogWarning("Could not automatically slice Lin Zhaoying sprite sheet. Open Sprite Editor and slice manually using 8 columns x 4 rows, then rerun JianDeng/Setup Lin Zhaoying Player.");
            return;
        }

        AssetDatabase.Refresh();

        Dictionary<string, Sprite> sprites = LoadSprites(sheetPath);
        if (!HasRequiredSprites(sprites))
        {
            Debug.LogWarning("Sprite sheet import did not expose all named frames. Open Sprite Editor, confirm frame names, then rerun setup.");
            return;
        }

        AnimationClip idle = CreateClip("Idle", sprites, "Idle", 3, 6f, true);
        AnimationClip walk = CreateClip("Walk", sprites, "Walk", 7, 10f, true);
        AnimationClip interact = CreateClip("Interact", sprites, "Interact", 6, 12f, false);
        AnimationClip raiseLantern = CreateClip("RaiseLantern", sprites, "RaiseLantern", 8, 12f, false);

        AnimatorController controller = CreateAnimatorController(idle, walk, interact, raiseLantern);
        CreatePlayerPrefab(controller, sprites["Idle_01"]);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Lin Zhaoying player setup complete: " + PrefabPath);
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(AnimationsDir);
        Directory.CreateDirectory("Assets/Prefabs/Player");
        Directory.CreateDirectory("Assets/Scripts/Player");
        Directory.CreateDirectory("Assets/Scripts/Interaction");
        Directory.CreateDirectory("Assets/Scripts/LanternVision");
    }

    private static string ResolveSheetPath()
    {
        if (File.Exists(PreferredSheetPath))
        {
            return PreferredSheetPath;
        }

        if (File.Exists(TransparentSheetPath))
        {
            return TransparentSheetPath;
        }

        return File.Exists(ExistingSheetPath) ? ExistingSheetPath : string.Empty;
    }

    private static bool TrySliceSpriteSheet(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return false;
        }

        Texture2D texture = LoadPngTexture(path);
        if (texture == null)
        {
            return false;
        }

        if (!HasUsableTransparency(texture))
        {
            Debug.LogWarning("Lin Zhaoying sprite sheet appears to have no transparent background. Please replace it with a transparent PNG before creating the player prefab.");
            return false;
        }

        List<Rect> rowRects = DetectOpaqueBandsByRows(texture, FrameGroups.Length);
        if (rowRects.Count != FrameGroups.Length)
        {
            Debug.LogWarning("Automatic row detection found " + rowRects.Count + " rows, expected " + FrameGroups.Length + ". Open Sprite Editor and slice manually by character outline.");
            return false;
        }

        List<SpriteMetaData> metas = new List<SpriteMetaData>();

        for (int groupIndex = 0; groupIndex < FrameGroups.Length; groupIndex++)
        {
            FrameGroup group = FrameGroups[groupIndex];
            Rect rowRect = rowRects[groupIndex];
            List<Rect> frameRects = DetectOpaqueBandsByColumns(texture, rowRect, group.Count);
            if (frameRects.Count != group.Count)
            {
                Debug.LogWarning("Automatic column detection for " + group.Name + " found " + frameRects.Count + " frames, expected " + group.Count + ". Open Sprite Editor and slice manually by character outline.");
                return false;
            }

            for (int index = 0; index < frameRects.Count; index++)
            {
                Rect rect = ExpandRect(frameRects[index], texture.width, texture.height, 8);
                SpriteMetaData meta = new SpriteMetaData
                {
                    name = group.Name + "_" + (index + 1).ToString("00"),
                    alignment = (int)SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0f),
                    rect = rect
                };
                metas.Add(meta);
            }
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 100f;

        foreach (string target in new[] { "DefaultTexturePlatform", "Standalone", "WebGL", "Android", "Windows Store Apps" })
        {
            TextureImporterPlatformSettings platform = importer.GetPlatformTextureSettings(target);
            platform.overridden = false;
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(platform);
        }

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);

        importer.spritesheet = metas.ToArray();
        importer.SaveAndReimport();
        return true;
    }

    private static Texture2D LoadPngTexture(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        return texture.LoadImage(File.ReadAllBytes(path)) ? texture : null;
    }

    private static bool HasUsableTransparency(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        int transparentPixels = 0;
        int opaqueBlackPixels = 0;

        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 pixel = pixels[i];
            if (pixel.a < 16)
            {
                transparentPixels++;
            }
            else if (pixel.a > 240 && pixel.r < 8 && pixel.g < 8 && pixel.b < 8)
            {
                opaqueBlackPixels++;
            }
        }

        if (transparentPixels == 0)
        {
            return false;
        }

        float opaqueBlackRatio = opaqueBlackPixels / (float)pixels.Length;
        return opaqueBlackRatio < 0.15f;
    }

    private static List<Rect> DetectOpaqueBandsByRows(Texture2D texture, int expectedCount)
    {
        int[] rowCounts = new int[texture.height];
        Color32[] pixels = texture.GetPixels32();
        for (int y = 0; y < texture.height; y++)
        {
            int rowOffset = y * texture.width;
            for (int x = 0; x < texture.width; x++)
            {
                if (pixels[rowOffset + x].a > 16)
                {
                    rowCounts[y]++;
                }
            }
        }

        List<Vector2Int> bands = FindBands(rowCounts, 5, 8);
        bands.Sort((a, b) => b.x.CompareTo(a.x));

        List<Rect> rows = new List<Rect>();
        foreach (Vector2Int band in bands)
        {
            rows.Add(new Rect(0f, band.x, texture.width, band.y - band.x + 1));
        }

        rows.Sort((a, b) => b.y.CompareTo(a.y));
        return rows.Count == expectedCount ? rows : new List<Rect>();
    }

    private static List<Rect> DetectOpaqueBandsByColumns(Texture2D texture, Rect rowRect, int expectedCount)
    {
        int startY = Mathf.Clamp(Mathf.FloorToInt(rowRect.yMin), 0, texture.height - 1);
        int endY = Mathf.Clamp(Mathf.CeilToInt(rowRect.yMax), 0, texture.height);
        int[] columnCounts = new int[texture.width];
        Color32[] pixels = texture.GetPixels32();

        for (int y = startY; y < endY; y++)
        {
            int rowOffset = y * texture.width;
            for (int x = 0; x < texture.width; x++)
            {
                if (pixels[rowOffset + x].a > 16)
                {
                    columnCounts[x]++;
                }
            }
        }

        List<Vector2Int> bands = FindBands(columnCounts, 5, 8);
        if (bands.Count != expectedCount)
        {
            return new List<Rect>();
        }

        List<Rect> rects = new List<Rect>();
        foreach (Vector2Int band in bands)
        {
            rects.Add(FindOpaqueBounds(texture, band.x, band.y, startY, endY));
        }

        rects.Sort((a, b) => a.x.CompareTo(b.x));
        return rects;
    }

    private static List<Vector2Int> FindBands(int[] counts, int threshold, int minimumSize)
    {
        List<Vector2Int> bands = new List<Vector2Int>();
        int start = -1;

        for (int i = 0; i < counts.Length; i++)
        {
            if (counts[i] > threshold)
            {
                if (start < 0)
                {
                    start = i;
                }
            }
            else if (start >= 0)
            {
                if (i - start >= minimumSize)
                {
                    bands.Add(new Vector2Int(start, i - 1));
                }

                start = -1;
            }
        }

        if (start >= 0 && counts.Length - start >= minimumSize)
        {
            bands.Add(new Vector2Int(start, counts.Length - 1));
        }

        return bands;
    }

    private static Rect FindOpaqueBounds(Texture2D texture, int startX, int endX, int startY, int endY)
    {
        Color32[] pixels = texture.GetPixels32();
        int minX = texture.width;
        int minY = texture.height;
        int maxX = 0;
        int maxY = 0;

        for (int y = startY; y < endY; y++)
        {
            int rowOffset = y * texture.width;
            for (int x = startX; x <= endX; x++)
            {
                if (pixels[rowOffset + x].a <= 16)
                {
                    continue;
                }

                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }

        return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private static Rect ExpandRect(Rect rect, int textureWidth, int textureHeight, int padding)
    {
        float xMin = Mathf.Max(0f, rect.xMin - padding);
        float yMin = Mathf.Max(0f, rect.yMin - padding);
        float xMax = Mathf.Min(textureWidth, rect.xMax + padding);
        float yMax = Mathf.Min(textureHeight, rect.yMax + padding);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static Dictionary<string, Sprite> LoadSprites(string sheetPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(sheetPath)
            .OfType<Sprite>()
            .ToDictionary(sprite => sprite.name, sprite => sprite);
    }

    private static bool HasRequiredSprites(Dictionary<string, Sprite> sprites)
    {
        foreach (FrameGroup group in FrameGroups)
        {
            for (int index = 1; index <= group.Count; index++)
            {
                if (!sprites.ContainsKey(group.Name + "_" + index.ToString("00")))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static AnimationClip CreateClip(string clipName, Dictionary<string, Sprite> sprites, string prefix, int count, float frameRate, bool loop)
    {
        AnimationClip clip = new AnimationClip
        {
            frameRate = frameRate
        };

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] frames = new ObjectReferenceKeyframe[count];
        for (int i = 0; i < count; i++)
        {
            frames[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = sprites[prefix + "_" + (i + 1).ToString("00")]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string clipPath = AnimationsDir + "/LinZhaoying_" + clipName + ".anim";
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(clip, existing);
            return existing;
        }

        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static AnimatorController CreateAnimatorController(AnimationClip idle, AnimationClip walk, AnimationClip interact, AnimationClip raiseLantern)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        controller.parameters = new AnimatorControllerParameter[0];
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Interact", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsLanternVision", AnimatorControllerParameterType.Bool);
        controller.AddParameter("RaiseLantern", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState state in stateMachine.states)
        {
            stateMachine.RemoveState(state.state);
        }
        foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions)
        {
            stateMachine.RemoveAnyStateTransition(transition);
        }

        AnimatorState idleState = stateMachine.AddState("Idle", new Vector3(260f, 80f, 0f));
        idleState.motion = idle;
        AnimatorState walkState = stateMachine.AddState("Walk", new Vector3(520f, 80f, 0f));
        walkState.motion = walk;
        AnimatorState interactState = stateMachine.AddState("Interact", new Vector3(260f, 240f, 0f));
        interactState.motion = interact;
        AnimatorState raiseLanternState = stateMachine.AddState("RaiseLantern", new Vector3(520f, 240f, 0f));
        raiseLanternState.motion = raiseLantern;
        stateMachine.defaultState = idleState;

        AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0.05f;
        idleToWalk.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");

        AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0.05f;
        walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");

        AnimatorStateTransition anyToInteract = stateMachine.AddAnyStateTransition(interactState);
        anyToInteract.hasExitTime = false;
        anyToInteract.duration = 0.03f;
        anyToInteract.AddCondition(AnimatorConditionMode.If, 0f, "Interact");

        AnimatorStateTransition interactToIdle = interactState.AddTransition(idleState);
        interactToIdle.hasExitTime = true;
        interactToIdle.exitTime = 0.95f;
        interactToIdle.duration = 0.05f;

        AnimatorStateTransition anyToRaise = stateMachine.AddAnyStateTransition(raiseLanternState);
        anyToRaise.hasExitTime = false;
        anyToRaise.duration = 0.03f;
        anyToRaise.AddCondition(AnimatorConditionMode.If, 0f, "RaiseLantern");

        AnimatorStateTransition raiseToIdle = raiseLanternState.AddTransition(idleState);
        raiseToIdle.hasExitTime = true;
        raiseToIdle.exitTime = 0.95f;
        raiseToIdle.duration = 0.05f;

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void CreatePlayerPrefab(AnimatorController controller, Sprite idleSprite)
    {
        GameObject root = new GameObject("Player_LinZhaoying");
        SpriteRenderer spriteRenderer = root.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = idleSprite;
        spriteRenderer.sortingOrder = 20;

        Animator animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.gravityScale = 2.5f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.freezeRotation = true;

        CapsuleCollider2D collider = root.AddComponent<CapsuleCollider2D>();
        collider.size = new Vector2(0.55f, 1.65f);
        collider.offset = new Vector2(0f, 0.82f);

        root.AddComponent<PlayerController>();
        root.AddComponent<InteractionDetector>();

        GameObject range = new GameObject("InteractionRange");
        range.transform.SetParent(root.transform, false);
        CircleCollider2D trigger = range.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 1.15f;
        trigger.offset = new Vector2(0f, 0.55f);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
    }

    private readonly struct FrameGroup
    {
        public readonly string Name;
        public readonly int Row;
        public readonly int Count;
        public readonly float FrameRate;
        public readonly bool Loop;

        public FrameGroup(string name, int row, int count, float frameRate, bool loop)
        {
            Name = name;
            Row = row;
            Count = count;
            FrameRate = frameRate;
            Loop = loop;
        }
    }
}
