using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace ChapterGame.EditorTools
{
    [InitializeOnLoad]
    public static class PlayerAnimatorAssetBuilder
    {
        private const string CharacterFolder = "Assets/素材和策划案/角色动作";
        private const string BuilderPath = "Assets/Editor/PlayerAnimatorAssetBuilder.cs";
        private const string OutputFolder = "Assets/Animations/Player";
        private const string ControllerPath = OutputFolder + "/PlayerAnimator.controller";
        private const float IdleHeight = 430f;
        private const float DisplayHeight = 360f;

        private static readonly SheetSpec[] Sheets =
        {
            new SheetSpec("待机", CharacterFolder + "/待机1.png", 5, 1, 1, 4f, 0),
            new SheetSpec("行走", CharacterFolder + "/行走1.png", 8, 1, 8, 10f, 1),
            new SheetSpec("举灯", CharacterFolder + "/提灯.jpg", 8, 1, 8, 4f, 2),
        };

        static PlayerAnimatorAssetBuilder()
        {
            EditorApplication.delayCall += BuildIfMissing;
        }

        [MenuItem("Tools/Chapter Game/Build Player Animator Assets")]
        public static void Build()
        {
            Directory.CreateDirectory(OutputFolder);

            var clips = new List<AnimationClip>();
            var spritesBySheet = new Dictionary<SheetSpec, Sprite[]>();
            foreach (var sheet in Sheets)
            {
                var sprites = SliceSheet(sheet);
                spritesBySheet[sheet] = sprites;
            }

            var displayWidth = DisplayHeight * GetWalkAspect(spritesBySheet);
            foreach (var sheet in Sheets)
            {
                var clip = BuildClip(sheet, spritesBySheet[sheet], displayWidth);
                clips.Add(clip);
            }

            BuildController(clips);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Player animation clips and animator controller are ready: " + ControllerPath);
        }

        private static void BuildIfMissing()
        {
            if (!File.Exists(ControllerPath) || Sheets.Any(NeedsRebuild))
            {
                Build();
            }
        }

        private static bool NeedsRebuild(SheetSpec sheet)
        {
            var clipPath = GetClipPath(sheet);
            if (!File.Exists(clipPath))
            {
                return true;
            }

            if (!File.Exists(sheet.Path))
            {
                return true;
            }

            var sourceTime = File.GetLastWriteTimeUtc(sheet.Path);
            var builderTime = File.Exists(BuilderPath) ? File.GetLastWriteTimeUtc(BuilderPath) : sourceTime;
            return sourceTime > File.GetLastWriteTimeUtc(clipPath)
                || sourceTime > File.GetLastWriteTimeUtc(ControllerPath)
                || builderTime > File.GetLastWriteTimeUtc(clipPath)
                || builderTime > File.GetLastWriteTimeUtc(ControllerPath);
        }

        private static Sprite[] SliceSheet(SheetSpec sheet)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(sheet.Path);
            if (texture == null)
            {
                Debug.LogError("Missing player animation sheet: " + sheet.Path);
                return new Sprite[0];
            }

            var importer = AssetImporter.GetAtPath(sheet.Path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("Missing TextureImporter: " + sheet.Path);
                return new Sprite[0];
            }

            var rects = BuildTrimmedRects(sheet, texture);
            var spriteMeta = new List<SpriteMetaData>();
            for (var i = 0; i < rects.Count; i++)
            {
                spriteMeta.Add(new SpriteMetaData
                {
                    name = sheet.Name + "_" + i.ToString("00"),
                    rect = rects[i],
                    alignment = (int)SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0f),
                });
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.spritePixelsPerUnit = 100f;
            importer.spritesheet = spriteMeta.ToArray();
            importer.SaveAndReimport();

            return AssetDatabase.LoadAllAssetsAtPath(sheet.Path)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name)
                .Where(sprite => sprite.name.StartsWith(sheet.Name + "_"))
                .ToArray();
        }

        private static List<Rect> BuildTrimmedRects(SheetSpec sheet, Texture2D texture)
        {
            var bytes = File.ReadAllBytes(sheet.Path);
            var readable = new Texture2D(2, 2);
            readable.LoadImage(bytes);

            var cellWidth = readable.width / sheet.Columns;
            var cellHeight = readable.height / sheet.Rows;
            var sharedTrim = GetSharedTransparentTrim(readable, sheet, cellWidth, cellHeight);
            var rects = new List<Rect>();
            for (var i = 0; i < sheet.FrameCount; i++)
            {
                var column = i % sheet.Columns;
                var rowFromTop = i / sheet.Columns;
                var x = column * cellWidth;
                var y = readable.height - (rowFromTop + 1) * cellHeight;
                rects.Add(new Rect(x + sharedTrim.x, y + sharedTrim.y, sharedTrim.width, sharedTrim.height));
            }

            Object.DestroyImmediate(readable);
            return rects;
        }

        private static RectInt GetSharedTransparentTrim(Texture2D texture, SheetSpec sheet, int cellWidth, int cellHeight)
        {
            var minX = cellWidth;
            var minY = cellHeight;
            var maxX = -1;
            var maxY = -1;

            for (var i = 0; i < sheet.FrameCount; i++)
            {
                var column = i % sheet.Columns;
                var rowFromTop = i / sheet.Columns;
                var startX = column * cellWidth;
                var startY = texture.height - (rowFromTop + 1) * cellHeight;
                for (var y = 0; y < cellHeight; y++)
                {
                    for (var x = 0; x < cellWidth; x++)
                    {
                        if (texture.GetPixel(startX + x, startY + y).a <= 0.03f)
                        {
                            continue;
                        }

                        minX = Mathf.Min(minX, x);
                        minY = Mathf.Min(minY, y);
                        maxX = Mathf.Max(maxX, x);
                        maxY = Mathf.Max(maxY, y);
                    }
                }
            }

            if (maxX < minX || maxY < minY)
            {
                return new RectInt(0, 0, cellWidth, cellHeight);
            }

            const int padding = 2;
            minX = Mathf.Max(0, minX - padding);
            minY = Mathf.Max(0, minY - padding);
            maxX = Mathf.Min(cellWidth - 1, maxX + padding);
            maxY = Mathf.Min(cellHeight - 1, maxY + padding);
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static Rect TrimTransparentPixels(Texture2D texture, RectInt source)
        {
            var minX = source.width;
            var minY = source.height;
            var maxX = -1;
            var maxY = -1;

            for (var y = 0; y < source.height; y++)
            {
                for (var x = 0; x < source.width; x++)
                {
                    if (texture.GetPixel(source.x + x, source.y + y).a <= 0.03f)
                    {
                        continue;
                    }

                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            if (maxX < minX || maxY < minY)
            {
                return new Rect(source.x, source.y, source.width, source.height);
            }

            const int padding = 2;
            minX = Mathf.Max(0, minX - padding);
            minY = Mathf.Max(0, minY - padding);
            maxX = Mathf.Min(source.width - 1, maxX + padding);
            maxY = Mathf.Min(source.height - 1, maxY + padding);

            return new Rect(source.x + minX, source.y + minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static AnimationClip BuildClip(SheetSpec sheet, Sprite[] sprites, float displayWidth)
        {
            var clipPath = GetClipPath(sheet);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, clipPath);
            }

            clip.name = "Player_" + sheet.Name;
            clip.frameRate = sheet.Fps;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var keys = new ObjectReferenceKeyframe[sprites.Length];
            for (var i = 0; i < sprites.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = i / sheet.Fps,
                    value = sprites[i],
                };
            }

            var imageBinding = EditorCurveBinding.PPtrCurve("", typeof(Image), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, imageBinding, keys);

            SetConstantCurve(clip, typeof(RectTransform), "m_SizeDelta.x", displayWidth);
            SetConstantCurve(clip, typeof(RectTransform), "m_SizeDelta.y", DisplayHeight);

            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void SetConstantCurve(AnimationClip clip, System.Type type, string property, float value)
        {
            var curve = AnimationCurve.Constant(0f, 1f / Mathf.Max(1f, clip.frameRate), value);
            clip.SetCurve("", type, property, curve);
        }

        private static float GetAverageAspect(Sprite[] sprites)
        {
            if (sprites == null || sprites.Length == 0)
            {
                return 1f;
            }

            var total = 0f;
            foreach (var sprite in sprites)
            {
                total += sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            }

            return total / sprites.Length;
        }

        private static float GetWalkAspect(IReadOnlyDictionary<SheetSpec, Sprite[]> spritesBySheet)
        {
            foreach (var pair in spritesBySheet)
            {
                if (pair.Key.StateValue == 1)
                {
                    return GetAverageAspect(pair.Value);
                }
            }

            return 1f;
        }

        private static void BuildController(IReadOnlyList<AnimationClip> clips)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            controller.parameters = new AnimatorControllerParameter[0];
            controller.AddParameter("state", AnimatorControllerParameterType.Int);

            var layer = controller.layers[0];
            var stateMachine = layer.stateMachine;
            foreach (var child in stateMachine.states)
            {
                stateMachine.RemoveState(child.state);
            }

            for (var i = 0; i < Sheets.Length; i++)
            {
                var state = stateMachine.AddState(Sheets[i].Name);
                state.motion = clips[i];
                state.writeDefaultValues = true;
                if (Sheets[i].StateValue == 0)
                {
                    stateMachine.defaultState = state;
                }

                var transition = stateMachine.AddAnyStateTransition(state);
                transition.hasExitTime = false;
                transition.duration = 0f;
                transition.canTransitionToSelf = false;
                transition.AddCondition(AnimatorConditionMode.Equals, Sheets[i].StateValue, "state");
            }

            EditorUtility.SetDirty(controller);
        }

        private static string GetClipPath(SheetSpec sheet)
        {
            return OutputFolder + "/Player_" + sheet.Name + ".anim";
        }

        private sealed class SheetSpec
        {
            public SheetSpec(string name, string path, int columns, int rows, int frameCount, float fps, int stateValue)
            {
                Name = name;
                Path = path;
                Columns = columns;
                Rows = rows;
                FrameCount = frameCount;
                Fps = fps;
                StateValue = stateValue;
            }

            public string Name { get; }
            public string Path { get; }
            public int Columns { get; }
            public int Rows { get; }
            public int FrameCount { get; }
            public float Fps { get; }
            public int StateValue { get; }
        }
    }
}
