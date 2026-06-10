using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ChapterGame
{
    public sealed class ChapterGameBootstrap : MonoBehaviour
    {
        private const string Chapter1Folder = "Assets/第一章/";
        private const string Chapter2Folder = "Assets/素材和策划案/第二章/";
        private const string Chapter3Folder = "Assets/素材和策划案/第三章/";
        private const string Chapter4Folder = "Assets/素材和策划案/第四章/";
        private const string UiFolder = "Assets/素材和策划案/UI设计/";
        private const string MainMenuFolder = "Assets/素材和策划案/主菜单/";
        private const string CharacterFolder = "Assets/素材和策划案/角色动作/";
        private const string InteractionPromptPath = "Assets/素材和策划案/第一章/UI/48_e_interaction_prompt.png";
        private const string BgmPath = "Assets/BGM.mp3";
        private const float PlayerIdleWidth = 143.31f;
        private const float PlayerIdleHeight = 430f;
        private const float PlayerDisplayHeight = 360f;
        private const float Chapter2DoorLanternMinX = 0.36f;
        private const float Chapter2DoorLanternMaxX = 0.64f;
        private const string BoundaryBlocked = "__blocked";
        private const int SpawnDefault = 0;
        private const int SpawnLeft = -1;
        private const int SpawnRight = 1;

        private readonly Dictionary<string, ChapterDefinition> chapters = new Dictionary<string, ChapterDefinition>();
        private readonly Dictionary<string, SceneDefinition> scenes = new Dictionary<string, SceneDefinition>();
        private readonly HashSet<string> inventory = new HashSet<string>();
        private readonly HashSet<string> flags = new HashSet<string>();

        private Canvas canvas;
        private RectTransform canvasRect;
        private RectTransform sceneViewport;
        private RectTransform worldLayer;
        private Image background;
        private RectTransform vfxRoot;
        private RectTransform propRoot;
        private RectTransform characterRoot;
        private Image playerImage;
        private RectTransform hotspotRoot;
        private Text titleText;
        private Text objectiveText;
        private Text messageText;
        private Text inventoryText;
        private Text lightText;
        private Image boundaryPromptImage;
        private GameObject modal;
        private Text modalTitle;
        private Text modalBody;
        private Image modalImage;
        private RectTransform modalActionRoot;
        private GameObject dialoguePanel;
        private Text dialogueTitle;
        private Text dialogueBody;
        private Text dialogueHint;
        private Image dialoguePortrait;
        private GameObject chapterMenuPanel;
        private Image transitionOverlay;
        private Text transitionTitle;
        private readonly List<string> dialogueLines = new List<string>();
        private GameObject maskPuzzlePanel;
        private Text maskPuzzleHint;
        private readonly string[] maskPuzzleSlots = new string[3];
        private readonly RectTransform[] maskPuzzleSlotRects = new RectTransform[3];
        private readonly Image[] maskPuzzleSlotImages = new Image[3];
        private readonly Dictionary<string, GameObject> maskPuzzlePieces = new Dictionary<string, GameObject>();
        private GameObject genericPuzzlePanel;
        private Text genericPuzzleHint;
        private string[] genericPuzzleSlots = new string[0];
        private string[] genericPuzzleCorrect = new string[0];
        private RectTransform[] genericPuzzleSlotRects = new RectTransform[0];
        private Image[] genericPuzzleSlotImages = new Image[0];
        private readonly Dictionary<string, GameObject> genericPuzzlePieces = new Dictionary<string, GameObject>();
        private Action genericPuzzleComplete;
        private GameObject cuttingTablePanel;
        private Text cuttingTableHint;
        private Coroutine dialogueCoroutine;
        private int dialogueLineIndex;
        private string dialogueFullLine = "";
        private bool dialogueTyping;
        private bool lightView;
        private string currentChapterId;
        private string currentSceneId;
        private string selectedLamp;
        private string boundaryNavigationTargetSceneId;
        private string boundaryNavigationBlockedMessage;
        private int boundaryNavigationSpawnSide = SpawnDefault;
        private int boundaryNavigationSide = SpawnDefault;
        private HotspotDefinition currentHoverHotspot;
        private readonly List<string> offeringPuzzleInput = new List<string>();
        private readonly List<RuntimeVfx> activeVfx = new List<RuntimeVfx>();
        private readonly List<Sprite> playerIdleFrames = new List<Sprite>();
        private readonly List<Sprite> playerWalkFrames = new List<Sprite>();
        private readonly List<Sprite> playerLanternFrames = new List<Sprite>();
        private Vector2 playerDisplaySize = new Vector2(PlayerIdleWidth, PlayerIdleHeight);
        private List<Sprite> currentPlayerFrames;
        private float playerX = 0.28f;
        private float playerFrameTime;
        private int playerFrameIndex;
        private bool playerFacingRight = true;
        private string playerState = "";
        private float viewportWidth = 1920f;
        private float viewportHeight = 1080f;
        private float sceneWidth = 1920f;
        private float sceneHeight = 1080f;
        private float cameraX;
        private float targetCameraX;
        private float nextBoundaryBlockMessageTime;
        private bool inputLocked;
        private bool sceneReady = true;
        private int pendingSpawnSide = SpawnDefault;
        private Coroutine transitionCoroutine;
        private AudioSource bgmSource;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartGame()
        {
            if (FindObjectOfType<ChapterGameBootstrap>() != null)
            {
                return;
            }

            var host = new GameObject("Chapter Game Bootstrap");
            DontDestroyOnLoad(host);
            host.AddComponent<ChapterGameBootstrap>();
        }

        private void Awake()
        {
            BuildData();
            BuildUi();
            PlayBackgroundMusic();
            ShowChapterSelect();
        }

        private void PlayBackgroundMusic()
        {
            if (bgmSource != null && bgmSource.isPlaying)
            {
                return;
            }

            var clip = LoadAudioClip(BgmPath);
            if (clip == null)
            {
                Debug.LogWarning("Background music not found: " + BgmPath);
                return;
            }

            if (FindObjectOfType<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }

            bgmSource = gameObject.GetComponent<AudioSource>();
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
            }

            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = 0.55f;
            bgmSource.spatialBlend = 0f;
            bgmSource.Play();
        }

        private void Update()
        {
            UpdateSceneMetrics(false);

            if (inputLocked)
            {
                UpdateCamera(Time.deltaTime);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Q) && !string.IsNullOrEmpty(currentSceneId))
            {
                ToggleLightView();
            }

            if (Input.GetKeyDown(KeyCode.Escape) && modal.activeSelf)
            {
                modal.SetActive(false);
            }

            if (Input.GetKeyDown(KeyCode.Escape) && dialoguePanel.activeSelf)
            {
                HideDialogue();
            }

            if (Input.GetKeyDown(KeyCode.Escape) && maskPuzzlePanel != null && maskPuzzlePanel.activeSelf)
            {
                HideMaskPuzzle();
            }

            if (Input.GetKeyDown(KeyCode.Escape) && genericPuzzlePanel != null && genericPuzzlePanel.activeSelf)
            {
                HideGenericPuzzle();
            }

            if (Input.GetKeyDown(KeyCode.Escape) && cuttingTablePanel != null && cuttingTablePanel.activeSelf)
            {
                HideCuttingTableCloseup();
            }

            UpdateBoundaryNavigationPrompt();
            if (Input.GetKeyDown(KeyCode.E) && currentChapterId != "chapter2" && TryActivateBoundaryNavigation())
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.E) && currentChapterId != "chapter2" && !modal.activeSelf && !dialoguePanel.activeSelf && (maskPuzzlePanel == null || !maskPuzzlePanel.activeSelf) && (genericPuzzlePanel == null || !genericPuzzlePanel.activeSelf) && (cuttingTablePanel == null || !cuttingTablePanel.activeSelf) && currentHoverHotspot != null)
            {
                InvokeHotspot(currentHoverHotspot);
            }

            for (var i = 0; i < activeVfx.Count; i++)
            {
                activeVfx[i].Tick(Time.deltaTime);
            }

            UpdatePlayer(Time.deltaTime);
            UpdateBoundaryNavigationPrompt();
            UpdateCamera(Time.deltaTime);
        }

        private void BuildUi()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            if (Camera.main != null)
            {
                Camera.main.backgroundColor = Color.black;
            }

            canvas = new GameObject("Game Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasRect = canvas.GetComponent<RectTransform>();

            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            sceneViewport = new GameObject("Scene Viewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            sceneViewport.SetParent(canvas.transform, false);
            Stretch(sceneViewport);

            worldLayer = new GameObject("World Layer", typeof(RectTransform)).GetComponent<RectTransform>();
            worldLayer.SetParent(sceneViewport, false);
            worldLayer.anchorMin = new Vector2(0, 0);
            worldLayer.anchorMax = new Vector2(0, 0);
            worldLayer.pivot = new Vector2(0, 0);
            worldLayer.anchoredPosition = Vector2.zero;
            worldLayer.sizeDelta = new Vector2(sceneWidth, sceneHeight);

            background = CreateImage("Background", worldLayer, Color.black);
            background.raycastTarget = false;
            background.preserveAspect = true;
            var backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = new Vector2(0, 0);
            backgroundRect.anchorMax = new Vector2(0, 0);
            backgroundRect.pivot = new Vector2(0, 0);
            backgroundRect.anchoredPosition = Vector2.zero;
            backgroundRect.sizeDelta = new Vector2(sceneWidth, sceneHeight);

            vfxRoot = new GameObject("VFX", typeof(RectTransform)).GetComponent<RectTransform>();
            vfxRoot.SetParent(worldLayer, false);
            Stretch(vfxRoot);

            propRoot = new GameObject("Scene Props", typeof(RectTransform)).GetComponent<RectTransform>();
            propRoot.SetParent(worldLayer, false);
            Stretch(propRoot);

            characterRoot = new GameObject("Player", typeof(RectTransform)).GetComponent<RectTransform>();
            characterRoot.SetParent(worldLayer, false);
            Stretch(characterRoot);

            playerImage = CreateImage("林照萤", characterRoot, Color.white);
            playerImage.raycastTarget = false;
            playerImage.preserveAspect = true;
            var playerRect = playerImage.rectTransform;
            playerRect.anchorMin = new Vector2(0, 0);
            playerRect.anchorMax = new Vector2(0, 0);
            playerRect.pivot = new Vector2(0.5f, 0);
            playerRect.anchoredPosition = new Vector2(1920f * playerX, 96f);
            playerRect.sizeDelta = new Vector2(170f, 430f);
            characterRoot.gameObject.SetActive(false);

            hotspotRoot = new GameObject("Hotspots", typeof(RectTransform)).GetComponent<RectTransform>();
            hotspotRoot.SetParent(worldLayer, false);
            Stretch(hotspotRoot);

            var top = CreatePanel("Top Bar", canvas.transform, new Color(0f, 0f, 0f, 0.92f));
            Anchor(top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -92), Vector2.zero);
            DecoratePanel(top.GetComponent<Image>(), null, new Color(0f, 0f, 0f, 0.92f));

            titleText = CreateText("Title", top, 32, TextAnchor.MiddleLeft);
            Anchor(titleText.rectTransform, new Vector2(0, 0), new Vector2(0.35f, 1), new Vector2(34, 0), new Vector2(-8, 0));

            objectiveText = CreateText("Objective", top, 24, TextAnchor.MiddleCenter);
            Anchor(objectiveText.rectTransform, new Vector2(0.34f, 0), new Vector2(0.78f, 1), new Vector2(12, 0), new Vector2(-12, 0));

            lightText = CreateText("Light Hint", top, 20, TextAnchor.MiddleRight);
            Anchor(lightText.rectTransform, new Vector2(0.78f, 0), new Vector2(1, 1), new Vector2(8, 0), new Vector2(-34, 0));

            var bottom = CreatePanel("Bottom Bar", canvas.transform, new Color(0f, 0f, 0f, 0.92f));
            Anchor(bottom, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 92));
            DecoratePanel(bottom.GetComponent<Image>(), null, new Color(0f, 0f, 0f, 0.92f));

            messageText = CreateText("Message", bottom, 24, TextAnchor.MiddleLeft);
            Anchor(messageText.rectTransform, new Vector2(0, 0), new Vector2(0.74f, 1), new Vector2(34, 10), new Vector2(-12, -10));

            inventoryText = CreateText("Inventory", bottom, 21, TextAnchor.MiddleRight);
            Anchor(inventoryText.rectTransform, new Vector2(0.74f, 0), new Vector2(1, 1), new Vector2(10, 10), new Vector2(-34, -10));

            boundaryPromptImage = CreateImage("Boundary E Prompt", canvas.transform, Color.white);
            boundaryPromptImage.sprite = LoadSprite(InteractionPromptPath);
            boundaryPromptImage.preserveAspect = true;
            boundaryPromptImage.raycastTarget = false;
            boundaryPromptImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            boundaryPromptImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            boundaryPromptImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            boundaryPromptImage.rectTransform.sizeDelta = new Vector2(130f, 60f);
            boundaryPromptImage.gameObject.SetActive(false);

            modal = CreatePanel("Modal", canvas.transform, new Color(0.02f, 0.018f, 0.015f, 0.94f)).gameObject;
            DecoratePanel(modal.GetComponent<Image>(), UiFolder + "对话框.png", new Color(0.07f, 0.035f, 0.02f, 0.96f));
            var modalRect = modal.GetComponent<RectTransform>();
            Anchor(modalRect, new Vector2(0.18f, 0.16f), new Vector2(0.82f, 0.84f), Vector2.zero, Vector2.zero);

            modalTitle = CreateText("Modal Title", modal.transform, 34, TextAnchor.MiddleLeft);
            Anchor(modalTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -76), new Vector2(-30, -18));

            modalImage = CreateImage("Modal Image", modal.transform, new Color(0, 0, 0, 0.25f));
            Anchor(modalImage.rectTransform, new Vector2(0.04f, 0.18f), new Vector2(0.42f, 0.82f), Vector2.zero, Vector2.zero);
            modalImage.preserveAspect = true;

            modalBody = CreateText("Modal Body", modal.transform, 28, TextAnchor.UpperLeft);
            Anchor(modalBody.rectTransform, new Vector2(0.46f, 0.18f), new Vector2(0.96f, 0.82f), Vector2.zero, Vector2.zero);

            modalActionRoot = new GameObject("Modal Actions", typeof(RectTransform)).GetComponent<RectTransform>();
            modalActionRoot.SetParent(modal.transform, false);
            Anchor(modalActionRoot, new Vector2(0.46f, 0.04f), new Vector2(0.96f, 0.17f), Vector2.zero, Vector2.zero);

            var close = CreateButton("Close Button", modal.transform, "关闭", 24, delegate
            {
                ClearModalActions();
                modal.SetActive(false);
            });
            Anchor(close.GetComponent<RectTransform>(), new Vector2(0.72f, 0.86f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);
            modal.SetActive(false);

            dialoguePanel = CreatePanel("Dialogue Panel", canvas.transform, new Color(0.035f, 0.025f, 0.018f, 0.92f)).gameObject;
            DecoratePanel(dialoguePanel.GetComponent<Image>(), UiFolder + "对话框.png", new Color(0.07f, 0.035f, 0.02f, 0.94f));
            var dialogueRect = dialoguePanel.GetComponent<RectTransform>();
            Anchor(dialogueRect, new Vector2(0.10f, 0.07f), new Vector2(0.90f, 0.32f), Vector2.zero, Vector2.zero);
            var dialogueClick = dialoguePanel.AddComponent<Button>();
            dialogueClick.targetGraphic = dialoguePanel.GetComponent<Image>();
            dialogueClick.transition = Selectable.Transition.None;
            dialogueClick.onClick.AddListener(AdvanceDialogue);

            dialoguePortrait = CreateImage("Dialogue Portrait", dialoguePanel.transform, new Color(0, 0, 0, 0.12f));
            Anchor(dialoguePortrait.rectTransform, new Vector2(0.035f, 0.08f), new Vector2(0.20f, 0.92f), Vector2.zero, Vector2.zero);
            dialoguePortrait.preserveAspect = true;
            dialoguePortrait.raycastTarget = false;

            dialogueTitle = CreateText("Dialogue Title", dialoguePanel.transform, 28, TextAnchor.MiddleLeft);
            Anchor(dialogueTitle.rectTransform, new Vector2(0.23f, 0.72f), new Vector2(0.72f, 0.94f), Vector2.zero, Vector2.zero);
            dialogueTitle.raycastTarget = false;

            dialogueBody = CreateText("Dialogue Body", dialoguePanel.transform, 30, TextAnchor.UpperLeft);
            Anchor(dialogueBody.rectTransform, new Vector2(0.23f, 0.20f), new Vector2(0.95f, 0.72f), Vector2.zero, Vector2.zero);
            dialogueBody.raycastTarget = false;

            dialogueHint = CreateText("Dialogue Hint", dialoguePanel.transform, 20, TextAnchor.MiddleRight);
            Anchor(dialogueHint.rectTransform, new Vector2(0.68f, 0.04f), new Vector2(0.95f, 0.17f), Vector2.zero, Vector2.zero);
            dialogueHint.text = "点击继续";
            dialogueHint.raycastTarget = false;
            dialoguePanel.SetActive(false);

            transitionOverlay = CreateImage("Scene Transition Overlay", canvas.transform, Color.black);
            Stretch(transitionOverlay.rectTransform);
            transitionOverlay.raycastTarget = true;
            transitionOverlay.color = new Color(0f, 0f, 0f, 0f);

            transitionTitle = CreateText("Transition Title", transitionOverlay.transform, 48, TextAnchor.MiddleCenter);
            Anchor(transitionTitle.rectTransform, new Vector2(0.24f, 0.40f), new Vector2(0.76f, 0.60f), Vector2.zero, Vector2.zero);
            transitionTitle.text = "";
            transitionTitle.color = new Color(0.94f, 0.86f, 0.68f, 0f);
            transitionTitle.raycastTarget = false;
            transitionOverlay.gameObject.SetActive(false);

            LoadPlayerFrames();
        }

        private void ShowChapterSelect()
        {
            if (inputLocked)
            {
                return;
            }

            currentChapterId = null;
            currentSceneId = null;
            SetPlayerVisible(false);
            lightView = false;
            ClearHotspots();
            ClearChapterMenu();
            background.sprite = null;
            background.sprite = LoadSprite(MainMenuFolder + "MainMenu.png");
            background.color = background.sprite == null ? new Color(0.05f, 0.04f, 0.035f, 1) : Color.white;
            UpdateSceneMetrics(true);
            titleText.text = "织灯：归渡";
            objectiveText.text = "章节选择";
            lightText.text = "";
            messageText.text = "选择章节开始。悬停发光区域查看交互提示，点击或按 E 调查。";
            inventoryText.text = "背包：空";

            ClearVfx();
            ClearProps();
            CreateChapterMenu();
        }

        private void StartChapter(string chapterId)
        {
            if (inputLocked)
            {
                return;
            }

            ClearChapterMenu();
            inventory.Clear();
            flags.Clear();
            lightView = false;
            currentChapterId = chapterId;
            EnsureChapter4EndingItem();
            var chapter = chapters[chapterId];
            messageText.text = chapter.StartMessage;
            GoToScene(chapter.StartScene);
        }

        private void CreateChapterMenu()
        {
            chapterMenuPanel = CreatePanel("Chapter Menu", canvas.transform, new Color(0.02f, 0.016f, 0.012f, 0.68f)).gameObject;
            Anchor(chapterMenuPanel.GetComponent<RectTransform>(), new Vector2(0.60f, 0.20f), new Vector2(0.90f, 0.58f), Vector2.zero, Vector2.zero);
            DecoratePanel(chapterMenuPanel.GetComponent<Image>(), null, new Color(0.025f, 0.020f, 0.015f, 0.72f));

            var header = CreateText("Chapter Menu Header", chapterMenuPanel.transform, 28, TextAnchor.MiddleCenter);
            Anchor(header.rectTransform, new Vector2(0.08f, 0.80f), new Vector2(0.92f, 0.96f), Vector2.zero, Vector2.zero);
            header.text = "章节";

            CreateChapterButton("第一章", "归镇", 0, delegate { StartChapter("chapter1"); });
            CreateChapterButton("第二章", "纸马铺", 1, delegate { StartChapter("chapter2"); });
            CreateChapterButton("第三章", "无声戏台", 2, delegate { StartChapter("chapter3"); });
            CreateChapterButton("第四章", "无声灯楼", 3, delegate { StartChapter("chapter4"); });
        }

        private void CreateChapterButton(string chapter, string subtitle, int index, UnityEngine.Events.UnityAction action)
        {
            var top = 0.76f - index * 0.17f;
            var button = CreateButton("Chapter " + (index + 1), chapterMenuPanel.transform, chapter + "  " + subtitle, 28, action);
            Anchor(button.GetComponent<RectTransform>(), new Vector2(0.10f, top - 0.12f), new Vector2(0.90f, top), Vector2.zero, Vector2.zero);
        }

        private void ClearChapterMenu()
        {
            if (chapterMenuPanel == null)
            {
                return;
            }

            Destroy(chapterMenuPanel);
            chapterMenuPanel = null;
        }

        private void GoToScene(string sceneId)
        {
            GoToScene(sceneId, SpawnDefault);
        }

        private void GoToScene(string sceneId, int spawnSide)
        {
            if (inputLocked || transitionCoroutine != null || string.IsNullOrEmpty(sceneId) || !scenes.ContainsKey(sceneId))
            {
                return;
            }

            pendingSpawnSide = spawnSide;
            transitionCoroutine = StartCoroutine(TransitionToScene(sceneId));
        }

        private void ApplyScene(string sceneId)
        {
            currentSceneId = sceneId;
            var scene = scenes[sceneId];
            titleText.text = chapters[currentChapterId].Title + " / " + scene.Title;
            SetObjective(scene.Objective);
            lightView = false;
            selectedLamp = null;
            ApplyBackground(scene);
            ApplyPendingSpawnSide();
            RebuildProps();
            RebuildVfx();
            RefreshHud();
            RebuildHotspots();
            ShowPlayerInScene();
        }

        private IEnumerator TransitionToScene(string sceneId)
        {
            inputLocked = true;
            sceneReady = false;
            currentHoverHotspot = null;
            ClearBoundaryNavigationPrompt();
            HideDialogue();
            HideCuttingTableCloseup();
            ClearModalActions();
            if (modal != null)
            {
                modal.SetActive(false);
            }

            SetTransitionVisible(true);
            yield return FadeTransition(0f, 1f, 0.62f, false);

            var scene = scenes[sceneId];
            if (transitionTitle != null)
            {
                transitionTitle.text = chapters[currentChapterId].Title + "\n" + scene.Title;
                transitionTitle.color = new Color(0.94f, 0.86f, 0.68f, 1f);
            }

            ApplyScene(sceneId);
            UpdateSceneMetrics(true);
            yield return null;
            yield return new WaitUntil(() => sceneReady);
            yield return new WaitForSeconds(0.18f);

            yield return FadeTransition(1f, 0f, 0.62f, true);
            SetTransitionVisible(false);
            sceneReady = true;
            inputLocked = false;
            transitionCoroutine = null;
        }

        private void SetTransitionVisible(bool visible)
        {
            if (transitionOverlay == null)
            {
                return;
            }

            transitionOverlay.transform.SetAsLastSibling();
            transitionOverlay.gameObject.SetActive(visible);
            transitionOverlay.raycastTarget = visible;
            if (transitionTitle != null && !visible)
            {
                transitionTitle.text = "";
            }
        }

        private IEnumerator FadeTransition(float from, float to, float duration, bool hideTitle)
        {
            if (transitionOverlay == null)
            {
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                t = t * t * (3f - 2f * t);
                var alpha = Mathf.Lerp(from, to, t);
                transitionOverlay.color = new Color(0f, 0f, 0f, alpha);
                if (transitionTitle != null)
                {
                    var title = transitionTitle.color;
                    title.a = hideTitle ? alpha : Mathf.Clamp01(alpha * 1.15f);
                    transitionTitle.color = title;
                }
                yield return null;
            }

            transitionOverlay.color = new Color(0f, 0f, 0f, to);
            if (transitionTitle != null)
            {
                var title = transitionTitle.color;
                title.a = hideTitle ? to : Mathf.Clamp01(to);
                transitionTitle.color = title;
            }
        }

        private void ToggleLightView()
        {
            if (currentChapterId == "chapter1" && !inventory.Contains("黑灯（未点燃）") && !inventory.Contains("黑灯（已点燃）"))
            {
                Say("还没有灯。");
                return;
            }

            if (currentChapterId == "chapter1" && inventory.Contains("黑灯（未点燃）") && !inventory.Contains("黑灯（已点燃）"))
            {
                Say("黑灯还没有点燃。");
                return;
            }

            lightView = !lightView;
            var scene = scenes[currentSceneId];
            ApplyBackground(scene);
            RebuildProps();
            RebuildVfx();
            RefreshHud();
            RebuildHotspots();
            SetPlayerAnimation(lightView ? "lantern" : "idle", true);

            if (!string.IsNullOrEmpty(scene.LightMessage))
            {
                Say(lightView ? scene.LightMessage : "灯影退去，眼前又恢复了普通视角。");
            }
        }

        private void ApplyBackground(SceneDefinition scene)
        {
            var imageName = lightView && !string.IsNullOrEmpty(scene.LightImage) ? scene.LightImage : scene.Image;
            if (string.IsNullOrEmpty(imageName))
            {
                background.sprite = null;
                background.color = new Color(0.03f, 0.025f, 0.02f, 1);
                sceneReady = true;
                UpdateSceneMetrics(true);
                return;
            }

            background.sprite = LoadSprite(scene.Folder + imageName + ".png");
            background.color = background.sprite == null ? new Color(0.08f, 0.07f, 0.06f, 1) : Color.white;
            sceneReady = true;
            UpdateSceneMetrics(true);
        }

        private void UpdateSceneMetrics(bool snapCamera)
        {
            if (canvasRect == null || worldLayer == null || background == null)
            {
                return;
            }

            var rect = canvasRect.rect;
            var newViewportWidth = Mathf.Max(1f, rect.width);
            var newViewportHeight = Mathf.Max(1f, rect.height);
            if (!snapCamera && Mathf.Approximately(newViewportWidth, viewportWidth) && Mathf.Approximately(newViewportHeight, viewportHeight))
            {
                return;
            }

            viewportWidth = newViewportWidth;
            viewportHeight = newViewportHeight;
            sceneHeight = viewportHeight;
            sceneWidth = viewportWidth;

            if (background.sprite != null)
            {
                var spriteRect = background.sprite.rect;
                var ratio = spriteRect.width / Mathf.Max(1f, spriteRect.height);
                sceneWidth = Mathf.Max(viewportWidth, viewportHeight * ratio);
            }

            worldLayer.anchorMin = new Vector2(0, 0);
            worldLayer.anchorMax = new Vector2(0, 0);
            worldLayer.pivot = new Vector2(0, 0);
            worldLayer.sizeDelta = new Vector2(sceneWidth, sceneHeight);

            var backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = new Vector2(0, 0);
            backgroundRect.anchorMax = new Vector2(0, 0);
            backgroundRect.pivot = new Vector2(0, 0);
            backgroundRect.anchoredPosition = Vector2.zero;
            backgroundRect.sizeDelta = new Vector2(sceneWidth, sceneHeight);

            Stretch(vfxRoot);
            Stretch(propRoot);
            Stretch(characterRoot);
            Stretch(hotspotRoot);

            playerX = Mathf.Clamp(playerX, GetPlayerEdgePadding(), 1f - GetPlayerEdgePadding());
            PositionPlayer();
            UpdateCameraTarget();
            if (snapCamera)
            {
                cameraX = targetCameraX;
                ApplyCamera();
            }
        }

        private void UpdateCamera(float deltaTime)
        {
            if (worldLayer == null)
            {
                return;
            }

            UpdateCameraTarget();
            var maxCamera = Mathf.Max(0f, sceneWidth - viewportWidth);
            cameraX = Mathf.Clamp(Mathf.Lerp(cameraX, targetCameraX, 1f - Mathf.Exp(-8f * deltaTime)), 0f, maxCamera);
            ApplyCamera();
        }

        private void UpdateCameraTarget()
        {
            var maxCamera = Mathf.Max(0f, sceneWidth - viewportWidth);
            var playerWorldX = sceneWidth * playerX;
            targetCameraX = Mathf.Clamp(playerWorldX - viewportWidth * 0.5f, 0f, maxCamera);
        }

        private void ApplyCamera()
        {
            if (worldLayer == null)
            {
                return;
            }

            worldLayer.anchoredPosition = new Vector2(-Mathf.Round(cameraX), 0f);
        }

        private float GetPlayerEdgePadding()
        {
            return Mathf.Clamp(140f / Mathf.Max(1f, sceneWidth), 0.02f, 0.08f);
        }

        private void ApplyPendingSpawnSide()
        {
            if (pendingSpawnSide == SpawnLeft)
            {
                playerX = GetPlayerEdgePadding();
            }
            else if (pendingSpawnSide == SpawnRight)
            {
                playerX = 1f - GetPlayerEdgePadding();
            }

            pendingSpawnSide = SpawnDefault;
        }

        private void RebuildHotspots()
        {
            ClearHotspots();
            if (string.IsNullOrEmpty(currentSceneId))
            {
                return;
            }

            var scene = scenes[currentSceneId];
            foreach (var hotspot in scene.Hotspots)
            {
                if (hotspot.LightOnly && !lightView)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(hotspot.RequiredFlag) && !flags.Contains(hotspot.RequiredFlag))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(hotspot.HiddenAfterFlag) && flags.Contains(hotspot.HiddenAfterFlag))
                {
                    continue;
                }

                CreateHotspot(hotspot);
            }
        }

        private void ClearHotspots()
        {
            currentHoverHotspot = null;
            for (var i = hotspotRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(hotspotRoot.GetChild(i).gameObject);
            }
        }

        private void RebuildVfx()
        {
            ClearVfx();
            if (string.IsNullOrEmpty(currentSceneId))
            {
                return;
            }

            var scene = scenes[currentSceneId];
            foreach (var effect in scene.Effects)
            {
                if (effect.LightOnly && !lightView)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(effect.RequiredFlag) && !flags.Contains(effect.RequiredFlag))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(effect.HiddenAfterFlag) && flags.Contains(effect.HiddenAfterFlag))
                {
                    continue;
                }

                CreateVfx(effect);
            }
        }

        private void RebuildProps()
        {
            ClearProps();
            if (string.IsNullOrEmpty(currentSceneId))
            {
                return;
            }

            var scene = scenes[currentSceneId];
            foreach (var prop in scene.Props)
            {
                if (prop.LightOnly && !lightView)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(prop.RequiredFlag) && !flags.Contains(prop.RequiredFlag))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(prop.HiddenAfterFlag) && flags.Contains(prop.HiddenAfterFlag))
                {
                    continue;
                }

                CreateProp(prop);
            }
        }

        private void ClearProps()
        {
            if (propRoot == null)
            {
                return;
            }

            for (var i = propRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(propRoot.GetChild(i).gameObject);
            }
        }

        private void CreateProp(PropDefinition prop)
        {
            var image = CreateImage(prop.Name, propRoot, new Color(1, 1, 1, prop.Alpha));
            image.raycastTarget = false;
            image.preserveAspect = prop.PreserveAspect;
            Anchor(image.rectTransform, new Vector2(prop.Area.x, prop.Area.y), new Vector2(prop.Area.width, prop.Area.height), Vector2.zero, Vector2.zero);
            image.rectTransform.localEulerAngles = new Vector3(0f, 0f, prop.RotationZ);
            image.rectTransform.localScale = new Vector3(prop.ScaleX, prop.ScaleY, 1f);
            image.sprite = LoadSprite(prop.Path);
            if (image.sprite == null)
            {
                Destroy(image.gameObject);
            }
        }

        private void ClearVfx()
        {
            activeVfx.Clear();
            if (vfxRoot == null)
            {
                return;
            }

            for (var i = vfxRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(vfxRoot.GetChild(i).gameObject);
            }
        }

        private void CreateVfx(VfxDefinition effect)
        {
            var image = CreateImage(effect.Name, vfxRoot, new Color(1, 1, 1, effect.Alpha));
            image.raycastTarget = false;
            image.preserveAspect = effect.PreserveAspect;
            Anchor(image.rectTransform, new Vector2(effect.Area.x, effect.Area.y), new Vector2(effect.Area.width, effect.Area.height), Vector2.zero, Vector2.zero);

            var frames = LoadSprites(effect.Path);
            if (frames.Count == 0)
            {
                Destroy(image.gameObject);
                return;
            }

            activeVfx.Add(new RuntimeVfx(image, frames, effect.Fps));
        }

        private void CreateHotspot(HotspotDefinition hotspot)
        {
            var button = CreateButton(hotspot.Label, hotspotRoot, "", 1, delegate { InvokeHotspot(hotspot); }, false);
            var rect = button.GetComponent<RectTransform>();
            Anchor(rect, new Vector2(hotspot.Area.x, hotspot.Area.y), new Vector2(hotspot.Area.width, hotspot.Area.height), Vector2.zero, Vector2.zero);
            var image = button.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.01f);
            image.raycastTarget = true;

            var effect = button.gameObject.AddComponent<RuntimeHotspotEffect>();
            effect.Initialize(hotspot.Label, GetHotspotRingSize(hotspot), IsNavigationHotspot(hotspot), delegate
            {
                currentHoverHotspot = hotspot;
            },
            delegate
            {
                if (currentHoverHotspot == hotspot)
                {
                    currentHoverHotspot = null;
                }
            });
        }

        private void InvokeHotspot(HotspotDefinition hotspot)
        {
            if (inputLocked || hotspot == null || hotspot.Action == null)
            {
                return;
            }

            hotspot.Action();
        }

        private void Say(string text)
        {
            messageText.text = text;
        }

        private void SetObjective(string text)
        {
            if (objectiveText == null)
            {
                return;
            }

            objectiveText.text = NormalizeObjective(text);
        }

        private static string NormalizeObjective(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            var value = text.Replace("当前目标：", "").Trim();
            var split = value.IndexOf('。');
            if (split >= 0 && split + 1 < value.Length)
            {
                value = value.Substring(split + 1).Trim();
            }

            return "目标：" + value.TrimEnd('。');
        }

        private void Take(string item, string flag, string message)
        {
            inventory.Add(item);
            if (!string.IsNullOrEmpty(flag))
            {
                flags.Add(flag);
            }

            Say(message);
            RefreshHud();
            RebuildProps();
            RebuildHotspots();
        }

        private bool NeedItem(string item, string missingMessage)
        {
            if (inventory.Contains(item))
            {
                return true;
            }

            Say(missingMessage);
            return false;
        }

        private bool NeedFlag(string flag, string missingMessage)
        {
            if (flags.Contains(flag))
            {
                return true;
            }

            Say(missingMessage);
            return false;
        }

        private void SetFlag(string flag, string message)
        {
            flags.Add(flag);
            Say(message);
            RefreshHud();
            RebuildProps();
            RebuildHotspots();
        }

        private void Inspect(string title, string body, string imageName)
        {
            Dialogue(title, body, imageName);
        }

        private void Dialogue(string title, string body, string portraitPath)
        {
            ClearModalActions();
            modal.SetActive(false);
            dialogueLines.Clear();
            var lines = body.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    dialogueLines.Add(line);
                }
            }

            if (dialogueLines.Count == 0)
            {
                dialogueLines.Add(body);
            }

            dialogueTitle.text = title;
            dialoguePortrait.sprite = string.IsNullOrEmpty(portraitPath) ? null : LoadSprite(portraitPath);
            dialoguePortrait.color = dialoguePortrait.sprite == null ? new Color(0, 0, 0, 0.12f) : Color.white;
            dialoguePortrait.gameObject.SetActive(dialoguePortrait.sprite != null);
            var textMinX = dialoguePortrait.sprite == null ? 0.06f : 0.23f;
            Anchor(dialogueTitle.rectTransform, new Vector2(textMinX, 0.72f), new Vector2(0.72f, 0.94f), Vector2.zero, Vector2.zero);
            Anchor(dialogueBody.rectTransform, new Vector2(textMinX, 0.20f), new Vector2(0.95f, 0.72f), Vector2.zero, Vector2.zero);
            dialogueLineIndex = 0;
            dialoguePanel.SetActive(true);
            ShowDialogueLine();
        }

        private static bool LooksLikeDialogue(string body)
        {
            if (string.IsNullOrEmpty(body))
            {
                return false;
            }

            var dialogueLineCount = 0;
            var lines = body.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                var split = line.IndexOf('：');
                if (split <= 0 || split > 6)
                {
                    continue;
                }

                var speaker = line.Substring(0, split);
                if (speaker == "普通视角" || speaker == "灯影视角" || speaker == "当前顺序")
                {
                    continue;
                }

                dialogueLineCount++;
            }

            return dialogueLineCount >= 2;
        }

        private void AdvanceDialogue()
        {
            if (!dialoguePanel.activeSelf)
            {
                return;
            }

            if (dialogueTyping)
            {
                if (dialogueCoroutine != null)
                {
                    StopCoroutine(dialogueCoroutine);
                    dialogueCoroutine = null;
                }

                dialogueTyping = false;
                dialogueBody.text = dialogueFullLine;
                return;
            }

            dialogueLineIndex++;
            if (dialogueLineIndex >= dialogueLines.Count)
            {
                HideDialogue();
                return;
            }

            ShowDialogueLine();
        }

        private void ShowDialogueLine()
        {
            if (dialogueCoroutine != null)
            {
                StopCoroutine(dialogueCoroutine);
            }

            dialogueFullLine = dialogueLines[dialogueLineIndex];
            dialogueCoroutine = StartCoroutine(TypeDialogueLine(dialogueFullLine));
        }

        private IEnumerator TypeDialogueLine(string line)
        {
            dialogueTyping = true;
            dialogueBody.text = "";
            for (var i = 0; i < line.Length; i++)
            {
                dialogueBody.text = line.Substring(0, i + 1);
                yield return new WaitForSeconds(0.03f);
            }

            dialogueTyping = false;
            dialogueCoroutine = null;
        }

        private void HideDialogue()
        {
            if (dialogueCoroutine != null)
            {
                StopCoroutine(dialogueCoroutine);
                dialogueCoroutine = null;
            }

            dialogueTyping = false;
            dialoguePanel.SetActive(false);
        }

        private void ShowMaskPuzzle()
        {
            if (flags.Contains("c3_stage_key"))
            {
                Say("面具机关已经打开，戏台机关钥已取得。");
                return;
            }

            if (!NeedItem("傩面喜", "还缺傩面喜。") || !NeedItem("傩面哀", "还缺傩面哀。") || !NeedItem("傩面无名", "还缺傩面无名。") || !NeedItem("戏文残页", "需要戏文残页上的顺序。"))
            {
                return;
            }

            HideDialogue();
            modal.SetActive(false);
            HideMaskPuzzle();
            HideCuttingTableCloseup();

            Array.Clear(maskPuzzleSlots, 0, maskPuzzleSlots.Length);
            Array.Clear(maskPuzzleSlotRects, 0, maskPuzzleSlotRects.Length);
            Array.Clear(maskPuzzleSlotImages, 0, maskPuzzleSlotImages.Length);
            maskPuzzlePieces.Clear();

            maskPuzzlePanel = CreatePanel("Mask Puzzle", canvas.transform, new Color(0f, 0f, 0f, 0.70f)).gameObject;
            Stretch(maskPuzzlePanel.GetComponent<RectTransform>());

            var board = CreatePanel("Mask Puzzle Board", maskPuzzlePanel.transform, new Color(0.04f, 0.028f, 0.018f, 0.94f));
            DecoratePanel(board.GetComponent<Image>(), UiFolder + "对话框.png", new Color(0.07f, 0.035f, 0.02f, 0.94f));
            Anchor(board, new Vector2(0.16f, 0.16f), new Vector2(0.84f, 0.84f), Vector2.zero, Vector2.zero);

            var scenePreview = CreateImage("Mask Puzzle Scene", board, new Color(1f, 1f, 1f, 0.24f));
            Anchor(scenePreview.rectTransform, new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.88f), Vector2.zero, Vector2.zero);
            scenePreview.sprite = LoadSprite(Chapter3Folder + "后台区域灯影下的场景.png");
            scenePreview.preserveAspect = false;
            scenePreview.raycastTarget = false;

            var title = CreateText("Mask Puzzle Title", board, 30, TextAnchor.MiddleLeft);
            Anchor(title.rectTransform, new Vector2(0.06f, 0.88f), new Vector2(0.62f, 0.98f), Vector2.zero, Vector2.zero);
            title.text = "傩面机关";

            maskPuzzleHint = CreateText("Mask Puzzle Hint", board, 22, TextAnchor.MiddleLeft);
            Anchor(maskPuzzleHint.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.66f, 0.18f), Vector2.zero, Vector2.zero);
            maskPuzzleHint.text = "拖动背包里的面具到三个挂点。戏文残页提示：喜面入门，哀面开路，无名守灯。";

            var close = CreateButton("Mask Puzzle Close", board, "关闭", 22, HideMaskPuzzle);
            Anchor(close.GetComponent<RectTransform>(), new Vector2(0.82f, 0.89f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);

            CreateMaskPuzzleSlot(board, 0, new Vector2(0.38f, 0.45f), new Vector2(0.50f, 0.70f), "挂点一");
            CreateMaskPuzzleSlot(board, 1, new Vector2(0.49f, 0.45f), new Vector2(0.61f, 0.70f), "挂点二");
            CreateMaskPuzzleSlot(board, 2, new Vector2(0.60f, 0.45f), new Vector2(0.72f, 0.70f), "挂点三");

            CreateMaskPuzzlePiece(board, "傩面喜", Chapter3Folder + "傩面喜.png", new Vector2(0.18f, 0.20f), new Vector2(0.30f, 0.42f));
            CreateMaskPuzzlePiece(board, "傩面哀", Chapter3Folder + "傩面哀.png", new Vector2(0.44f, 0.20f), new Vector2(0.56f, 0.42f));
            CreateMaskPuzzlePiece(board, "傩面无名", Chapter3Folder + "傩面无名.png", new Vector2(0.70f, 0.20f), new Vector2(0.82f, 0.42f));
        }

        private void CreateMaskPuzzleSlot(RectTransform parent, int index, Vector2 min, Vector2 max, string label)
        {
            var slot = CreateImage("Mask Slot " + index, parent, new Color(0.18f, 0.12f, 0.06f, 0.62f));
            Anchor(slot.rectTransform, min, max, Vector2.zero, Vector2.zero);
            slot.preserveAspect = true;
            slot.raycastTarget = false;
            var outline = slot.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.72f, 0.32f, 0.76f);
            outline.effectDistance = new Vector2(2f, -2f);
            maskPuzzleSlotRects[index] = slot.rectTransform;
            maskPuzzleSlotImages[index] = slot;

            var text = CreateText(label, slot.transform, 18, TextAnchor.LowerCenter);
            Stretch(text.rectTransform);
            text.text = label;
            text.raycastTarget = false;
        }

        private void CreateMaskPuzzlePiece(RectTransform parent, string maskName, string spritePath, Vector2 min, Vector2 max)
        {
            var piece = CreateImage(maskName, parent, Color.white);
            Anchor(piece.rectTransform, min, max, Vector2.zero, Vector2.zero);
            piece.sprite = LoadSprite(spritePath);
            piece.preserveAspect = true;
            piece.raycastTarget = true;
            var group = piece.gameObject.AddComponent<CanvasGroup>();
            var drag = piece.gameObject.AddComponent<MaskPuzzleDragItem>();
            drag.Initialize(this, maskName, parent, group);
            maskPuzzlePieces[maskName] = piece.gameObject;
        }

        private bool TryDropMask(string maskName, Vector2 screenPosition)
        {
            for (var i = 0; i < maskPuzzleSlotRects.Length; i++)
            {
                var slotRect = maskPuzzleSlotRects[i];
                if (slotRect == null || !RectTransformUtility.RectangleContainsScreenPoint(slotRect, screenPosition, null))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(maskPuzzleSlots[i]))
                {
                    maskPuzzleHint.text = "这个挂点已经有面具了，换一个位置试试。";
                    return false;
                }

                PlaceMask(maskName, i);
                return true;
            }

            maskPuzzleHint.text = "把面具拖到墙上的三个挂点上。";
            return false;
        }

        private void PlaceMask(string maskName, int slotIndex)
        {
            maskPuzzleSlots[slotIndex] = maskName;
            maskPuzzleSlotImages[slotIndex].sprite = LoadSprite(Chapter3Folder + maskName + ".png");
            maskPuzzleSlotImages[slotIndex].color = Color.white;
            if (maskPuzzlePieces.ContainsKey(maskName))
            {
                maskPuzzlePieces[maskName].SetActive(false);
            }

            if (string.IsNullOrEmpty(maskPuzzleSlots[0]) || string.IsNullOrEmpty(maskPuzzleSlots[1]) || string.IsNullOrEmpty(maskPuzzleSlots[2]))
            {
                maskPuzzleHint.text = "继续放置剩下的面具。";
                return;
            }

            if (maskPuzzleSlots[0] == "傩面喜" && maskPuzzleSlots[1] == "傩面哀" && maskPuzzleSlots[2] == "傩面无名")
            {
                CompleteMaskPuzzle();
                return;
            }

            ResetMaskPuzzle("顺序不对。戏文残页提示：喜面入门，哀面开路，无名守灯。");
        }

        private void ResetMaskPuzzle(string hint)
        {
            for (var i = 0; i < maskPuzzleSlots.Length; i++)
            {
                maskPuzzleSlots[i] = null;
                if (maskPuzzleSlotImages[i] != null)
                {
                    maskPuzzleSlotImages[i].sprite = null;
                    maskPuzzleSlotImages[i].color = new Color(0.18f, 0.12f, 0.06f, 0.62f);
                }
            }

            foreach (var piece in maskPuzzlePieces.Values)
            {
                piece.SetActive(true);
                var drag = piece.GetComponent<MaskPuzzleDragItem>();
                if (drag != null)
                {
                    drag.ReturnHome();
                }
            }

            maskPuzzleHint.text = hint;
        }

        private void CompleteMaskPuzzle()
        {
            inventory.Add("戏台机关钥");
            flags.Add("c3_stage_key");
            HideMaskPuzzle();
            Say("你按“喜、哀、无名”的顺序挂回傩面，机关吐出戏台机关钥。");
            SetObjective("回舞台，敲响破锣");
            RefreshHud();
            RebuildProps();
            RebuildHotspots();
        }

        private void HideMaskPuzzle()
        {
            if (maskPuzzlePanel == null)
            {
                return;
            }

            Destroy(maskPuzzlePanel);
            maskPuzzlePanel = null;
            maskPuzzlePieces.Clear();
        }

        private void ShowGenericPuzzle(string title, string previewPath, string hint, string[] slotLabels, PuzzlePiece[] pieces, string[] correctOrder, Action onComplete, bool showSlotLabels = true, bool useTwoByTwoSlots = false)
        {
            HideDialogue();
            modal.SetActive(false);
            HideMaskPuzzle();
            HideGenericPuzzle();
            HideCuttingTableCloseup();

            genericPuzzleSlots = new string[slotLabels.Length];
            genericPuzzleCorrect = correctOrder;
            genericPuzzleSlotRects = new RectTransform[slotLabels.Length];
            genericPuzzleSlotImages = new Image[slotLabels.Length];
            genericPuzzlePieces.Clear();
            genericPuzzleComplete = onComplete;
            useTwoByTwoSlots = useTwoByTwoSlots || (slotLabels.Length == 4 && pieces.Length == 4);
            var displayPieces = pieces;
            if (useTwoByTwoSlots && pieces.Length == 4)
            {
                displayPieces = new[] { pieces[2], pieces[0], pieces[3], pieces[1] };
            }

            genericPuzzlePanel = CreatePanel("Generic Puzzle", canvas.transform, new Color(0f, 0f, 0f, 0.70f)).gameObject;
            Stretch(genericPuzzlePanel.GetComponent<RectTransform>());

            var board = CreatePanel("Generic Puzzle Board", genericPuzzlePanel.transform, new Color(0.04f, 0.028f, 0.018f, 0.94f));
            DecoratePanel(board.GetComponent<Image>(), UiFolder + "对话框.png", new Color(0.07f, 0.035f, 0.02f, 0.94f));
            Anchor(board, new Vector2(0.15f, 0.14f), new Vector2(0.85f, 0.86f), Vector2.zero, Vector2.zero);

            if (!string.IsNullOrEmpty(previewPath))
            {
                var scenePreview = CreateImage("Puzzle Preview", board, new Color(1f, 1f, 1f, 0.22f));
                Anchor(scenePreview.rectTransform, new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.84f), Vector2.zero, Vector2.zero);
                scenePreview.sprite = LoadSprite(previewPath);
                scenePreview.preserveAspect = false;
                scenePreview.raycastTarget = false;
            }

            var titleText = CreateText("Puzzle Title", board, 30, TextAnchor.MiddleLeft);
            Anchor(titleText.rectTransform, new Vector2(0.06f, 0.88f), new Vector2(0.62f, 0.98f), Vector2.zero, Vector2.zero);
            titleText.text = title;

            genericPuzzleHint = CreateText("Puzzle Hint", board, 21, TextAnchor.MiddleLeft);
            Anchor(genericPuzzleHint.rectTransform, new Vector2(0.06f, 0.07f), new Vector2(0.72f, 0.18f), Vector2.zero, Vector2.zero);
            genericPuzzleHint.text = hint;

            var close = CreateButton("Puzzle Close", board, "关闭", 22, HideGenericPuzzle);
            Anchor(close.GetComponent<RectTransform>(), new Vector2(0.82f, 0.89f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);

            if (useTwoByTwoSlots && slotLabels.Length == 4)
            {
                var slotMin = new[]
                {
                    new Vector2(0.40f, 0.60f),
                    new Vector2(0.52f, 0.60f),
                    new Vector2(0.40f, 0.39f),
                    new Vector2(0.52f, 0.39f)
                };
                var slotMax = new[]
                {
                    new Vector2(0.50f, 0.78f),
                    new Vector2(0.62f, 0.78f),
                    new Vector2(0.50f, 0.57f),
                    new Vector2(0.62f, 0.57f)
                };

                for (var i = 0; i < slotLabels.Length; i++)
                {
                    CreateGenericPuzzleSlot(board, i, slotMin[i], slotMax[i], slotLabels[i], showSlotLabels);
                }
            }
            else
            {
                var slotWidth = Mathf.Min(0.13f, 0.70f / Mathf.Max(1, slotLabels.Length));
                var slotGap = Mathf.Min(0.025f, 0.12f / Mathf.Max(1, slotLabels.Length));
                var slotTotal = slotLabels.Length * slotWidth + (slotLabels.Length - 1) * slotGap;
                var slotStart = 0.5f - slotTotal * 0.5f;
                for (var i = 0; i < slotLabels.Length; i++)
                {
                    var x = slotStart + i * (slotWidth + slotGap);
                    CreateGenericPuzzleSlot(board, i, new Vector2(x, 0.48f), new Vector2(x + slotWidth, 0.72f), slotLabels[i], showSlotLabels);
                }
            }

            var pieceWidth = Mathf.Min(0.12f, 0.78f / Mathf.Max(1, displayPieces.Length));
            var pieceGap = Mathf.Min(0.03f, 0.16f / Mathf.Max(1, displayPieces.Length));
            var pieceTotal = displayPieces.Length * pieceWidth + (displayPieces.Length - 1) * pieceGap;
            var pieceStart = 0.5f - pieceTotal * 0.5f;
            for (var i = 0; i < displayPieces.Length; i++)
            {
                var x = pieceStart + i * (pieceWidth + pieceGap);
                CreateGenericPuzzlePiece(board, displayPieces[i], new Vector2(x, 0.20f), new Vector2(x + pieceWidth, 0.42f));
            }
        }

        private void CreateGenericPuzzleSlot(RectTransform parent, int index, Vector2 min, Vector2 max, string label, bool showLabel)
        {
            var slot = CreateImage("Puzzle Slot " + index, parent, new Color(0.18f, 0.12f, 0.06f, 0.62f));
            Anchor(slot.rectTransform, min, max, Vector2.zero, Vector2.zero);
            slot.preserveAspect = true;
            slot.raycastTarget = false;
            var outline = slot.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.72f, 0.32f, 0.76f);
            outline.effectDistance = new Vector2(2f, -2f);
            genericPuzzleSlotRects[index] = slot.rectTransform;
            genericPuzzleSlotImages[index] = slot;

            if (showLabel)
            {
                var text = CreateText(label, slot.transform, 18, TextAnchor.LowerCenter);
                Stretch(text.rectTransform);
                text.text = label;
                text.raycastTarget = false;
            }
        }

        private void CreateGenericPuzzlePiece(RectTransform parent, PuzzlePiece pieceData, Vector2 min, Vector2 max)
        {
            var piece = CreateImage(pieceData.Name, parent, Color.white);
            Anchor(piece.rectTransform, min, max, Vector2.zero, Vector2.zero);
            piece.sprite = LoadSprite(pieceData.SpritePath);
            piece.preserveAspect = true;
            piece.raycastTarget = true;

            if (piece.sprite == null)
            {
                var label = CreateText(pieceData.Name + " Label", piece.transform, 22, TextAnchor.MiddleCenter);
                Stretch(label.rectTransform);
                label.text = pieceData.Name;
                label.raycastTarget = false;
                piece.color = new Color(0.20f, 0.13f, 0.08f, 0.90f);
            }

            var group = piece.gameObject.AddComponent<CanvasGroup>();
            var drag = piece.gameObject.AddComponent<GenericPuzzleDragItem>();
            drag.Initialize(this, pieceData.Name, group);
            genericPuzzlePieces[pieceData.Name] = piece.gameObject;
        }

        private bool TryDropGenericPuzzlePiece(string pieceName, Vector2 screenPosition)
        {
            for (var i = 0; i < genericPuzzleSlotRects.Length; i++)
            {
                var slotRect = genericPuzzleSlotRects[i];
                if (slotRect == null || !RectTransformUtility.RectangleContainsScreenPoint(slotRect, screenPosition, null))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(genericPuzzleSlots[i]))
                {
                    genericPuzzleHint.text = "这个位置已经放过东西了，换一个槽位试试。";
                    return false;
                }

                PlaceGenericPuzzlePiece(pieceName, i);
                return true;
            }

            genericPuzzleHint.text = "把物品拖到上方的空槽里。";
            return false;
        }

        private void PlaceGenericPuzzlePiece(string pieceName, int slotIndex)
        {
            genericPuzzleSlots[slotIndex] = pieceName;
            var piece = genericPuzzlePieces.ContainsKey(pieceName) ? genericPuzzlePieces[pieceName] : null;
            var pieceImage = piece == null ? null : piece.GetComponent<Image>();
            genericPuzzleSlotImages[slotIndex].sprite = pieceImage == null ? null : pieceImage.sprite;
            genericPuzzleSlotImages[slotIndex].color = Color.white;
            if (piece != null)
            {
                piece.SetActive(false);
            }

            for (var i = 0; i < genericPuzzleSlots.Length; i++)
            {
                if (string.IsNullOrEmpty(genericPuzzleSlots[i]))
                {
                    genericPuzzleHint.text = "继续放置剩下的物品。";
                    return;
                }
            }

            for (var i = 0; i < genericPuzzleCorrect.Length; i++)
            {
                if (genericPuzzleSlots[i] != genericPuzzleCorrect[i])
                {
                    ResetGenericPuzzle("顺序不对，所有物品退回。重新按照线索摆放。");
                    return;
                }
            }

            var complete = genericPuzzleComplete;
            HideGenericPuzzle();
            if (complete != null)
            {
                complete();
            }
        }

        private void ResetGenericPuzzle(string hint)
        {
            for (var i = 0; i < genericPuzzleSlots.Length; i++)
            {
                genericPuzzleSlots[i] = null;
                if (genericPuzzleSlotImages[i] != null)
                {
                    genericPuzzleSlotImages[i].sprite = null;
                    genericPuzzleSlotImages[i].color = new Color(0.18f, 0.12f, 0.06f, 0.62f);
                }
            }

            foreach (var piece in genericPuzzlePieces.Values)
            {
                piece.SetActive(true);
                var drag = piece.GetComponent<GenericPuzzleDragItem>();
                if (drag != null)
                {
                    drag.ReturnHome();
                }
            }

            genericPuzzleHint.text = hint;
        }

        private void HideGenericPuzzle()
        {
            if (genericPuzzlePanel == null)
            {
                return;
            }

            Destroy(genericPuzzlePanel);
            genericPuzzlePanel = null;
            genericPuzzlePieces.Clear();
            genericPuzzleComplete = null;
        }

        private void ShowCuttingTableCloseup()
        {
            HideDialogue();
            modal.SetActive(false);
            HideMaskPuzzle();
            HideGenericPuzzle();
            HideCuttingTableCloseup();

            SetPlayerVisible(false);

            cuttingTablePanel = CreatePanel("Cutting Table Closeup", canvas.transform, new Color(0f, 0f, 0f, 0.72f)).gameObject;
            Stretch(cuttingTablePanel.GetComponent<RectTransform>());

            var board = CreatePanel("Cutting Table Board", cuttingTablePanel.transform, new Color(0.03f, 0.022f, 0.016f, 0.96f));
            Anchor(board, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.90f), Vector2.zero, Vector2.zero);

            var table = CreateImage("Cutting Table Image", board, Color.white);
            Anchor(table.rectTransform, new Vector2(0.02f, 0.10f), new Vector2(0.98f, 0.92f), Vector2.zero, Vector2.zero);
            table.sprite = LoadSprite(Chapter2Folder + "调查剪纸台.png");
            table.preserveAspect = false;
            table.raycastTarget = false;

            var title = CreateText("Cutting Table Title", board, 28, TextAnchor.MiddleLeft);
            Anchor(title.rectTransform, new Vector2(0.04f, 0.91f), new Vector2(0.55f, 0.99f), Vector2.zero, Vector2.zero);
            title.text = "调查剪纸台";

            cuttingTableHint = CreateText("Cutting Table Hint", board, 20, TextAnchor.MiddleLeft);
            Anchor(cuttingTableHint.rectTransform, new Vector2(0.04f, 0.02f), new Vector2(0.70f, 0.09f), Vector2.zero, Vector2.zero);
            cuttingTableHint.text = "拿走浆糊刷和剪纸碎片四。";

            var close = CreateButton("Cutting Table Close", board, "关闭", 22, HideCuttingTableCloseup);
            Anchor(close.GetComponent<RectTransform>(), new Vector2(0.84f, 0.92f), new Vector2(0.96f, 0.985f), Vector2.zero, Vector2.zero);

            CreateCuttingTablePickup(board, "浆糊刷", "c2_brush", Chapter2Folder + "浆糊刷.png", new Rect(0.27f, 0.26f, 0.42f, 0.62f), "你拿到了浆糊刷。");
            CreateCuttingTablePickup(board, "剪纸碎片四", "c2_piece4", Chapter2Folder + "剪纸碎片四.png", new Rect(0.60f, 0.33f, 0.78f, 0.66f), "红纸堆里藏着剪纸碎片四。");
        }

        private void CreateCuttingTablePickup(RectTransform parent, string itemName, string flag, string spritePath, Rect area, string message)
        {
            if (flags.Contains(flag))
            {
                return;
            }

            var propImage = CreateImage(itemName + " Image", parent, Color.white);
            Anchor(propImage.rectTransform, new Vector2(area.x, area.y), new Vector2(area.width, area.height), Vector2.zero, Vector2.zero);
            propImage.sprite = LoadSprite(spritePath);
            propImage.preserveAspect = true;
            propImage.raycastTarget = false;

            var button = CreateButton(itemName + " Pickup", parent, "", 1, delegate { }, false);
            Anchor(button.GetComponent<RectTransform>(), new Vector2(area.x, area.y), new Vector2(area.width, area.height), Vector2.zero, Vector2.zero);
            button.onClick.AddListener(delegate
            {
                Take(itemName, flag, message);
                propImage.gameObject.SetActive(false);
                button.gameObject.SetActive(false);
                if (cuttingTableHint != null)
                {
                    cuttingTableHint.text = flags.Contains("c2_brush") && flags.Contains("c2_piece4")
                        ? "道具已经拿齐，可以关闭。"
                        : "继续寻找剩下的道具。";
                }
            });
        }

        private void HideCuttingTableCloseup()
        {
            if (cuttingTablePanel == null)
            {
                return;
            }

            Destroy(cuttingTablePanel);
            cuttingTablePanel = null;
            cuttingTableHint = null;
            ShowPlayerInScene();
        }

        private void ShowOfferingPuzzle()
        {
            ShowGenericPuzzle("供品顺序",
                Chapter1Folder + "摆满贡品的供桌.png",
                "拖动供品到五个位置。看供桌上的残留痕迹决定顺序。",
                new[] { "果", "糕", "酒", "香", "火" },
                new[]
                {
                    new PuzzlePiece("酒杯", Chapter1Folder + "酒杯.png"),
                    new PuzzlePiece("白蜡烛", Chapter1Folder + "白蜡烛.png"),
                    new PuzzlePiece("苹果", Chapter1Folder + "苹果.png"),
                    new PuzzlePiece("香炉", Chapter1Folder + "香炉.png"),
                    new PuzzlePiece("糕点", Chapter1Folder + "糕点.png")
                },
                new[] { "苹果", "糕点", "酒杯", "香炉", "白蜡烛" },
                CompleteOfferingPuzzle,
                false);
        }

        private void CreatePuzzleButton(string label, string token, int index)
        {
            var width = 1f / 5f;
            var button = CreateButton("Offering " + label, modalActionRoot, label, 21, delegate { ChooseOffering(token); });
            Anchor(button.GetComponent<RectTransform>(), new Vector2(index * width + 0.01f, 0.05f), new Vector2((index + 1) * width - 0.01f, 0.95f), Vector2.zero, Vector2.zero);
        }

        private void ChooseOffering(string token)
        {
            var correct = new[] { "果", "糕", "酒", "香", "火" };
            offeringPuzzleInput.Add(token);

            for (var i = 0; i < offeringPuzzleInput.Count; i++)
            {
                if (offeringPuzzleInput[i] == correct[i])
                {
                    continue;
                }

                offeringPuzzleInput.Clear();
                modalBody.text = "顺序不对，供桌上的烛火晃了一下。\n灯影提示仍是：果、糕、酒、香、火。";
                return;
            }

            modalBody.text = "当前顺序：" + string.Join("、", offeringPuzzleInput);
            if (offeringPuzzleInput.Count >= correct.Length)
            {
                CompleteOfferingPuzzle();
            }
        }

        private void CompleteOfferingPuzzle()
        {
            ClearModalActions();
            SetFlag("c1_offering_solved", "你按“果、糕、酒、香、火”的顺序摆好苹果、糕点、酒杯、香炉和白蜡烛。供桌微微一震。");
            inventory.Add("黑灯（已点燃）");
            inventory.Remove("黑灯（未点燃）");
            flags.Add("c1_lantern_lit");
            Inspect("黑灯（已点燃）", "黑灯点燃。\n外婆：灯点起来，才能看见被藏住的名字。\n林照萤：外婆……？\n外婆的声音：先回桥边，再去井边。\n黑灯已点燃，按 Q 切换灯影视角。", Chapter1Folder + "黑灯.png");
            SetObjective("返回石桥，查看写名河灯");
            RefreshHud();
            RebuildProps();
            RebuildVfx();
            RebuildHotspots();
        }

        private void ShowWindowPuzzle()
        {
            if (!NeedFlag("c2_piece1", "窗花还缺剪纸碎片一。") || !NeedFlag("c2_piece2", "窗花还缺剪纸碎片二。") || !NeedFlag("c2_piece3", "窗花还缺剪纸碎片三。") || !NeedFlag("c2_piece4", "窗花还缺剪纸碎片四。") || !NeedItem("浆糊刷", "还需要浆糊刷把窗花贴上。"))
            {
                return;
            }

            ShowGenericPuzzle("拼合窗花",
                Chapter2Folder + "调查窗户平面图.png",
                "拖动四片剪纸到窗格。线索：一在左上，二在右上，三在左下，四在右下。",
                new[] { "左上", "右上", "左下", "右下" },
                new[]
                {
                    new PuzzlePiece("剪纸碎片一", Chapter2Folder + "剪纸碎片一.png"),
                    new PuzzlePiece("剪纸碎片二", Chapter2Folder + "剪纸碎片二.png"),
                    new PuzzlePiece("剪纸碎片三", Chapter2Folder + "剪纸碎片三.png"),
                    new PuzzlePiece("剪纸碎片四", Chapter2Folder + "剪纸碎片四.png")
                },
                new[] { "剪纸碎片一", "剪纸碎片二", "剪纸碎片三", "剪纸碎片四" },
                CompleteWindowPuzzle);
        }

        private void CompleteWindowPuzzle()
        {
            SetFlag("c2_window_complete", "四片剪纸归位，窗花贴上旧窗。当前目标：按 Q 切换灯影视角，查看窗花投影。");
            Inspect("完整窗花", "四片剪纸归位：一在左上，二在右上，三在左下，四在右下。窗花贴上旧窗后，等待灯影照出隐藏文字。", Chapter2Folder + "调查窗户平面图.png");
            SetObjective("按 Q 切换灯影视角，查看窗花投影");
        }

        private void ShowFurnacePuzzle()
        {
            if (!lightView)
            {
                Say("火炉太烫，不能直接拾取。也许灯影视角能看清炉灰里的东西。");
                return;
            }

            if (!NeedItem("炉灰铲", "需要炉灰铲翻动炉灰。") || !NeedItem("火钳", "需要火钳夹出残页。"))
            {
                return;
            }

            ShowGenericPuzzle("取出残页",
                Chapter2Folder + "焚火炉区域灯影下场景.png",
                "拖动工具按步骤处理焚纸炉：先用炉灰铲翻开炉灰，再用火钳夹出残页。",
                new[] { "翻灰", "夹取" },
                new[]
                {
                    new PuzzlePiece("炉灰铲", Chapter2Folder + "炉灰铲.png"),
                    new PuzzlePiece("火钳", Chapter2Folder + "火钳.png")
                },
                new[] { "炉灰铲", "火钳" },
                CompleteFurnacePuzzle);
        }

        private void CompleteFurnacePuzzle()
        {
            Inspect("未烧尽的残页", "普通视角：\n“……祭名单……”\n“……封……”\n“……黑灯……”\n\n灯影视角：\n“缄灯祭名单其三。”\n“封名者，不入族谱。”\n“黑灯既燃，名与忆皆归灯中。”\n\n林照萤：封名……不是给死人用的。是为了让活着的人，也被忘掉。",
                Chapter2Folder + "灯影下未烧尽的残页.png");
            flags.Add("c2_page_read");
            RebuildProps();
            CompleteChapter("想知道名字去哪了，就去听那出无声戏。第二章结束，第三章《无声戏台》已解锁。");
        }

        private void ShowNamePuzzle()
        {
            if (!NeedItem("望字块", "族谱空位已有“陈”，还缺“望”。") || !NeedItem("月字块", "族谱空位已有“陈望”，还缺“月”。"))
            {
                return;
            }

            ShowGenericPuzzle("补全族谱",
                Chapter3Folder + "破残族谱.png",
                "拖动字块补全名字。族谱空位已有“陈”，后面还缺两个字。",
                new[] { "第二字", "第三字" },
                new[]
                {
                    new PuzzlePiece("望字块", Chapter3Folder + "望字块.png"),
                    new PuzzlePiece("月字块", Chapter3Folder + "月字块.png")
                },
                new[] { "望字块", "月字块" },
                CompleteNamePuzzle);
        }

        private void CompleteNamePuzzle()
        {
            SetFlag("c3_name_restored", "你把“望”放在“陈”后，又把“月”放在“望”后。名字补全：陈望月。");
            Inspect("破残族谱", "族谱空位中，陈望月三个字终于重新出现。", Chapter3Folder + "破残族谱.png");
        }

        private void ShowGatePuzzle()
        {
            if (!NeedItem("灯楼木牌", "门缝里没有声音。也许灯影视角能找到开门用的东西。"))
            {
                return;
            }

            ShowGenericPuzzle("灯楼大门",
                Chapter4Folder + "灯楼外区域灯影下场景.png",
                "拖动灯楼木牌嵌入门上的凹槽。",
                new[] { "门上凹槽" },
                new[] { new PuzzlePiece("灯楼木牌", Chapter4Folder + "灯楼木牌.png") },
                new[] { "灯楼木牌" },
                CompleteGatePuzzle);
        }

        private void CompleteGatePuzzle()
        {
            SetFlag("c4_gate_open", "灯楼木牌嵌入门上凹槽，大门无声打开。当前目标：进入族谱室。");
            SetObjective("进入族谱室");
        }

        private void ShowLampPuzzle()
        {
            if (flags.Contains("c4_all_lit"))
            {
                Say("三盏灯已经点亮。中央族谱可以打开了。");
                return;
            }

            ShowGenericPuzzle("点亮三灯",
                Chapter4Folder + "族谱室灯影下的场景.png",
                "拖动灯到对应文字。红灯引人入祭，白灯送魂归河，黑灯封名藏忆。",
                new[] { "陈望月", "替灯入船", "林照萤" },
                new[]
                {
                    new PuzzlePiece("白灯", Chapter4Folder + "未点亮的白灯.png"),
                    new PuzzlePiece("红灯", Chapter4Folder + "未点亮的红灯.png"),
                    new PuzzlePiece("黑灯", Chapter4Folder + "未点亮的黑灯.png")
                },
                new[] { "白灯", "红灯", "黑灯" },
                CompleteLampPuzzle);
        }

        private void CompleteLampPuzzle()
        {
            flags.Add("c4_white_lit");
            flags.Add("c4_red_lit");
            flags.Add("c4_black_lit");
            flags.Add("c4_all_lit");
            selectedLamp = null;
            SetObjective("查看打开的族谱");
            Say("三盏灯同时亮起。中央族谱打开，去查看封名真相。");
            RefreshHud();
            RebuildProps();
            RebuildHotspots();
        }

        private void ClearModalActions()
        {
            if (modalActionRoot == null)
            {
                return;
            }

            for (var i = modalActionRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(modalActionRoot.GetChild(i).gameObject);
            }
        }

        private void SelectLamp(string lampName)
        {
            selectedLamp = lampName;
            Say("已选择" + lampName + "。灯影中文字浮起，选择它对应的名字。");
        }

        private void PairLamp(string text, string correctLamp, string flag, string litMessage)
        {
            if (string.IsNullOrEmpty(selectedLamp))
            {
                Say("先点击一盏灯，再点击对应文字。");
                return;
            }

            if (selectedLamp != correctLamp)
            {
                selectedLamp = null;
                Say("灯火晃动了一下，名字没有留下。");
                return;
            }

            flags.Add(flag);
            selectedLamp = null;
            Say(litMessage);
            if (flags.Contains("c4_red_lit") && flags.Contains("c4_white_lit") && flags.Contains("c4_black_lit") && !flags.Contains("c4_all_lit"))
            {
                flags.Add("c4_all_lit");
                SetObjective("查看打开的族谱");
                Say("三盏灯同时亮起。中央族谱打开，去查看封名真相。");
            }

            RefreshHud();
            RebuildProps();
            RebuildHotspots();
        }

        private void ShowFinalChoice()
        {
            EnsureChapter4EndingItem();
            titleText.text = "第四章 无声灯楼 / 最终选择";
            SetObjective("做出最终选择");
            Say("黑灯台前只剩三个选择。");
            ClearHotspots();
            SetPlayerVisible(false);
            CreateHotspot(new HotspotDefinition("隐瞒真相", new Rect(0.16f, 0.42f, 0.36f, 0.58f), delegate { ShowEnding("沉灯"); }));
            CreateHotspot(new HotspotDefinition("公开真相", new Rect(0.40f, 0.42f, 0.60f, 0.58f), delegate { ShowEnding("归灯"); }));
            CreateHotspot(new HotspotDefinition("留在灯楼", new Rect(0.64f, 0.42f, 0.84f, 0.58f), delegate { ShowEnding("守灯"); }));
        }

        private void EnsureChapter4EndingItem()
        {
            if (currentChapterId != "chapter4" || inventory.Contains("守灵灯钥"))
            {
                return;
            }

            inventory.Add("守灵灯钥");
            RefreshHud();
        }

        private void ShowEnding(string ending)
        {
            if (ending == "沉灯")
            {
                Dialogue("结局一：沉灯",
                    "陈望川：你还是不打算公开？\n林照萤：我不知道公开之后，会变成什么样。\n陈望川：可他们已经等了太久。\n林照萤：对不起。\n陈望川：这句话，你不该对我说。\n\n一个名字回来了。更多名字仍在灯中沉默。渡灯镇依旧安静。",
                    Chapter4Folder + "林照萤角色立绘.png");
                CompleteChapter("第四章结束。\n结局一：沉灯");
                return;
            }

            if (ending == "归灯")
            {
                Dialogue("结局二：归灯",
                    "林照萤：这些名字，不该再被藏起来。\n陈望川：望月……\n陈望月残影：哥。\n陈望川：对不起。\n陈望月残影：你终于记得我了。\n\n黑灯熄灭。名字回到族谱。渡灯镇再也无法假装什么都没有发生。",
                    Chapter4Folder + "陈望月角色立绘.png");
                CompleteChapter("第四章结束。\n结局二：归灯");
                return;
            }

            Dialogue("结局三：守灯",
                "陈望川：你真的要留下？\n林照萤：总要有人记得他们。\n陈望川：这不是你的错。\n林照萤：但我已经记起来了。\n\n林照萤留在了灯楼。她不再封住名字。每一盏灯熄灭前，她都会念出灯中的姓名。",
                Chapter4Folder + "林照萤角色立绘.png");
            CompleteChapter("第四章结束。\n结局三：守灯");
        }

        private void CompleteChapter(string message)
        {
            Say(message);
            ClearHotspots();
            SetPlayerVisible(false);
            CreateHotspot(new HotspotDefinition("返回章节选择", new Rect(0.36f, 0.42f, 0.64f, 0.58f), ShowChapterSelect));
        }

        private void LoadPlayerFrames()
        {
            playerIdleFrames.Clear();
            playerWalkFrames.Clear();
            playerLanternFrames.Clear();
            playerIdleFrames.AddRange(LoadSpriteSheet(CharacterFolder + "待机1.png", 5, 1, 1, true));
            playerWalkFrames.AddRange(LoadSpriteSheet(CharacterFolder + "行走1.png", 8, 1, 8, true));
            playerLanternFrames.AddRange(LoadSpriteSheet(CharacterFolder + "提灯.jpg", 8, 1, 8, true, true));
            playerDisplaySize = GetPlayerDisplaySizeFromWalkFrames();
            SetPlayerAnimation("idle", true);
        }

        private void ShowPlayerInScene()
        {
            if (string.IsNullOrEmpty(currentSceneId))
            {
                SetPlayerVisible(false);
                return;
            }

            SetPlayerVisible(true);
            SetPlayerAnimation(lightView ? "lantern" : "idle", true);
            PositionPlayer();
        }

        private void SetPlayerVisible(bool visible)
        {
            if (characterRoot != null)
            {
                characterRoot.gameObject.SetActive(visible);
            }
        }

        private void UpdatePlayer(float deltaTime)
        {
            if (inputLocked || playerImage == null || characterRoot == null || !characterRoot.gameObject.activeSelf || modal.activeSelf || dialoguePanel.activeSelf || (maskPuzzlePanel != null && maskPuzzlePanel.activeSelf) || (genericPuzzlePanel != null && genericPuzzlePanel.activeSelf) || (cuttingTablePanel != null && cuttingTablePanel.activeSelf))
            {
                return;
            }

            var axis = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                axis -= 1f;
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                axis += 1f;
            }

            if (Mathf.Abs(axis) > 0.01f)
            {
                var playerWorldX = sceneWidth * playerX;
                var nextPlayerWorldX = playerWorldX + axis * 430f * deltaTime;
                if (TryHandleChapter1BoundaryTransition(axis, nextPlayerWorldX))
                {
                    return;
                }

                if (TryHandleChapter2BoundaryTransition(axis, nextPlayerWorldX))
                {
                    return;
                }

                if (TryHandleChapter4BoundaryTransition(axis, nextPlayerWorldX))
                {
                    return;
                }

                playerWorldX = nextPlayerWorldX;
                var padding = sceneWidth * GetPlayerEdgePadding();
                playerWorldX = Mathf.Clamp(playerWorldX, padding, Mathf.Max(padding, sceneWidth - padding));
                playerX = Mathf.Clamp01(playerWorldX / Mathf.Max(1f, sceneWidth));
                playerFacingRight = axis > 0f;
                SetPlayerAnimation("walk", false);
                PositionPlayer();
            }
            else
            {
                SetPlayerAnimation(lightView ? "lantern" : "idle", false);
            }

            TickPlayerAnimation(deltaTime);
        }

        private bool TryHandleChapter1BoundaryTransition(float axis, float nextPlayerWorldX)
        {
            if (currentChapterId != "chapter1" || string.IsNullOrEmpty(currentSceneId))
            {
                return false;
            }

            var padding = sceneWidth * GetPlayerEdgePadding();
            if (axis > 0f && nextPlayerWorldX < sceneWidth - padding)
            {
                return false;
            }

            if (axis < 0f && nextPlayerWorldX > padding)
            {
                return false;
            }

            var spawnSide = axis > 0f ? SpawnLeft : SpawnRight;
            var targetSceneId = GetChapter1BoundaryTarget(axis);
            if (targetSceneId == BoundaryBlocked)
            {
                return true;
            }

            if (string.IsNullOrEmpty(targetSceneId))
            {
                return false;
            }

            playerFacingRight = axis > 0f;
            SetPlayerAnimation("walk", false);
            GoToScene(targetSceneId, spawnSide);
            return true;
        }

        private string GetChapter1BoundaryTarget(float axis)
        {
            if (axis > 0f)
            {
                switch (currentSceneId)
                {
                    case "c1_town_gate":
                        if (!flags.Contains("c1_town_checked"))
                        {
                            SayBoundaryBlocked("先调查镇口标识物。");
                            return BoundaryBlocked;
                        }

                        return "c1_bridge";
                    case "c1_bridge":
                        if (flags.Contains("c1_lantern_lit") && !flags.Contains("c1_seen_named_lantern"))
                        {
                            SayBoundaryBlocked("先按 Q 进入灯影视角，再调查河灯。");
                            return BoundaryBlocked;
                        }

                        if (flags.Contains("c1_lantern_lit") && flags.Contains("c1_seen_named_lantern"))
                        {
                            return "c1_well";
                        }

                        return "c1_grandma_home";
                    case "c1_grandma_home":
                        if (!inventory.Contains("黑灯（未点燃）") && !inventory.Contains("黑灯（已点燃）"))
                        {
                            SayBoundaryBlocked("先找到外婆留下的黑灯。");
                            return BoundaryBlocked;
                        }

                        if (flags.Contains("c1_lantern_lit"))
                        {
                            return "c1_well";
                        }

                        return "c1_mourning_hall";
                    case "c1_mourning_hall":
                        if (!flags.Contains("c1_lantern_lit"))
                        {
                            SayBoundaryBlocked("黑灯还没有点燃。");
                            return BoundaryBlocked;
                        }

                        return "c1_well";
                }
            }

            if (axis < 0f)
            {
                switch (currentSceneId)
                {
                    case "c1_bridge":
                        return "c1_town_gate";
                    case "c1_grandma_home":
                        return "c1_bridge";
                    case "c1_mourning_hall":
                        return "c1_grandma_home";
                    case "c1_well":
                        return "c1_mourning_hall";
                }
            }

            return null;
        }

        private bool TryHandleChapter2BoundaryTransition(float axis, float nextPlayerWorldX)
        {
            if (currentChapterId != "chapter2" || string.IsNullOrEmpty(currentSceneId))
            {
                return false;
            }

            var padding = sceneWidth * GetPlayerEdgePadding();
            if (axis > 0f && nextPlayerWorldX < sceneWidth - padding)
            {
                return false;
            }

            if (axis < 0f && nextPlayerWorldX > padding)
            {
                return false;
            }

            var side = axis > 0f ? SpawnRight : SpawnLeft;
            var spawnSide = axis > 0f ? SpawnLeft : SpawnRight;
            var targetSceneId = GetChapter2BoundaryTarget(side, out var blockedMessage);
            if (!string.IsNullOrEmpty(blockedMessage))
            {
                SayBoundaryBlocked(blockedMessage);
                return true;
            }

            if (string.IsNullOrEmpty(targetSceneId))
            {
                return false;
            }

            playerFacingRight = axis > 0f;
            SetPlayerAnimation("walk", false);
            GoToScene(targetSceneId, spawnSide);
            return true;
        }

        private bool TryHandleChapter4BoundaryTransition(float axis, float nextPlayerWorldX)
        {
            if (currentChapterId != "chapter4" || string.IsNullOrEmpty(currentSceneId))
            {
                return false;
            }

            var padding = sceneWidth * GetPlayerEdgePadding();
            if (axis > 0f && nextPlayerWorldX < sceneWidth - padding)
            {
                return false;
            }

            if (axis < 0f && nextPlayerWorldX > padding)
            {
                return false;
            }

            var spawnSide = axis > 0f ? SpawnLeft : SpawnRight;
            var targetSceneId = GetChapter4BoundaryTarget(axis);
            if (targetSceneId == BoundaryBlocked)
            {
                return true;
            }

            if (string.IsNullOrEmpty(targetSceneId))
            {
                return false;
            }

            playerFacingRight = axis > 0f;
            SetPlayerAnimation("walk", false);
            GoToScene(targetSceneId, spawnSide);
            return true;
        }

        private string GetChapter4BoundaryTarget(float axis)
        {
            if (axis > 0f)
            {
                switch (currentSceneId)
                {
                    case "c4_tower_gate":
                        if (!flags.Contains("c4_gate_open"))
                        {
                            SayBoundaryBlocked("灯楼大门还没有打开。");
                            return BoundaryBlocked;
                        }

                        return "c4_genealogy";
                    case "c4_genealogy":
                        if (!flags.Contains("c4_truth_genealogy"))
                        {
                            SayBoundaryBlocked("先查看打开的族谱。");
                            return BoundaryBlocked;
                        }

                        return "c4_black_lamp_stage";
                }
            }

            if (axis < 0f)
            {
                switch (currentSceneId)
                {
                    case "c4_genealogy":
                        return "c4_tower_gate";
                    case "c4_black_lamp_stage":
                        return "c4_genealogy";
                }
            }

            return null;
        }

        private bool TryActivateBoundaryNavigation()
        {
            if (TryActivateChapter3BoatDarkroomNavigation())
            {
                return true;
            }

            if (TryActivateChapter3BridgeBoatNavigation())
            {
                return true;
            }

            if (TryActivateChapter3TunnelBridgeNavigation())
            {
                return true;
            }

            if (TryActivateChapter3BackstageNavigation())
            {
                return true;
            }

            if (string.IsNullOrEmpty(boundaryNavigationTargetSceneId) && string.IsNullOrEmpty(boundaryNavigationBlockedMessage))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(boundaryNavigationBlockedMessage))
            {
                SayBoundaryBlocked(boundaryNavigationBlockedMessage);
                return true;
            }

            GoToScene(boundaryNavigationTargetSceneId, boundaryNavigationSpawnSide);
            ClearBoundaryNavigationPrompt();
            return true;
        }

        private bool TryActivateChapter3BoatDarkroomNavigation()
        {
            if (currentChapterId != "chapter3" || currentSceneId != "c3_boat")
            {
                return false;
            }

            if (!IsPlayerInNavigationZone(new Rect(0.68f, 0.18f, 0.98f, 0.58f)))
            {
                return false;
            }

            if (!HasChapter3FullTag())
            {
                SayBoundaryBlocked("没有陈望月名牌，暗门不会打开。");
                return true;
            }

            GoToScene("c3_darkroom", SpawnLeft);
            ClearBoundaryNavigationPrompt();
            return true;
        }

        private bool TryActivateChapter3BridgeBoatNavigation()
        {
            if (currentChapterId != "chapter3" || currentSceneId != "c3_bridge")
            {
                return false;
            }

            if (!IsPlayerInNavigationZone(new Rect(0.70f, 0.16f, 0.98f, 0.58f)))
            {
                return false;
            }

            if (!HasChapter3BoatClue())
            {
                SayBoundaryBlocked("先在断桥边找到名单碎片。");
                return true;
            }

            GoToScene("c3_boat", SpawnLeft);
            ClearBoundaryNavigationPrompt();
            return true;
        }

        private bool TryActivateChapter3TunnelBridgeNavigation()
        {
            if (currentChapterId != "chapter3" || currentSceneId != "c3_tunnel")
            {
                return false;
            }

            if (!IsPlayerInNavigationZone(new Rect(0.58f, 0.20f, 0.98f, 0.60f)))
            {
                return false;
            }

            if (!HasChapter3NamePaper())
            {
                SayBoundaryBlocked("你还没有拿到封名残纸。");
                return true;
            }

            GoToScene("c3_bridge", SpawnLeft);
            ClearBoundaryNavigationPrompt();
            return true;
        }

        private bool TryActivateChapter3BackstageNavigation()
        {
            if (currentChapterId != "chapter3" || currentSceneId != "c3_stage")
            {
                return false;
            }

            if (!IsPlayerInNavigationZone(new Rect(0.76f, 0.20f, 0.98f, 0.58f)))
            {
                return false;
            }

            if (!IsChapter3StageReadyForBackstage())
            {
                SayBoundaryBlocked("先调查舞台上的线索。");
                return true;
            }

            GoToScene("c3_backstage", SpawnLeft);
            ClearBoundaryNavigationPrompt();
            return true;
        }

        private void UpdateBoundaryNavigationPrompt()
        {
            ClearBoundaryNavigationState();

            if (boundaryPromptImage == null || inputLocked || currentChapterId != "chapter3" || string.IsNullOrEmpty(currentSceneId) || playerImage == null || characterRoot == null || !characterRoot.gameObject.activeSelf || modal.activeSelf || dialoguePanel.activeSelf || (maskPuzzlePanel != null && maskPuzzlePanel.activeSelf) || (genericPuzzlePanel != null && genericPuzzlePanel.activeSelf) || (cuttingTablePanel != null && cuttingTablePanel.activeSelf))
            {
                SetBoundaryPromptVisible(false);
                return;
            }

            if (TrySetupChapter3Navigation())
            {
                return;
            }

            SetBoundaryPromptVisible(false);
        }

        private bool TrySetupChapter3Navigation()
        {
            if (currentChapterId != "chapter3")
            {
                return false;
            }

            switch (currentSceneId)
            {
                case "c3_stage":
                    if (TrySetupChapterNavigationZone(new Rect(0.42f, 0.12f, 0.62f, 0.34f), flags.Contains("c3_tunnel_entrance_open") ? "c3_tunnel" : null, null, SpawnLeft))
                    {
                        return true;
                    }
                    return TrySetupChapterNavigationZone(new Rect(0.76f, 0.20f, 0.98f, 0.58f), IsChapter3StageReadyForBackstage() ? "c3_backstage" : null, "先调查舞台上的线索。", SpawnLeft);
                case "c3_backstage":
                    return TrySetupChapterNavigationZone(new Rect(0.02f, 0.22f, 0.18f, 0.52f), "c3_stage", null, SpawnRight);
                case "c3_tunnel":
                    if (TrySetupChapterNavigationZone(new Rect(0.02f, 0.20f, 0.18f, 0.52f), "c3_stage", null, SpawnRight))
                    {
                        return true;
                    }
                    return TrySetupChapterNavigationZone(new Rect(0.58f, 0.20f, 0.98f, 0.60f), HasChapter3NamePaper() ? "c3_bridge" : null, "你还没有拿到封名残纸。", SpawnLeft);
                case "c3_bridge":
                    if (TrySetupChapterNavigationZone(new Rect(0.02f, 0.20f, 0.18f, 0.52f), "c3_tunnel", null, SpawnRight))
                    {
                        return true;
                    }
                    return TrySetupChapterNavigationZone(new Rect(0.70f, 0.16f, 0.98f, 0.58f), HasChapter3BoatClue() ? "c3_boat" : null, "先在断桥边找到名单碎片。", SpawnLeft);
                case "c3_boat":
                    if (TrySetupChapterNavigationZone(new Rect(0.02f, 0.20f, 0.18f, 0.52f), "c3_bridge", null, SpawnRight))
                    {
                        return true;
                    }
                    return TrySetupChapterNavigationZone(new Rect(0.68f, 0.18f, 0.98f, 0.58f), HasChapter3FullTag() ? "c3_darkroom" : null, "没有陈望月名牌，暗门不会打开。", SpawnLeft);
                case "c3_darkroom":
                    return TrySetupChapterNavigationZone(new Rect(0.02f, 0.20f, 0.18f, 0.52f), "c3_boat", null, SpawnRight);
            }

            return false;
        }

        private bool TrySetupChapterNavigationZone(Rect area, string targetSceneId, string blockedMessage, int spawnSide)
        {
            if (!IsPlayerInNavigationZone(area))
            {
                return false;
            }

            if (string.IsNullOrEmpty(targetSceneId) && string.IsNullOrEmpty(blockedMessage))
            {
                return false;
            }

            boundaryNavigationTargetSceneId = targetSceneId;
            boundaryNavigationBlockedMessage = blockedMessage;
            boundaryNavigationSpawnSide = spawnSide;
            boundaryNavigationSide = spawnSide == SpawnLeft ? SpawnRight : SpawnLeft;
            PositionBoundaryPrompt();
            SetBoundaryPromptVisible(true);
            return true;
        }

        private bool IsChapter3StageReadyForBackstage()
        {
            return flags.Contains("c3_stage_checked")
                || flags.Contains("c3_stage_talk")
                || flags.Contains("c3_stage_words")
                || flags.Contains("c3_script");
        }

        private bool HasItemOrFlag(string item, string flag)
        {
            return inventory.Contains(item) || flags.Contains(flag);
        }

        private bool HasChapter3NamePaper()
        {
            if (flags.Contains("c3_name_paper") || inventory.Contains("封名残纸"))
            {
                return true;
            }

            foreach (var item in inventory)
            {
                if (item.Contains("残纸") || item.Contains("封名"))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasChapter3BoatClue()
        {
            if (flags.Contains("c3_ghost_seen") || flags.Contains("c3_list_piece") || inventory.Contains("名单碎片"))
            {
                return true;
            }

            foreach (var item in inventory)
            {
                if (item.Contains("名单") || item.Contains("碎片"))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasChapter3FullTag()
        {
            if (flags.Contains("c3_full_tag") || inventory.Contains("陈望月名牌"))
            {
                return true;
            }

            foreach (var item in inventory)
            {
                if (item.Contains("陈望月") || item.Contains("名牌"))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsPlayerInNavigationZone(Rect area)
        {
            return playerX >= area.x && playerX <= area.width;
        }

        private void ClearBoundaryNavigationState()
        {
            boundaryNavigationTargetSceneId = null;
            boundaryNavigationBlockedMessage = null;
            boundaryNavigationSpawnSide = SpawnDefault;
            boundaryNavigationSide = SpawnDefault;
        }

        private void ClearBoundaryNavigationPrompt()
        {
            ClearBoundaryNavigationState();
            SetBoundaryPromptVisible(false);
        }

        private void SetBoundaryPromptVisible(bool visible)
        {
            if (boundaryPromptImage != null && boundaryPromptImage.gameObject.activeSelf != visible)
            {
                boundaryPromptImage.gameObject.SetActive(visible);
            }
        }

        private void PositionBoundaryPrompt()
        {
            if (boundaryPromptImage == null)
            {
                return;
            }

            var playerScreenX = sceneWidth * playerX - cameraX;
            var x = Mathf.Clamp(playerScreenX - viewportWidth * 0.5f, -viewportWidth * 0.42f, viewportWidth * 0.42f);
            var y = Mathf.Clamp(320f - viewportHeight * 0.5f, -viewportHeight * 0.40f, viewportHeight * 0.18f);
            boundaryPromptImage.rectTransform.anchoredPosition = new Vector2(x, y);
        }

        private string GetChapter2BoundaryTarget(int side, out string blockedMessage)
        {
            blockedMessage = null;

            if (side == SpawnRight)
            {
                switch (currentSceneId)
                {
                    case "c2_door":
                        if (!flags.Contains("c2_door_sign_checked") || !flags.Contains("c2_door_lantern_checked"))
                        {
                            blockedMessage = "Check the sign and red lantern first.";
                            return null;
                        }

                        return "c2_front";
                    case "c2_front":
                        return "c2_paper_people";
                    case "c2_paper_people":
                        if (!inventory.Contains("后院小钥匙"))
                        {
                            blockedMessage = "门已上锁，需要找到钥匙。";
                            return null;
                        }

                        return "c2_cutting_table";
                    case "c2_cutting_table":
                        if (!flags.Contains("c2_projection_seen"))
                        {
                            blockedMessage = "还没有得到焚纸炉的提示。";
                            return null;
                        }

                        return "c2_furnace";
                }
            }

            if (side == SpawnLeft)
            {
                switch (currentSceneId)
                {
                    case "c2_front":
                        return "c2_door";
                    case "c2_paper_people":
                        return "c2_front";
                    case "c2_cutting_table":
                        return "c2_paper_people";
                    case "c2_furnace":
                        return "c2_cutting_table";
                }
            }

            return null;
        }

        private void SayBoundaryBlocked(string message)
        {
            if (Time.unscaledTime < nextBoundaryBlockMessageTime)
            {
                return;
            }

            nextBoundaryBlockMessageTime = Time.unscaledTime + 0.85f;
            Say(message);
        }

        private void SetPlayerAnimation(string state, bool restart)
        {
            if (!restart && playerState == state)
            {
                return;
            }

            playerState = state;
            currentPlayerFrames = playerIdleFrames;
            if (state == "walk" && playerWalkFrames.Count > 0)
            {
                currentPlayerFrames = playerWalkFrames;
            }
            else if (state == "lantern" && playerLanternFrames.Count > 0)
            {
                currentPlayerFrames = playerLanternFrames;
            }

            playerFrameIndex = 0;
            playerFrameTime = 0f;
            ApplyPlayerFrame();
        }

        private void TickPlayerAnimation(float deltaTime)
        {
            if (currentPlayerFrames == null || currentPlayerFrames.Count == 0)
            {
                return;
            }

            var fps = playerState == "walk" ? 10f : 4f;
            var frameDuration = 1f / fps;
            playerFrameTime += deltaTime;
            while (playerFrameTime >= frameDuration)
            {
                playerFrameTime -= frameDuration;
                playerFrameIndex = (playerFrameIndex + 1) % currentPlayerFrames.Count;
                ApplyPlayerFrame();
            }
        }

        private void ApplyPlayerFrame()
        {
            if (playerImage == null || currentPlayerFrames == null || currentPlayerFrames.Count == 0)
            {
                return;
            }

            playerImage.sprite = currentPlayerFrames[Mathf.Clamp(playerFrameIndex, 0, currentPlayerFrames.Count - 1)];
            playerImage.color = Color.white;
            PositionPlayer();
        }

        private void PositionPlayer()
        {
            if (playerImage == null)
            {
                return;
            }

            var rect = playerImage.rectTransform;
            rect.sizeDelta = playerDisplaySize;
            rect.anchoredPosition = new Vector2(sceneWidth * playerX, 104f);
            rect.localScale = new Vector3(playerFacingRight ? 1f : -1f, 1f, 1f);
        }

        private Vector2 GetPlayerDisplaySizeFromWalkFrames()
        {
            var aspect = GetAverageSpriteAspect(playerWalkFrames);
            if (aspect <= 0f)
            {
                aspect = PlayerIdleWidth / PlayerIdleHeight;
            }

            return new Vector2(PlayerDisplayHeight * aspect, PlayerDisplayHeight);
        }

        private static float GetAverageSpriteAspect(IReadOnlyList<Sprite> sprites)
        {
            if (sprites == null || sprites.Count == 0)
            {
                return 0f;
            }

            var total = 0f;
            var count = 0;
            foreach (var sprite in sprites)
            {
                if (sprite == null)
                {
                    continue;
                }

                total += sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
                count++;
            }

            return count > 0 ? total / count : 0f;
        }

        private void RefreshHud()
        {
            lightText.text = "灯影视角：" + (lightView ? "已开启" : "未开启") + "  Q";
            inventoryText.text = inventory.Count == 0 ? "背包：空" : "背包：" + string.Join("、", inventory);
        }

        private void BuildData()
        {
            BuildChapter1();
            BuildChapter2();
            BuildChapter3();
            BuildChapter4();
        }

        private void BuildChapter1()
        {
            chapters["chapter1"] = new ChapterDefinition("第一章 归镇", "c1_town_gate", "当前目标：回到渡灯镇，完成移动与互动教学。");

            AddScene(new SceneDefinition("c1_town_gate", "chapter1", "镇口", Chapter1Folder, "镇口区域", "镇口区域灯影下的场景", "A/D 移动，E 互动；调查镇口标识物后向右前往石桥")
                .WithLightMessage("镇口在灯影下发白，门楼深处像藏着一层旧灰。")
                .Add("镇口标识物", new Rect(0.26f, 0.44f, 0.58f, 0.76f), delegate
                {
                    SetFlag("c1_town_checked", "林照萤：渡灯镇……我已经很多年没回来了。\n林照萤：这里和记忆里一样，却又像少了什么。\n当前目标：前往石桥。");
                })
                .Add("镇口地面", new Rect(0.28f, 0.10f, 0.58f, 0.28f), delegate
                {
                    Say("纸灰被风吹过门楼，像刚烧完没多久。");
                }));

            AddScene(new SceneDefinition("c1_bridge", "chapter1", "石桥", Chapter1Folder, "石桥区域", "石桥区域灯影下场景", "调查桥边河灯；之后继续向右寻找外婆留下的黑灯")
                .WithLightMessage("河灯上的字在灯影下浮出，水面像在把名字往下拖。")
                .AddProp("普通河灯", Chapter1Folder + "普通河灯.png", new Rect(0.2592f, 0.2383f, 0.3792f, 0.3783f), 1f, false, null, "c1_seen_named_lantern")
                .AddProp("写有名字的河灯", Chapter1Folder + "写有名字的河灯.png", new Rect(0.2192f, 0.2083f, 0.4192f, 0.4383f), 1f, true, "c1_seen_named_lantern")
                .Add("河灯", new Rect(0.2192f, 0.2183f, 0.4192f, 0.4583f), delegate
                {
                    if (!flags.Contains("c1_lantern_lit"))
                    {
                        Inspect("普通河灯", "林照萤：河灯还亮着，像是刚被人放下。", Chapter1Folder + "普通河灯.png");
                        return;
                    }

                    if (!lightView)
                    {
                        Say("也许要用黑灯照一照河灯。");
                        return;
                    }

                    Take("写名河灯线索", "c1_seen_named_lantern", "林照萤：这上面……写的是我的名字。\n林照萤：为什么我的名字会在河灯上？");
                    Inspect("写有名字的河灯", "灯影下，河灯上浮出“林照萤”三个字。水面像在把这个名字拖进更深处。", Chapter1Folder + "写有名字的河灯.png");
                    SetObjective("去老井查清名字的来源");
                }, null, "c1_seen_named_lantern"));

            AddScene(new SceneDefinition("c1_grandma_home", "chapter1", "外婆家", Chapter1Folder, "外婆家", "外婆家灯影", "寻找外婆留下的东西，获得黑灯")
                .WithLightMessage("外婆家在灯影下泛出冷光，旧物的影子像还停在原处。")
                .AddProp("旧藤椅", Chapter1Folder + "旧藤椅.png", new Rect(0.2225f, 0.20f, 0.4025f, 0.46f), 1f, false, null, null, true, 0f, -1f, 1f)
                .AddProp("香炉", Chapter1Folder + "香炉.png", new Rect(0.5981f, 0.35f, 0.6981f, 0.51f), 1f, false, null, null, true, 0f, 0.91749f, 0.91749f)
                .AddProp("黑灯", Chapter1Folder + "黑灯.png", new Rect(0.6471f, 0.4724f, 0.8071f, 0.7924f), 1f, false, null, "c1_has_lantern", true, 0f, 0.98834f, 0.98834f)
                .Add("旧藤椅", new Rect(0.2225f, 0.24f, 0.4025f, 0.50f), delegate
                {
                    Say("林照萤：藤椅还在，只是没人再坐了。");
                })
                .Add("香炉", new Rect(0.5781f, 0.37f, 0.7181f, 0.53f), delegate
                {
                    Say("林照萤：香灰还没散……有人来过？");
                })
                .Add("黑灯", new Rect(0.6271f, 0.4524f, 0.8271f, 0.8124f), delegate
                {
                    Take("黑灯（未点燃）", "c1_has_lantern", "林照萤：这盏黑灯……外婆以前从不让我碰。\n获得道具：黑灯（未点燃）。\n当前目标：去灵堂找到点燃黑灯的方法。");
                    Inspect("黑灯（未点燃）", "灯芯冰冷，灯罩内侧有被烟熏出的黑色纹路。黑灯未点燃前，无法进入灯影视角。", Chapter1Folder + "黑灯.png");
                    SetObjective("去灵堂找到点燃黑灯的方法");
                }, null, "c1_has_lantern"));

            AddScene(new SceneDefinition("c1_mourning_hall", "chapter1", "灵堂", Chapter1Folder, "灵堂区域", "灵堂区域灯影下场景", "按果、糕、酒、香、火的顺序摆放供品，点燃黑灯")
                .WithLightMessage("供桌残留痕迹浮现：果印、糕屑、杯痕、香灰、烛油。")
                .AddProp("未摆贡品的供桌", Chapter1Folder + "未摆贡品的供桌.png", new Rect(0.10f, 0.10f, 0.87f, 0.48f), 1f, false, null, "c1_offering_solved")
                .AddProp("摆满贡品的供桌", Chapter1Folder + "摆满贡品的供桌.png", new Rect(0.10f, 0.10f, 0.87f, 0.48f), 1f, false, "c1_offering_solved")
                .AddProp("纸人", Chapter1Folder + "纸人.png", new Rect(0.61f, 0.08f, 0.94f, 0.74f), 1f, false, null, null, true, 0f, 0.17099f, 0.17099f)
                .AddProp("外婆", Chapter1Folder + "外婆.png", new Rect(0.13f, 0.10f, 0.63f, 0.70f), 0.72f, false, "c1_lantern_lit", null, true, 0f, 0.65365f, 0.65365f)
                .Add("供桌", new Rect(0.10f, 0.10f, 0.87f, 0.48f), delegate
                {
                    if (flags.Contains("c1_offering_solved"))
                    {
                        Inspect("摆满贡品的供桌", "苹果、糕点、酒杯、香炉、白蜡烛已经归位。黑灯的火光贴着供桌边缘晃动。", Chapter1Folder + "摆满贡品的供桌.png");
                        return;
                    }

                    Say("林照萤：供桌上的位置是空的，像是在等什么。\n残留痕迹：果印、糕屑、杯痕、香灰、烛油。");
                })
                .Add("纸人", new Rect(0.61f, 0.08f, 0.94f, 0.74f), delegate
                {
                    Inspect("纸人", "林照萤：纸人的脸……好像被人重新画过。", Chapter1Folder + "纸人.png");
                })
                .Add("摆放供品", new Rect(0.10f, 0.10f, 0.87f, 0.48f), delegate
                {
                    ShowOfferingPuzzle();
                }, null, "c1_offering_solved"));

            AddScene(new SceneDefinition("c1_well", "chapter1", "老井", Chapter1Folder, "老井区域", "老井区域灯影下的场景", "用已点燃的黑灯照井口，找到被藏住的名字")
                .WithLightMessage("井水出现冷光，名字像从水底慢慢浮上来。")
                .AddProp("老井", Chapter1Folder + "老井.png", new Rect(0.38f, 0.18f, 0.64f, 0.48f), 1f)
                .AddProp("井中名字", Chapter1Folder + "井中名字.png", new Rect(0.34f, 0.18f, 0.70f, 0.52f), 1f, true, "c1_lantern_lit")
                .Add("老井", new Rect(0.34f, 0.18f, 0.70f, 0.56f), delegate
                {
                    if (!NeedFlag("c1_lantern_lit", "需要先点燃黑灯。"))
                    {
                        return;
                    }

                    if (!lightView)
                    {
                        Say("林照萤：井里太暗，什么也看不见。也许要用黑灯照一照。");
                        return;
                    }

                    Inspect("井中名字", "林照萤：水面上浮出的名字，不止一个。\n林照萤：这些名字……为什么会被藏在井里？\n外婆：被封住的，不是亡魂。\n外婆：是真相。", Chapter1Folder + "井中名字.png");
                    CompleteChapter("第一章结束：归镇。\n下一章：纸马铺 已解锁。");
                }));
        }

        private void BuildChapter2()
        {
            chapters["chapter2"] = new ChapterDefinition("第二章 纸马铺", "c2_door", "当前目标：进入纸马铺，寻找祭祀线索。");

            AddScene(new SceneDefinition("c2_door", "chapter2", "纸马铺门口", Chapter2Folder, "纸马铺门口", null, "进入纸马铺")
                .Add("招牌", new Rect(0.50f, 0.76f, 0.86f, 0.92f), delegate
                {
                    SetFlag("c2_door_sign_checked", "“纸马铺”三个字已经褪色，像是被烟熏了很多年。");
                }, null, "c2_door_sign_checked")
                .Add("红灯笼", new Rect(0.38f, 0.64f, 0.47f, 0.88f), delegate
                {
                    SetFlag("c2_door_lantern_checked", "红灯笼照着门口，门缝里透出一线暗光。");
                }, null, "c2_door_lantern_checked")
                .Add("进入纸马铺", new Rect(0.54f, 0.18f, 0.76f, 0.70f), delegate
                {
                    if (!flags.Contains("c2_door_sign_checked") || !flags.Contains("c2_door_lantern_checked"))
                    {
                        SayBoundaryBlocked("先调查纸马铺招牌和红灯笼。");
                        return;
                    }

                    GoToScene("c2_front", SpawnLeft);
                }));

            AddScene(new SceneDefinition("c2_front", "chapter2", "前厅", Chapter2Folder, "纸马铺前厅", "纸马铺前厅灯影下场景", "在纸马铺内寻找祭祀线索")
                .WithLightMessage("柜台下方像藏着暗格，抽屉也被灯影勾出了缝。")
                .AddProp("纸马铺老板", Chapter2Folder + "纸马铺老板_桌后.png", new Rect(0.31f, 0.16f, 0.51f, 0.82f), 1f, false, null, null, true, 0f, 0.71413f, 0.71413f)
                .AddProp("调查账本", Chapter2Folder + "调查账本.png", new Rect(0.31f, 0.25f, 0.45f, 0.46f), 1f, false, null, "c2_read_account", true, 0f, 0.43383f, 0.43383f)
                .AddProp("后院钥匙", Chapter2Folder + "后院钥匙.png", new Rect(0.27f, 0.16f, 0.34f, 0.28f), 1f, true, null, "c2_key", true, 0f, 0.56399f, 0.52169f)
                .AddProp("剪纸碎片三", Chapter2Folder + "剪纸碎片三.png", new Rect(0.48f, 0.18f, 0.56f, 0.31f), 1f, true, null, "c2_piece3", true, 0f, 0.37262f, 0.37262f)
                .Add("老板对话", new Rect(0.32f, 0.30f, 0.50f, 0.78f), delegate
                {
                    Inspect("纸马铺老板",
                        "老板：姑娘，这时候来纸马铺，可不太吉利。\n林照萤：我想问问，最近镇上是不是又在准备什么祭祀？\n老板：镇上的事，都是老规矩。纸马、纸人、纸灯，该扎的扎，该烧的烧。\n林照萤：我外婆留下的东西里，提到过这里。\n老板：沈掌灯人啊……她知道得太多了。",
                        Chapter2Folder + "纸马铺老板.png");
                    flags.Add("c2_talked_boss");
                    SetObjective("在纸马铺内寻找祭祀线索");
                })
                .Add("柜台账本", new Rect(0.34f, 0.30f, 0.42f, 0.40f), delegate
                {
                    Inspect("祭祀订单", "黑灯祭用纸人七具，封名纸一匣，纸马一对。账页夹缝里有窗花左下角的纹样。", Chapter2Folder + "调查账本.png");
                    flags.Add("c2_read_account");
                    RebuildProps();
                    RebuildHotspots();
                })
                .AddLight("暗格钥匙", new Rect(0.28f, 0.17f, 0.33f, 0.27f), delegate
                {
                    Take("后院小钥匙", "c2_key", "暗格里压着一把后院小钥匙。");
                }, null, "c2_key")
                .AddLight("抽屉碎片", new Rect(0.51f, 0.22f, 0.54f, 0.28f), delegate
                {
                    Take("剪纸碎片三", "c2_piece3", "打开的抽屉里有剪纸碎片三。");
                }, null, "c2_piece3"));

            AddScene(new SceneDefinition("c2_paper_people", "chapter2", "纸人架", Chapter2Folder, "纸人架区域", "纸人架区域灯影下的场景", "收集剪纸碎片并寻找方向提示")
                .WithLightMessage("一个纸人抬起手，僵硬地指向左侧：柜台。")
                .AddProp("剪纸碎片一", Chapter2Folder + "剪纸碎片一.png", new Rect(0.12f, 0.16f, 0.22f, 0.29f), 1f, false, null, "c2_piece1", true, 0f, 0.47174f, 0.47174f)
                .AddProp("剪纸碎片二", Chapter2Folder + "剪纸碎片二.png", new Rect(0.60f, 0.14f, 0.72f, 0.30f), 1f, false, null, "c2_piece2", true, 0f, 0.44124f, 0.44124f)
                .AddProp("挂名签", Chapter2Folder + "挂名签.png", new Rect(0.68f, 0.34f, 0.82f, 0.72f), 1f, false, null, null, true, 0f, 0.33358f, 0.33358f)
                .AddProp("纸人", Chapter2Folder + "纸人.png", new Rect(0.70f, 0.19f, 0.90f, 0.78f), 0.95f, false, null, null, true, 0f, 0.36346f, 0.36346f)
                .Add("剪纸碎片一", new Rect(0.14f, 0.18f, 0.20f, 0.27f), delegate { Take("剪纸碎片一", "c2_piece1", "你从纸人脚边拾起剪纸碎片一。"); }, null, "c2_piece1")
                .Add("剪纸碎片二", new Rect(0.63f, 0.18f, 0.68f, 0.26f), delegate { Take("剪纸碎片二", "c2_piece2", "木架下方卡着剪纸碎片二。"); }, null, "c2_piece2")
                .Add("挂名签纸人", new Rect(0.70f, 0.46f, 0.80f, 0.62f), delegate { Inspect("挂名签", "名签上的名字被涂黑，只剩墨迹干裂后的空白。", Chapter2Folder + "挂名签.png"); }));

            AddScene(new SceneDefinition("c2_cutting_table", "chapter2", "剪纸台", Chapter2Folder, "剪纸台区域", null, "调查后面的剪纸台")
                .AddProp("调查剪纸台", Chapter2Folder + "调查剪纸台.png", new Rect(0.18f, 0.14f, 0.86f, 0.70f), 1f, false, "__cutting_table_closeup_only", null, true, 0f, 1.50769f, 1.50769f)
                .AddProp("浆糊刷", Chapter2Folder + "浆糊刷.png", new Rect(0.30f, 0.28f, 0.43f, 0.58f), 1f, false, "__cutting_table_closeup_only", "c2_brush", true, 0f, 1.72764f, 1.72764f)
                .AddProp("剪纸碎片四", Chapter2Folder + "剪纸碎片四.png", new Rect(0.59f, 0.35f, 0.76f, 0.64f), 1f, false, "__cutting_table_closeup_only", "c2_piece4", true, 0f, 1.09644f, 1.09644f)
                .AddProp("窗户平面图", Chapter2Folder + "调查窗户平面图.png", new Rect(0.36f, 0.52f, 0.72f, 0.88f), 1f, false, "c2_window_complete", null, true, 0f, 1.2593f, 1.2593f)
                .Add("后面的剪纸台", new Rect(0.20f, 0.20f, 0.76f, 0.47f), delegate
                {
                    flags.Add("c2_cutting_table_revealed");
                    Say("你掀开后面的剪纸台，红纸、浆糊刷和最后一片剪纸露了出来。");
                    SetObjective("拿到剪纸碎片四和浆糊刷，完成窗花");
                    RebuildProps();
                    RebuildHotspots();
                    ShowCuttingTableCloseup();
                }, null, "c2_cutting_table_revealed")
                .Add("调查剪纸台", new Rect(0.18f, 0.14f, 0.86f, 0.70f), delegate
                {
                    ShowCuttingTableCloseup();
                }, "c2_cutting_table_revealed")
                .Add("浆糊刷", new Rect(0.30f, 0.28f, 0.43f, 0.58f), delegate { Take("浆糊刷", "c2_brush", "你拿到了浆糊刷。"); }, "__cutting_table_closeup_only", "c2_brush")
                .Add("剪纸碎片四", new Rect(0.59f, 0.35f, 0.76f, 0.64f), delegate { Take("剪纸碎片四", "c2_piece4", "红纸堆里藏着剪纸碎片四。"); }, "__cutting_table_closeup_only", "c2_piece4")
                .Add("旧窗户", new Rect(0.36f, 0.52f, 0.72f, 0.88f), delegate
                {
                    if (!NeedFlag("c2_piece1", "窗花还缺剪纸碎片一。") || !NeedFlag("c2_piece2", "窗花还缺剪纸碎片二。") || !NeedFlag("c2_piece3", "窗花还缺剪纸碎片三。") || !NeedFlag("c2_piece4", "窗花还缺剪纸碎片四。") || !NeedItem("浆糊刷", "还需要浆糊刷把窗花贴上。"))
                    {
                        return;
                    }

                    Inspect("完整窗花", "四片剪纸归位：一在左上，二在右上，三在左下，四在右下。窗花贴上旧窗后，等待灯影照出隐藏文字。", Chapter2Folder + "调查窗户平面图.png");
                    flags.Add("c2_window_complete");
                    SetObjective("按 Q 切换灯影视角，查看窗花投影");
                    RebuildProps();
                    RebuildHotspots();
                }, null, "c2_window_complete")
                .Add("窗花拖拽解密", new Rect(0.36f, 0.52f, 0.72f, 0.88f), delegate { ShowWindowPuzzle(); }, null, "c2_window_complete")
                .AddLight("窗花投影", new Rect(0.36f, 0.52f, 0.72f, 0.88f), delegate
                {
                    if (!NeedFlag("c2_window_complete", "先把完整窗花贴到旧窗上。"))
                    {
                        return;
                    }

                    SetFlag("c2_projection_seen", "墙上浮出字影：“纸未尽，名未灭。火过三分，残页可现。” 当前目标：前往焚纸炉，寻找未烧尽的残页。");
                    SetObjective("前往焚纸炉，寻找未烧尽的残页");
                })
                );

            AddScene(new SceneDefinition("c2_furnace", "chapter2", "焚纸炉", Chapter2Folder, "焚纸炉区域", "焚火炉区域灯影下场景", "取出未烧尽的残页")
                .WithLightMessage("炉灰里有微弱红光，像压着没有烧完的东西。")
                .AddProp("火钳", Chapter2Folder + "火钳.png", new Rect(0.60f, 0.18f, 0.78f, 0.30f), 1f, false, null, "c2_tongs", true, -59f, 0.60522f, 0.60522f)
                .AddProp("炉灰铲", Chapter2Folder + "炉灰铲.png", new Rect(0.42f, 0.18f, 0.56f, 0.40f), 1f, false, null, "c2_shovel", true, -18f, 0.3931f, 0.3931f)
                .AddProp("未烧尽的残页", Chapter2Folder + "未烧尽的残页.png", new Rect(0.56f, 0.16f, 0.62f, 0.27f), 1f, false, null, "c2_page_read", true, 0f, 0.3931f, 0.3931f)
                .AddProp("灯影下未烧尽的残页", Chapter2Folder + "灯影下未烧尽的残页.png", new Rect(0.55f, 0.15f, 0.64f, 0.29f), 1f, true, null, "c2_page_read", true, 0f, 0.3931f, 0.3931f)
                .Add("火钳", new Rect(0.64f, 0.20f, 0.74f, 0.28f), delegate { Take("火钳", "c2_tongs", "你拿到了火钳。"); }, null, "c2_tongs")
                .Add("炉灰铲", new Rect(0.45f, 0.25f, 0.53f, 0.34f), delegate { Take("炉灰铲", "c2_shovel", "你拿到了炉灰铲。"); }, null, "c2_shovel")
                .Add("焚纸炉", new Rect(0.50f, 0.30f, 0.76f, 0.60f), delegate
                {
                    if (!lightView)
                    {
                        Say("火炉太烫，不能直接拾取。也许灯影视角能看清炉灰里的东西。");
                        return;
                    }

                    if (!NeedItem("炉灰铲", "需要炉灰铲翻动灰烬。") || !NeedItem("火钳", "需要火钳夹出残页。"))
                    {
                        return;
                    }

                    Inspect("未烧尽的残页", "普通视角：\n“……祭名单……”\n“……封……”\n“……黑灯……”\n\n灯影视角：\n“缄灯祭名单其三。”\n“封名者，不入族谱。”\n“黑灯既燃，名与忆皆归灯中。”\n\n林照萤：封名……不是给死人用的。是为了让活着的人，也被忘掉。",
                        Chapter2Folder + "灯影下未烧尽的残页.png");
                    flags.Add("c2_page_read");
                    RebuildProps();
                    CompleteChapter("想知道名字去哪了，就去听那出无声戏。第二章结束，第三章「无声戏台」已解锁。");
                })
                .Add("焚纸炉拖拽解密", new Rect(0.50f, 0.30f, 0.76f, 0.60f), delegate { ShowFurnacePuzzle(); }, null, "c2_page_read"));
        }

        private void BuildChapter3()
        {
            chapters["chapter3"] = new ChapterDefinition("第三章 无声戏台", "c3_stage", "当前目标：调查无声戏台，找到陈望月替死的线索。");

            AddScene(new SceneDefinition("c3_stage", "chapter3", "戏台前场", Chapter3Folder, "戏台前场区域", null, "调查戏台，寻找“替死戏”的线索")
                .AddProp("陈望川", Chapter3Folder + "陈望川.png", new Rect(0.58f, 0.22f, 0.76f, 0.74f), 1f, false, null, "c3_stage_talk", true, 0f, 0.80524f, 0.80524f)
                .AddProp("戏文残页", Chapter3Folder + "戏文残页.png", new Rect(0.20f, 0.16f, 0.34f, 0.32f), 1f, false, null, "c3_script", true, 0f, 0.54038f, 0.54038f)
                .AddProp("鼓棒", Chapter3Folder + "鼓棒.png", new Rect(0.30f, 0.17f, 0.40f, 0.28f), 1f, false, null, "c3_script", true, 0f, 1f, 1f)
                .AddProp("地下通道入口", Chapter3Folder + "地下通道入口.png", new Rect(0.42f, 0.12f, 0.62f, 0.34f), 1f, false, "c3_tunnel_entrance_open")
                .Add("无声戏台", new Rect(0.12f, 0.62f, 0.34f, 0.78f), delegate
                {
                    SetFlag("c3_stage_checked", "无声戏台。戏牌破损，边缘像被水泡过。");
                }, null, "c3_stage_checked")
                .Add("陈望川对话", new Rect(0.60f, 0.30f, 0.74f, 0.70f), delegate
                {
                    Inspect("戏台前场",
                        "林照萤：这里就是无声戏台？\n陈望川：以前镇上的祭祀，都要在这里唱一出戏。\n林照萤：现在为什么没人唱了？\n陈望川：因为最后一出戏，唱死了人。",
                        Chapter3Folder + "陈望川.png");
                    flags.Add("c3_stage_talk");
                    RebuildProps();
                }, null, "c3_stage_talk")
                .Add("戏文残页", new Rect(0.22f, 0.20f, 0.32f, 0.30f), delegate
                {
                    inventory.Add("鼓棒");
                    Take("戏文残页", "c3_script", "你在舞台中央前的楼梯旁获得了戏文残页和鼓棒。");
                    Inspect("戏文残页", "喜面入门，哀面开路，无名守灯。\n台上无人，锣鼓却像仍在等一声开场。", Chapter3Folder + "戏文残页.png");
                }, null, "c3_script")
                .Add("破锣", new Rect(0.30f, 0.17f, 0.40f, 0.28f), delegate
                {
                    if (!NeedItem("鼓棒", "虽然破旧了，但用手还是敲不响这个破锣。"))
                    {
                        return;
                    }
                    if (!inventory.Contains("戏台机关钥"))
                    {
                        Say("锣面开裂，像很久没人敲过。锣架被锁住了。");
                        return;
                    }

                    SetFlag("c3_tunnel_entrance_open", "破锣被敲响，舞台地板震动，地下通道入口出现在舞台中央。");
                    SetObjective("进入地下通道");
                })
                .Add("舞台背景板", new Rect(0.36f, 0.40f, 0.62f, 0.72f), delegate
                {
                    if (!NeedItem("鼓棒", "先在舞台楼梯附近找找可用的东西。"))
                    {
                        return;
                    }

                    SetFlag("c3_stage_words", "画上是一条河和一艘纸船。随后舞台中央浮现三个字：“面、锣、门。” 去后台寻找面具。");
                    SetObjective("去后台寻找面具");
                })
                );

            AddScene(new SceneDefinition("c3_backstage", "chapter3", "后台", Chapter3Folder, "后台区域", "后台区域灯影下的场景", "收集傩面，打开地下通道")
                .WithLightMessage("墙上出现提示：“先笑，后哭，最后忘了自己。”")
                .AddProp("傩面喜", Chapter3Folder + "傩面喜.png", new Rect(0.16f, 0.30f, 0.30f, 0.52f), 1f, false, null, "c3_mask_happy")
                .AddProp("傩面哀", Chapter3Folder + "傩面哀.png", new Rect(0.42f, 0.30f, 0.56f, 0.52f), 1f, false, null, "c3_mask_sad")
                .AddProp("傩面无名", Chapter3Folder + "傩面无名.png", new Rect(0.68f, 0.30f, 0.82f, 0.52f), 1f, false, null, "c3_mask_none")
                .AddProp("戏台机关钥", Chapter3Folder + "戏台机关钥.png", new Rect(0.46f, 0.48f, 0.56f, 0.62f), 1f, true, null, "c3_stage_key")
                .Add("傩面喜", new Rect(0.16f, 0.30f, 0.30f, 0.52f), delegate { Take("傩面喜", "c3_mask_happy", "你取下了傩面喜。"); }, null, "c3_mask_happy")
                .Add("傩面哀", new Rect(0.42f, 0.30f, 0.56f, 0.52f), delegate { Take("傩面哀", "c3_mask_sad", "你取下了傩面哀。"); }, null, "c3_mask_sad")
                .Add("傩面无名", new Rect(0.68f, 0.30f, 0.82f, 0.52f), delegate { Take("傩面无名", "c3_mask_none", "你取下了傩面无名。"); }, null, "c3_mask_none")
                .Add("傩面架", new Rect(0.34f, 0.54f, 0.66f, 0.78f), delegate { ShowMaskPuzzle(); })
                .AddLight("面具机关", new Rect(0.34f, 0.54f, 0.66f, 0.78f), delegate
                {
                    ShowMaskPuzzle();
                }));

            AddScene(new SceneDefinition("c3_tunnel", "chapter3", "地下通道", Chapter3Folder, "地下通道区域", "地下通道区域灯影下场景", "获得封名残纸，前往河岸")
                .WithLightMessage("墙面浮现大量被划掉的名字。其中一个名字只剩两个字：“望月。”\n林照萤：望月……这是一个人的名字？\n陈望川：别再念了。")
                .AddProp("封名残纸", Chapter3Folder + "普通视角下的名单碎片.png", new Rect(0.24f, 0.18f, 0.40f, 0.34f), 1f, false, null, "c3_name_paper", true, 0f, 0.56392f, 0.56392f)
                .Add("封名残纸", new Rect(0.27f, 0.21f, 0.37f, 0.31f), delegate { Take("封名残纸", "c3_name_paper", "你拾起地上的湿纸，获得封名残纸。"); }, null, "c3_name_paper")
                .Add("旧灯笼", new Rect(0.48f, 0.30f, 0.62f, 0.56f), delegate { Say("灯笼里没有灯芯，却有焦味。"); }));

            AddScene(new SceneDefinition("c3_bridge", "chapter3", "河岸断桥", Chapter3Folder, "河岸断桥", "河岸断桥区域灯影下的场景", "打开水闸，获得名单碎片")
                .WithLightMessage("桥下出现沉船残影。一个女孩站在船头，手里捧着黑灯。")
                .AddProp("水闸钥匙", Chapter3Folder + "戏台机关钥.png", new Rect(0.28f, 0.13f, 0.33f, 0.26f), 1f, false, null, "c3_sluice_key", true, 0f, 0.67002f, 0.67002f)
                .AddProp("水闸", Chapter3Folder + "水闸.png", new Rect(0.36f, 0.24f, 0.62f, 0.64f), 1f, false, null, null, true, 0f, 1.3536f, 1.3536f)
                .AddProp("名单碎片", Chapter3Folder + "普通视角下的名单碎片.png", new Rect(0.62f, 0.12f, 0.78f, 0.26f), 1f, false, "c3_sluice_open", "c3_list_piece", true, -49f, 0.33860f, 0.33860f)
                .AddProp("灯影名单碎片", Chapter3Folder + "灯影视角下的名单碎片.png", new Rect(0.63f, 0.12f, 0.80f, 0.27f), 1f, true, "c3_list_piece", null, true, -32f, 0.40250f, 0.40250f)
                .AddProp("陈望月残影", Chapter3Folder + "陈望月名牌.png", new Rect(0.18f, 0.26f, 0.31f, 0.74f), 0.78f, true, "c3_list_piece", null, true, 0f, 0.26801f, 0.26801f)
                .Add("水闸钥匙", new Rect(0.29f, 0.16f, 0.32f, 0.24f), delegate { Take("水闸钥匙", "c3_sluice_key", "你从水闸旁木桩上取下旧钥匙。"); }, null, "c3_sluice_key")
                .Add("水闸", new Rect(0.30f, 0.18f, 0.68f, 0.72f), delegate
                {
                    if (!NeedItem("水闸钥匙", "水闸锁死了，需要水闸钥匙。"))
                    {
                        return;
                    }
                    SetFlag("c3_sluice_open", "水位下降，可以去断桥区域了。");
                    SetObjective("进入断桥区域，寻找名单碎片");
                    Inspect("水闸", "水闸把手被旧钥匙打开，水声向下沉去。", Chapter3Folder + "水闸.png");
                })
                .Add("名单碎片", new Rect(0.67f, 0.16f, 0.73f, 0.22f), delegate
                {
                    if (!NeedFlag("c3_sluice_open", "水闸未开，暂时不能到断桥那边。"))
                    {
                        return;
                    }
                    Take("名单碎片", "c3_list_piece", "你在桥边淤泥中找到名单碎片。普通视角：“……月……”“……替灯……”");
                    Inspect("名单碎片", "普通视角：“……月……”“……替灯……”\n灯影视角：“望月。”“替灯入船。”", Chapter3Folder + "普通视角下的名单碎片.png");
                })
                .Add("石碑", new Rect(0.72f, 0.42f, 0.86f, 0.62f), delegate { Say("石碑上只剩“归渡”二字。"); })
                .AddLight("女孩残影", new Rect(0.20f, 0.34f, 0.29f, 0.64f), delegate
                {
                    if (!NeedItem("名单碎片", "先在断桥边找到名单碎片。"))
                    {
                        return;
                    }
                    SetFlag("c3_ghost_seen", "她在等自己的名字。屏幕提示：登上祭船。");
                    SetObjective("登上祭船");
                    Inspect("灯影名单碎片", "灯影下的碎片补全：望月。替灯入船。", Chapter3Folder + "灯影视角下的名单碎片.png");
                })
                );

            AddScene(new SceneDefinition("c3_boat", "chapter3", "祭船船舱", Chapter3Folder, "祭船船舱暗室区域", "祭船船舱暗示区域灯影下场景", "获得名牌，进入船舱暗室")
                .WithLightMessage("船舱里坐着几个无脸亡魂。屏幕文字显示：“把她的名字还回来。”")
                .AddProp("半块名牌", Chapter3Folder + "半块名牌.png", new Rect(0.76f, 0.22f, 0.86f, 0.38f), 1f, false, null, "c3_half_tag", true, 41f, 0.42980f, 0.42980f)
                .AddProp("陈望月名牌", Chapter3Folder + "陈望月名牌.png", new Rect(0.76f, 0.22f, 0.86f, 0.42f), 1f, true, null, "c3_full_tag", true, 0f, 0.26801f, 0.26801f)
                .Add("半块名牌", new Rect(0.74f, 0.20f, 0.88f, 0.42f), delegate
                {
                    Take("半块名牌", "c3_half_tag", "你在船舱里获得半块名牌。");
                    Inspect("半块名牌", "半块名牌边缘有新裂口，像是在等待另一半名字。", Chapter3Folder + "半块名牌.png");
                }, null, "c3_half_tag")
                .AddLight("补全名牌", new Rect(0.74f, 0.20f, 0.88f, 0.46f), delegate
                {
                    if (!HasItemOrFlag("半块名牌", "c3_half_tag"))
                    {
                        Say("先在船舱里找到半块名牌。");
                        return;
                    }
                    Take("陈望月名牌", "c3_full_tag", "桥灯照过半块名牌，名牌自动补全：陈望月。");
                    Inspect("陈望月名牌", "陈望月。她在等自己的名字。", Chapter3Folder + "陈望月名牌.png");
                    SetObjective("使用陈望月名牌进入船舱暗室");
                }, null, "c3_full_tag")
                );

            AddScene(new SceneDefinition("c3_darkroom", "chapter3", "船舱暗室", Chapter3Folder, "祭船船舱暗室区域", "祭船船舱暗示区域灯影下场景", "补全族谱上的名字")
                .WithLightMessage("残破族谱的空位上，墨迹反复吞吐着一个“陈”字。")
                .AddProp("望字块", Chapter3Folder + "望字块.png", new Rect(0.50f, 0.20f, 0.57f, 0.33f), 1f, false, null, "c3_wang", true, 0f, 0.48259f, 0.48259f)
                .AddProp("月字块", Chapter3Folder + "月字块.png", new Rect(0.68f, 0.21f, 0.76f, 0.35f), 1f, false, null, "c3_yue", true, 0f, 0.49912f, 0.49912f)
                .AddProp("破残族谱", Chapter3Folder + "破残族谱.png", new Rect(0.56f, 0.18f, 0.68f, 0.42f), 1f)
                .AddProp("陈望川残影", Chapter3Folder + "陈望川.png", new Rect(0.78f, 0.24f, 0.94f, 0.78f), 0.74f, false, "c3_name_restored", null, true, 0f, 0.81146f, 0.81146f)
                .Add("望字块", new Rect(0.52f, 0.23f, 0.56f, 0.30f), delegate { Take("望字块", "c3_wang", "你获得了望字块。"); }, null, "c3_wang")
                .Add("月字块", new Rect(0.71f, 0.24f, 0.75f, 0.32f), delegate { Take("月字块", "c3_yue", "你获得了月字块。"); }, null, "c3_yue")
                .Add("残破族谱", new Rect(0.54f, 0.16f, 0.70f, 0.44f), delegate
                {
                    if (!NeedItem("望字块", "族谱空位已有“陈”，还缺“望”。") || !NeedItem("月字块", "族谱空位已有“陈望”，还缺“月”。"))
                    {
                        return;
                    }

                    SetFlag("c3_name_restored", "你把“望”放在“陈”后，又把“月”放在“望”后。名字补全：陈望月。");
                    Inspect("破残族谱", "族谱空位中，陈望月三个字终于重新出现。", Chapter3Folder + "破残族谱.png");
                })
                .Add("族谱拖拽补名", new Rect(0.54f, 0.16f, 0.70f, 0.44f), delegate { ShowNamePuzzle(); }, null, "c3_name_restored")
                .Add("真相回忆", new Rect(0.76f, 0.28f, 0.96f, 0.76f), delegate
                {
                    if (!NeedFlag("c3_name_restored", "先补全族谱上的名字。"))
                    {
                        return;
                    }

                    Inspect("真相回忆",
                        "陈望月穿着戏服，被带上祭船。\n镇民点燃黑灯。\n有人从族谱上划掉她的名字。\n祭船入河。\n陈望川站在人群后，没有上前。\n\n林照萤：她是被选中的？\n陈望川：不是。\n林照萤：那她为什么会上船？\n陈望川：因为她替了别人。\n林照萤：替谁？\n陈望川：这就是最后被封起来的名字。\n陈望川：陈望月是我的妹妹。那年缄灯祭，她替别人上了祭船。\n林照萤：最后那个名字，在灯楼？\n陈望川：族谱室，黑灯台，封名册。答案都在那里。\n\n获得关键线索：族谱室。获得关键线索：黑灯台。获得道具：守灵灯钥。",
                        Chapter3Folder + "陈望川.png");
                    inventory.Add("守灵灯钥");
                    flags.Add("c3_truth_seen");
                    RefreshHud();
                    RebuildProps();
                    RebuildHotspots();
                })
                .Add("章节结尾", new Rect(0.78f, 0.18f, 0.96f, 0.44f), delegate
                {
                    if (!NeedFlag("c3_truth_seen", "先看完真相回忆。"))
                    {
                        return;
                    }
                    CompleteChapter("归名之人，须上灯楼。\n林照萤：我会把你的名字带回去。\n陈望川：那就去灯楼吧。但进了灯楼，就没有回头路了。\n第三章结束。最终章：无声灯楼 已解锁。");
                }));
        }

        private void BuildChapter4()
        {
            chapters["chapter4"] = new ChapterDefinition("第四章 无声灯楼", "c4_tower_gate", "归名之人，须上灯楼。当前目标：找到打开灯楼大门的方法。");

            AddScene(new SceneDefinition("c4_tower_gate", "chapter4", "灯楼外", Chapter4Folder, "灯楼外区域", "灯楼外区域灯影下场景", "找到打开灯楼大门的方法")
                .WithLightMessage("石碑背面浮现隐藏文字：“名字不归，门不开。”黑灯笼下方有一块发光木牌。")
                .AddProp("灯楼木牌", Chapter4Folder + "灯楼木牌.png", new Rect(0.40f, 0.14f, 0.52f, 0.36f), 1f, true, null, "c4_wood_tag", true, 0f, 0.40250f, 0.40250f)
                .Add("灯楼大门", new Rect(0.40f, 0.18f, 0.64f, 0.56f), delegate
                {
                    if (!NeedItem("灯楼木牌", "门缝里没有声音。也许灯影视角能找到开门用的东西。"))
                    {
                        return;
                    }

                    SetFlag("c4_gate_open", "灯楼木牌嵌入门上凹槽，大门无声打开。当前目标：进入族谱室。");
                    SetObjective("进入族谱室");
                })
                .Add("灯楼大门拖拽解密", new Rect(0.40f, 0.18f, 0.64f, 0.56f), delegate { ShowGatePuzzle(); }, null, "c4_gate_open")
                .Add("石碑", new Rect(0.18f, 0.30f, 0.34f, 0.54f), delegate { Say("无声灯楼。灯能渡魂，也能封魂。"); })
                .Add("红灯笼", new Rect(0.28f, 0.44f, 0.38f, 0.68f), delegate { Say("红灯已经熄灭。"); })
                .Add("黑灯笼", new Rect(0.66f, 0.42f, 0.78f, 0.68f), delegate { Say("黑灯里似乎藏着什么。"); })
                .AddLight("灯楼木牌", new Rect(0.43f, 0.20f, 0.49f, 0.30f), delegate
                {
                    Take("灯楼木牌", "c4_wood_tag", "获得道具：灯楼木牌。当前目标：进入族谱室。");
                    SetObjective("使用灯楼木牌打开灯楼大门");
                    Inspect("灯楼木牌", "木牌背面刻着一行小字：名字不归，门不开。", Chapter4Folder + "灯楼木牌.png");
                }, null, "c4_wood_tag")
                .Add("进入族谱室", new Rect(0.78f, 0.16f, 0.98f, 0.50f), delegate
                {
                    if (!NeedFlag("c4_gate_open", "灯楼大门还没有打开。"))
                    {
                        return;
                    }

                    GoToScene("c4_genealogy");
                    Dialogue("族谱室",
                        "林照萤：这里就是族谱室？\n陈望川：嗯。名字从族谱上消失后，会被送进灯里。\n林照萤：所以答案不在纸上？\n陈望川：在灯里。",
                        Chapter4Folder + "陈望川角色立绘.png");
                }, "c4_gate_open"));

            AddScene(new SceneDefinition("c4_genealogy", "chapter4", "族谱室", Chapter4Folder, "族谱室", "族谱室灯影下的场景", "点亮三盏灯，查看封名真相")
                .WithLightMessage("三盏灯上方浮现三段文字：“陈望月”“替灯入船”“林照萤”。先点灯，再点对应文字。")
                .AddProp("族谱", Chapter4Folder + "族谱.png", new Rect(0.42f, 0.28f, 0.62f, 0.72f), 1f, false, null, "c4_truth_genealogy")
                .AddProp("打开的族谱", Chapter4Folder + "打开的族谱.png", new Rect(0.35f, 0.28f, 0.69f, 0.72f), 1f, false, "c4_truth_genealogy")
                .AddProp("未点亮的红灯", Chapter4Folder + "未点亮的红灯.png", new Rect(0.23f, 0.20f, 0.38f, 0.52f), 1f, false, null, "c4_red_lit")
                .AddProp("点亮的红灯", Chapter4Folder + "点亮的红灯.png", new Rect(0.23f, 0.20f, 0.38f, 0.52f), 1f, false, "c4_red_lit")
                .AddProp("未点亮的白灯", Chapter4Folder + "未点亮的白灯.png", new Rect(0.43f, 0.15f, 0.58f, 0.47f), 1f, false, null, "c4_white_lit")
                .AddProp("点亮的白灯", Chapter4Folder + "点亮的白灯.png", new Rect(0.43f, 0.15f, 0.58f, 0.47f), 1f, false, "c4_white_lit")
                .AddProp("未点亮的黑灯", Chapter4Folder + "未点亮的黑灯.png", new Rect(0.63f, 0.20f, 0.80f, 0.54f), 1f, false, null, "c4_black_lit")
                .AddProp("点亮的黑灯", Chapter4Folder + "点亮的黑灯.png", new Rect(0.63f, 0.20f, 0.80f, 0.54f), 1f, false, "c4_black_lit")
                .Add("合上的族谱", new Rect(0.40f, 0.28f, 0.64f, 0.72f), delegate
                {
                    if (!NeedFlag("c4_all_lit", "族谱被锁住了，似乎需要先点亮前方的灯。"))
                    {
                        return;
                    }

                    Take("陈望月替灯入船", "c4_truth_genealogy", "获得关键线索：陈望月替灯入船。获得关键线索：林照萤曾被封名。当前目标：前往黑灯台。");
                    Inspect("打开的族谱", "陈望月，替灯入船。\n林照萤，封名藏忆。", Chapter4Folder + "打开的族谱.png");
                    Dialogue("族谱室真相",
                        "林照萤：红灯引她入祭，白灯送她归河。\n陈望川：陈望月替别人上了祭船。\n林照萤：黑灯封住的，是我的名字？\n陈望川：是。那年本该上船的人，是你。\n林照萤：所以陈望月替了我。\n陈望川：嗯。\n林照萤：可我为什么什么都不记得？\n陈望川：沈掌灯人封住了你的名字，也封住了你的记忆。\n林照萤：外婆是为了让我活下去。\n陈望川：也是为了让你有一天回来，把这些名字重新看见。",
                        Chapter4Folder + "林照萤角色立绘.png");
                }, "c4_all_lit", "c4_truth_genealogy")
                .Add("红灯", new Rect(0.24f, 0.22f, 0.38f, 0.46f), delegate { Say("红灯：引人入祭。"); })
                .Add("白灯", new Rect(0.44f, 0.16f, 0.58f, 0.40f), delegate { Say("白灯：送魂归河。"); })
                .Add("黑灯", new Rect(0.62f, 0.22f, 0.78f, 0.48f), delegate { Say("黑灯：封名藏忆。"); })
                .AddLight("选红灯", new Rect(0.22f, 0.18f, 0.38f, 0.48f), delegate { SelectLamp("红灯"); }, null, "c4_red_lit")
                .AddLight("选白灯", new Rect(0.42f, 0.14f, 0.58f, 0.44f), delegate { SelectLamp("白灯"); }, null, "c4_white_lit")
                .AddLight("选黑灯", new Rect(0.62f, 0.18f, 0.80f, 0.48f), delegate { SelectLamp("黑灯"); }, null, "c4_black_lit")
                .AddLight("陈望月", new Rect(0.16f, 0.64f, 0.34f, 0.78f), delegate { PairLamp("陈望月", "白灯", "c4_white_lit", "白灯已点亮。"); })
                .AddLight("替灯入船", new Rect(0.40f, 0.64f, 0.60f, 0.78f), delegate { PairLamp("替灯入船", "红灯", "c4_red_lit", "红灯已点亮。"); })
                .AddLight("林照萤", new Rect(0.66f, 0.64f, 0.84f, 0.78f), delegate { PairLamp("林照萤", "黑灯", "c4_black_lit", "黑灯已点亮。"); })
                .AddLight("三灯拖拽解密", new Rect(0.16f, 0.14f, 0.84f, 0.78f), delegate { ShowLampPuzzle(); }, null, "c4_all_lit")
                .Add("去黑灯台", new Rect(0.80f, 0.18f, 0.98f, 0.52f), delegate
                {
                    if (!NeedFlag("c4_truth_genealogy", "先查看打开的族谱。"))
                    {
                        return;
                    }

                    Say("所有被封住的名字，最后都会来到这里。");
                    GoToScene("c4_black_lamp_stage");
                }, "c4_truth_genealogy"));

            AddScene(new SceneDefinition("c4_black_lamp_stage", "chapter4", "黑灯台", Chapter4Folder, "黑灯台区域 ", "黑灯台区域灯影下场景", "调查最终证据")
                .WithLightMessage("封名册最后一行浮现文字：“林照萤。”")
                .AddProp("黑灯", Chapter4Folder + "黑灯.png", new Rect(0.40f, 0.42f, 0.62f, 0.86f), 1f)
                .AddProp("封名册", Chapter4Folder + "封名册.png", new Rect(0.36f, 0.13f, 0.66f, 0.38f), 1f, false, null, "c4_book")
                .AddProp("普通视角下的封名册", Chapter4Folder + "普通视角下的封名册.png", new Rect(0.35f, 0.12f, 0.67f, 0.40f), 1f, false, "c4_book", "c4_truth_final")
                .AddProp("灯影下的封名册", Chapter4Folder + "灯影视角下的封名册.png", new Rect(0.35f, 0.12f, 0.67f, 0.40f), 1f, true, "c4_truth_final")
                .Add("巨大黑灯", new Rect(0.38f, 0.46f, 0.64f, 0.88f), delegate
                {
                    Inspect("黑灯", "黑灯中封着最后的名字。", Chapter4Folder + "黑灯.png");
                })
                .Add("封名册", new Rect(0.34f, 0.11f, 0.68f, 0.41f), delegate
                {
                    Take("封名册", "c4_book", "获得道具：封名册。普通视角只能看到：“陈望月，替灯入船。”“真正应入船者：……”");
                    Inspect("封名册", "陈望月，替灯入船。\n真正应入船者：……", Chapter4Folder + "普通视角下的封名册.png");
                    SetObjective("用灯影视角查看封名册");
                }, null, "c4_book")
                .AddLight("封名真相", new Rect(0.34f, 0.11f, 0.68f, 0.41f), delegate
                {
                    if (!NeedItem("封名册", "先拿到封名册。"))
                    {
                        return;
                    }

                    flags.Add("c4_final_truth");
                    Take("封名真相", "c4_truth_final", "获得关键线索：封名真相。当前目标：做出最终选择。");
                    Inspect("灯影下的封名册", "陈望月，替灯入船。\n真正应入船者：林照萤。", Chapter4Folder + "灯影视角下的封名册.png");
                    Dialogue("最终真相",
                        "林照萤：我的名字……\n陈望川：那年本该上祭船的人，是你。\n林照萤：所以陈望月替了我？\n陈望川：是。\n林照萤：为什么？\n陈望川：因为她知道，你如果上了船，就再也回不来了。\n林照萤：那我为什么什么都不记得？\n陈望川：沈掌灯人封住了你的名字，也封住了你的记忆。\n林照萤：外婆是为了保护我？\n陈望川：她想让你活下去，也想让你有一天能回来。\n林照萤：回来做什么？\n陈望川：把这些名字还给他们。\n林照萤：陈望月被忘了这么多年。\n陈望川：不是没人知道，是没人敢记得。\n林照萤：那就从现在开始记住。",
                        Chapter4Folder + "陈望川角色立绘.png");
                    SetObjective("点击黑灯台，做出最终选择");
                }, "c4_book", "c4_truth_final")
                .Add("最终选择", new Rect(0.34f, 0.11f, 0.70f, 0.42f), delegate
                {
                    if (!NeedFlag("c4_truth_final", "先用灯影视角查看封名册最后一行。"))
                    {
                        return;
                    }

                    ShowFinalChoice();
                }, "c4_truth_final")
                .Add("回族谱室", new Rect(0.02f, 0.20f, 0.18f, 0.52f), delegate { GoToScene("c4_genealogy"); }));
        }

        private void AddScene(SceneDefinition scene)
        {
            scenes[scene.Id] = scene;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            return image;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            return CreateImage(name, parent, color).rectTransform;
        }

        private static Text CreateText(string name, Transform parent, int size, TextAnchor anchor)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = LoadFont(UiFolder + "字魂镇魂手书(商用需授权).ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = size;
            text.alignment = anchor;
            text.color = new Color(0.94f, 0.88f, 0.76f, 1);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.82f);
            shadow.effectDistance = new Vector2(2f, -2f);
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.62f);
            outline.effectDistance = new Vector2(1f, -1f);
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, int size, UnityEngine.Events.UnityAction action, bool styled = true)
        {
            var button = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)).GetComponent<Button>();
            button.transform.SetParent(parent, false);
            var image = button.GetComponent<Image>();
            button.targetGraphic = image;
            if (styled)
            {
                image.sprite = LoadSprite(MainMenuFolder + "Button.png");
                image.type = Image.Type.Simple;
                image.color = image.sprite == null ? new Color(0.22f, 0.12f, 0.06f, 0.88f) : new Color(0.98f, 0.86f, 0.62f, 0.95f);
            }
            else
            {
                image.color = new Color(1f, 1f, 1f, 0.01f);
            }
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = styled ? (image.sprite == null ? new Color(0.22f, 0.12f, 0.06f, 0.88f) : Color.white) : new Color(1f, 1f, 1f, 0.01f);
            colors.highlightedColor = styled ? new Color(1f, 0.92f, 0.68f, 1f) : new Color(1f, 1f, 1f, 0.01f);
            colors.pressedColor = styled ? new Color(0.74f, 0.32f, 0.16f, 1f) : new Color(1f, 1f, 1f, 0.01f);
            colors.disabledColor = styled ? new Color(0.24f, 0.22f, 0.20f, 0.44f) : new Color(1f, 1f, 1f, 0.01f);
            colors.fadeDuration = 0.10f;
            button.colors = colors;
            button.onClick.AddListener(action);

            var text = CreateText("Label", button.transform, size, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            text.text = label;
            text.color = new Color(0.93f, 0.82f, 0.62f, 1f);
            text.raycastTarget = false;
            if (styled)
            {
                var outline = button.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.84f, 0.54f, 0.24f, 0.72f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
                ConfigureButtonFeedback(button, image, outline);
            }
            return button;
        }

        private static void ConfigureButtonFeedback(Button button, Image image, Outline outline)
        {
            var rect = button.GetComponent<RectTransform>();
            var normalColor = image.color;
            var hoverColor = image.sprite == null ? new Color(0.34f, 0.22f, 0.08f, 0.56f) : new Color(1f, 0.96f, 0.72f, 0.95f);
            var normalOutline = outline.effectColor;
            var hoverOutline = new Color(1f, 0.78f, 0.30f, 0.92f);
            var normalScale = Vector3.one;
            var hoverScale = new Vector3(1.018f, 1.018f, 1f);

            var trigger = button.gameObject.AddComponent<EventTrigger>();
            AddTrigger(trigger, EventTriggerType.PointerEnter, delegate
            {
                image.color = hoverColor;
                outline.effectColor = hoverOutline;
                rect.localScale = hoverScale;
            });
            AddTrigger(trigger, EventTriggerType.PointerExit, delegate
            {
                image.color = normalColor;
                outline.effectColor = normalOutline;
                rect.localScale = normalScale;
            });
            AddTrigger(trigger, EventTriggerType.PointerDown, delegate
            {
                rect.localScale = Vector3.one;
            });
            AddTrigger(trigger, EventTriggerType.PointerUp, delegate
            {
                rect.localScale = hoverScale;
            });
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }

        private float GetHotspotRingSize(HotspotDefinition hotspot)
        {
            var width = Mathf.Abs(hotspot.Area.width - hotspot.Area.x) * sceneWidth;
            var height = Mathf.Abs(hotspot.Area.height - hotspot.Area.y) * sceneHeight;
            return Mathf.Clamp(Mathf.Min(width, height) * 0.85f, 92f, 260f);
        }

        private static bool IsNavigationHotspot(HotspotDefinition hotspot)
        {
            if (hotspot == null || string.IsNullOrEmpty(hotspot.Label))
            {
                return false;
            }

            return hotspot.Label.StartsWith("去", StringComparison.Ordinal)
                || hotspot.Label.StartsWith("回", StringComparison.Ordinal)
                || hotspot.Label.StartsWith("进入", StringComparison.Ordinal)
                || hotspot.Label.Contains("选择");
        }

        private static Sprite CreateRingSprite(int size, float innerRadius, float outerRadius, Color color, bool dashed = false)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            var center = (size - 1) * 0.5f;
            var pixels = new Color[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center) / center;
                    var dy = (y - center) / center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var edgeIn = Mathf.SmoothStep(innerRadius - 0.015f, innerRadius + 0.015f, distance);
                    var edgeOut = 1f - Mathf.SmoothStep(outerRadius - 0.015f, outerRadius + 0.015f, distance);
                    var alpha = Mathf.Clamp01(edgeIn * edgeOut);
                    var angle = Mathf.Atan2(dy, dx);
                    var markings = Mathf.Pow(Mathf.Abs(Mathf.Sin(angle * 12f)), 18f) * 0.45f;
                    if (dashed)
                    {
                        var dash = Mathf.SmoothStep(0.15f, 0.40f, Mathf.Abs(Mathf.Sin(angle * 8f)));
                        alpha *= dash;
                    }
                    var c = color;
                    c.a *= Mathf.Clamp01(alpha + markings * alpha);
                    pixels[y * size + x] = c;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static void DecoratePanel(Image image, string spritePath, Color fallbackColor)
        {
            if (image == null)
            {
                return;
            }

            image.color = fallbackColor;
            if (string.IsNullOrEmpty(spritePath))
            {
                return;
            }

            var sprite = LoadSprite(spritePath);
            if (sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        private static void Stretch(RectTransform rect)
        {
            Anchor(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Sprite LoadSprite(string assetPath)
        {
#if UNITY_EDITOR
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                return sprite;
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            return texture == null ? null : SpriteFromTexture(texture);
#else
            if (!File.Exists(assetPath))
            {
                return null;
            }

            var texture = new Texture2D(2, 2);
            return texture.LoadImage(File.ReadAllBytes(assetPath)) ? SpriteFromTexture(texture) : null;
#endif
        }

        private static List<Sprite> LoadSprites(string assetPath)
        {
            var sprites = new List<Sprite>();
            if (assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                var sprite = LoadSprite(assetPath);
                if (sprite != null)
                {
                    sprites.Add(sprite);
                }
                return sprites;
            }

            if (!Directory.Exists(assetPath))
            {
                return sprites;
            }

            var files = Directory.GetFiles(assetPath, "*.png");
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < files.Length; i++)
            {
                var normalized = files[i].Replace('\\', '/');
                var assetIndex = normalized.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase);
                if (assetIndex >= 0)
                {
                    normalized = normalized.Substring(assetIndex);
                }

                var sprite = LoadSprite(normalized);
                if (sprite != null)
                {
                    sprites.Add(sprite);
                }
            }

            return sprites;
        }

        private static Sprite SpriteFromTexture(Texture2D texture)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100);
        }

        private static List<Sprite> LoadSpriteSheet(string assetPath, int columns, int rows, int frameCount, bool trimTransparent = false, bool clearBlackBackground = false)
        {
            var frames = new List<Sprite>();
            Texture2D texture = null;
            if (File.Exists(assetPath))
            {
                texture = new Texture2D(2, 2);
                if (!texture.LoadImage(File.ReadAllBytes(assetPath)))
                {
                    texture = null;
                }
            }
#if UNITY_EDITOR
            if (texture == null)
            {
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            }
#endif
            if (texture == null || columns <= 0 || rows <= 0 || frameCount <= 0)
            {
                return frames;
            }

            if (clearBlackBackground)
            {
                ClearBlackBackground(texture);
            }

            var cellWidth = texture.width / columns;
            var cellHeight = texture.height / rows;
            var total = Mathf.Min(frameCount, columns * rows);
            var sharedTrim = trimTransparent ? GetSharedTransparentTrim(texture, columns, cellWidth, cellHeight, total) : null;
            for (var i = 0; i < total; i++)
            {
                var column = i % columns;
                var rowFromTop = i / columns;
                var y = texture.height - (rowFromTop + 1) * cellHeight;
                var rect = new Rect(column * cellWidth, y, cellWidth, cellHeight);
                if (sharedTrim.HasValue)
                {
                    var trim = sharedTrim.Value;
                    rect = new Rect(rect.x + trim.x, rect.y + trim.y, trim.width, trim.height);
                }

                frames.Add(Sprite.Create(texture, rect, new Vector2(0.5f, 0f), 100));
            }

            return frames;
        }

        private static void ClearBlackBackground(Texture2D texture)
        {
            var pixels = texture.GetPixels32();
            for (var i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                if (pixel.r <= 6 && pixel.g <= 6 && pixel.b <= 6)
                {
                    pixel.a = 0;
                    pixels[i] = pixel;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
        }

        private static Rect? GetSharedTransparentTrim(Texture2D texture, int columns, int cellWidth, int cellHeight, int frameCount)
        {
            var minX = cellWidth;
            var minY = cellHeight;
            var maxX = -1;
            var maxY = -1;

            for (var i = 0; i < frameCount; i++)
            {
                var column = i % columns;
                var rowFromTop = i / columns;
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
                return null;
            }

            const int padding = 2;
            minX = Mathf.Max(0, minX - padding);
            minY = Mathf.Max(0, minY - padding);
            maxX = Mathf.Min(cellWidth - 1, maxX + padding);
            maxY = Mathf.Min(cellHeight - 1, maxY + padding);
            return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static Rect TrimTransparentPixels(Texture2D texture, Rect sourceRect)
        {
            var startX = Mathf.FloorToInt(sourceRect.x);
            var startY = Mathf.FloorToInt(sourceRect.y);
            var width = Mathf.FloorToInt(sourceRect.width);
            var height = Mathf.FloorToInt(sourceRect.height);
            var minX = width;
            var minY = height;
            var maxX = -1;
            var maxY = -1;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
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

            if (maxX < minX || maxY < minY)
            {
                return sourceRect;
            }

            const int padding = 2;
            minX = Mathf.Max(0, minX - padding);
            minY = Mathf.Max(0, minY - padding);
            maxX = Mathf.Min(width - 1, maxX + padding);
            maxY = Mathf.Min(height - 1, maxY + padding);
            return new Rect(startX + minX, startY + minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static Font LoadFont(string assetPath)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Font>(assetPath);
#else
            return null;
#endif
        }

        private static AudioClip LoadAudioClip(string assetPath)
        {
#if UNITY_EDITOR
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip != null)
            {
                return clip;
            }
#endif
            return Resources.Load<AudioClip>(Path.GetFileNameWithoutExtension(assetPath));
        }

        private sealed class ChapterDefinition
        {
            public ChapterDefinition(string title, string startScene, string startMessage)
            {
                Title = title;
                StartScene = startScene;
                StartMessage = startMessage;
            }

            public string Title { get; private set; }
            public string StartScene { get; private set; }
            public string StartMessage { get; private set; }
        }

        private sealed class SceneDefinition
        {
            public SceneDefinition(string id, string chapterId, string title, string folder, string image, string lightImage, string objective)
            {
                Id = id;
                ChapterId = chapterId;
                Title = title;
                Folder = folder;
                Image = image;
                LightImage = lightImage;
                Objective = objective;
            }

            public string Id { get; private set; }
            public string ChapterId { get; private set; }
            public string Title { get; private set; }
            public string Folder { get; private set; }
            public string Image { get; private set; }
            public string LightImage { get; private set; }
            public string Objective { get; private set; }
            public string LightMessage { get; private set; }
            public List<HotspotDefinition> Hotspots { get; private set; } = new List<HotspotDefinition>();
            public List<VfxDefinition> Effects { get; private set; } = new List<VfxDefinition>();
            public List<PropDefinition> Props { get; private set; } = new List<PropDefinition>();

            public SceneDefinition WithLightMessage(string message)
            {
                LightMessage = message;
                return this;
            }

            public SceneDefinition Add(string label, Rect area, Action action, string requiredFlag = null, string hiddenAfterFlag = null)
            {
                Hotspots.Add(new HotspotDefinition(label, area, action, false, requiredFlag, hiddenAfterFlag));
                return this;
            }

            public SceneDefinition AddLight(string label, Rect area, Action action, string requiredFlag = null, string hiddenAfterFlag = null)
            {
                Hotspots.Add(new HotspotDefinition(label, area, action, true, requiredFlag, hiddenAfterFlag));
                return this;
            }

            public SceneDefinition AddEffect(string name, string path, Rect area, float fps, float alpha, bool lightOnly = false, string requiredFlag = null, string hiddenAfterFlag = null, bool preserveAspect = true)
            {
                Effects.Add(new VfxDefinition(name, path, area, fps, alpha, lightOnly, requiredFlag, hiddenAfterFlag, preserveAspect));
                return this;
            }

            public SceneDefinition AddProp(string name, string path, Rect area, float alpha = 1f, bool lightOnly = false, string requiredFlag = null, string hiddenAfterFlag = null, bool preserveAspect = true, float rotationZ = 0f, float scaleX = 1f, float scaleY = 1f)
            {
                Props.Add(new PropDefinition(name, path, area, alpha, lightOnly, requiredFlag, hiddenAfterFlag, preserveAspect, rotationZ, scaleX, scaleY));
                return this;
            }
        }

        private sealed class HotspotDefinition
        {
            public HotspotDefinition(string label, Rect area, Action action, bool lightOnly = false, string requiredFlag = null, string hiddenAfterFlag = null)
            {
                Label = label;
                Area = area;
                Action = action;
                LightOnly = lightOnly;
                RequiredFlag = requiredFlag;
                HiddenAfterFlag = hiddenAfterFlag;
            }

            public string Label { get; private set; }
            public Rect Area { get; private set; }
            public Action Action { get; private set; }
            public bool LightOnly { get; private set; }
            public string RequiredFlag { get; private set; }
            public string HiddenAfterFlag { get; private set; }
        }

        private sealed class VfxDefinition
        {
            public VfxDefinition(string name, string path, Rect area, float fps, float alpha, bool lightOnly, string requiredFlag, string hiddenAfterFlag, bool preserveAspect)
            {
                Name = name;
                Path = path;
                Area = area;
                Fps = fps;
                Alpha = alpha;
                LightOnly = lightOnly;
                RequiredFlag = requiredFlag;
                HiddenAfterFlag = hiddenAfterFlag;
                PreserveAspect = preserveAspect;
            }

            public string Name { get; private set; }
            public string Path { get; private set; }
            public Rect Area { get; private set; }
            public float Fps { get; private set; }
            public float Alpha { get; private set; }
            public bool LightOnly { get; private set; }
            public string RequiredFlag { get; private set; }
            public string HiddenAfterFlag { get; private set; }
            public bool PreserveAspect { get; private set; }
        }

        private sealed class PropDefinition
        {
            public PropDefinition(string name, string path, Rect area, float alpha, bool lightOnly, string requiredFlag, string hiddenAfterFlag, bool preserveAspect, float rotationZ, float scaleX, float scaleY)
            {
                Name = name;
                Path = path;
                Area = area;
                Alpha = alpha;
                LightOnly = lightOnly;
                RequiredFlag = requiredFlag;
                HiddenAfterFlag = hiddenAfterFlag;
                PreserveAspect = preserveAspect;
                RotationZ = rotationZ;
                ScaleX = scaleX;
                ScaleY = scaleY;
            }

            public string Name { get; private set; }
            public string Path { get; private set; }
            public Rect Area { get; private set; }
            public float Alpha { get; private set; }
            public bool LightOnly { get; private set; }
            public string RequiredFlag { get; private set; }
            public string HiddenAfterFlag { get; private set; }
            public bool PreserveAspect { get; private set; }
            public float RotationZ { get; private set; }
            public float ScaleX { get; private set; }
            public float ScaleY { get; private set; }
        }

        private sealed class PuzzlePiece
        {
            public PuzzlePiece(string name, string spritePath)
            {
                Name = name;
                SpritePath = spritePath;
            }

            public string Name { get; private set; }
            public string SpritePath { get; private set; }
        }

        private sealed class GenericPuzzleDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            private ChapterGameBootstrap owner;
            private RectTransform rect;
            private CanvasGroup group;
            private string pieceName;
            private Vector2 homeAnchorMin;
            private Vector2 homeAnchorMax;
            private Vector2 homeOffsetMin;
            private Vector2 homeOffsetMax;

            public void Initialize(ChapterGameBootstrap owner, string pieceName, CanvasGroup group)
            {
                this.owner = owner;
                this.pieceName = pieceName;
                this.group = group;
                rect = GetComponent<RectTransform>();
                homeAnchorMin = rect.anchorMin;
                homeAnchorMax = rect.anchorMax;
                homeOffsetMin = rect.offsetMin;
                homeOffsetMax = rect.offsetMax;
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                transform.SetAsLastSibling();
                group.alpha = 0.82f;
                group.blocksRaycasts = false;
            }

            public void OnDrag(PointerEventData eventData)
            {
                rect.position = eventData.position;
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                group.alpha = 1f;
                group.blocksRaycasts = true;
                if (owner != null && owner.TryDropGenericPuzzlePiece(pieceName, eventData.position))
                {
                    return;
                }

                ReturnHome();
            }

            public void ReturnHome()
            {
                if (rect == null)
                {
                    return;
                }

                rect.anchorMin = homeAnchorMin;
                rect.anchorMax = homeAnchorMax;
                rect.offsetMin = homeOffsetMin;
                rect.offsetMax = homeOffsetMax;
            }
        }

        private sealed class MaskPuzzleDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            private ChapterGameBootstrap owner;
            private RectTransform rect;
            private CanvasGroup group;
            private string maskName;
            private Vector2 homeAnchorMin;
            private Vector2 homeAnchorMax;
            private Vector2 homeOffsetMin;
            private Vector2 homeOffsetMax;

            public void Initialize(ChapterGameBootstrap owner, string maskName, RectTransform parent, CanvasGroup group)
            {
                this.owner = owner;
                this.maskName = maskName;
                this.group = group;
                rect = GetComponent<RectTransform>();
                homeAnchorMin = rect.anchorMin;
                homeAnchorMax = rect.anchorMax;
                homeOffsetMin = rect.offsetMin;
                homeOffsetMax = rect.offsetMax;
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                transform.SetAsLastSibling();
                group.alpha = 0.82f;
                group.blocksRaycasts = false;
            }

            public void OnDrag(PointerEventData eventData)
            {
                rect.position = eventData.position;
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                group.alpha = 1f;
                group.blocksRaycasts = true;
                if (owner != null && owner.TryDropMask(maskName, eventData.position))
                {
                    return;
                }

                ReturnHome();
            }

            public void ReturnHome()
            {
                if (rect == null)
                {
                    return;
                }

                rect.anchorMin = homeAnchorMin;
                rect.anchorMax = homeAnchorMax;
                rect.offsetMin = homeOffsetMin;
                rect.offsetMax = homeOffsetMax;
            }
        }

        private sealed class RuntimeVfx
        {
            private readonly Image image;
            private readonly List<Sprite> frames;
            private readonly float frameDuration;
            private float time;
            private int frameIndex;

            public RuntimeVfx(Image image, List<Sprite> frames, float fps)
            {
                this.image = image;
                this.frames = frames;
                frameDuration = 1f / Mathf.Max(1f, fps);
                image.sprite = frames[0];
            }

            public void Tick(float deltaTime)
            {
                if (frames.Count <= 1 || image == null)
                {
                    return;
                }

                time += deltaTime;
                while (time >= frameDuration)
                {
                    time -= frameDuration;
                    frameIndex = (frameIndex + 1) % frames.Count;
                    image.sprite = frames[frameIndex];
                }
            }
        }

        private sealed class RuntimeHotspotEffect : MonoBehaviour
        {
            private readonly List<Image> rings = new List<Image>();
            private RectTransform rect;
            private RectTransform ringRoot;
            private Action onEnter;
            private Action onExit;
            private bool hovering;
            private float baseSize;
            private float alpha;
            private float clickFlash;

            public void Initialize(string label, float ringSize, bool navigation, Action enter, Action exit)
            {
                rect = GetComponent<RectTransform>();
                baseSize = ringSize;
                onEnter = enter;
                onExit = exit;

                ringRoot = new GameObject("Gold Hover Ring", typeof(RectTransform)).GetComponent<RectTransform>();
                ringRoot.SetParent(transform, false);
                ringRoot.anchorMin = new Vector2(0.5f, 0.5f);
                ringRoot.anchorMax = new Vector2(0.5f, 0.5f);
                ringRoot.pivot = new Vector2(0.5f, 0.5f);
                ringRoot.anchoredPosition = Vector2.zero;
                ringRoot.sizeDelta = new Vector2(baseSize, baseSize);
                ringRoot.localScale = Vector3.one * (navigation ? 0.82f : 1f);

                AddRing("Outer", 1.00f, 0.80f, new Color(1f, 0.72f, 0.25f, 0.72f));
                AddRing("Middle", 0.72f, 0.58f, new Color(1f, 0.86f, 0.46f, 0.52f));
                AddRing("Inner", 0.44f, 0.37f, new Color(1f, 0.68f, 0.22f, 0.38f));

                var trigger = gameObject.AddComponent<EventTrigger>();
                AddTrigger(trigger, EventTriggerType.PointerEnter, delegate
                {
                    hovering = true;
                    onEnter?.Invoke();
                });
                AddTrigger(trigger, EventTriggerType.PointerExit, delegate
                {
                    hovering = false;
                    onExit?.Invoke();
                });
                AddTrigger(trigger, EventTriggerType.PointerDown, delegate
                {
                    clickFlash = 1f;
                });
            }

            private void Update()
            {
                if (ringRoot == null)
                {
                    return;
                }

                clickFlash = Mathf.MoveTowards(clickFlash, 0f, Time.deltaTime * 5.5f);
                var targetAlpha = hovering ? 0.95f : 0.20f;
                alpha = Mathf.Lerp(alpha, targetAlpha, Time.deltaTime * 8f);

                var pulse = 1f + Mathf.Sin(Time.time * 2.2f) * 0.045f;
                var hoverScale = hovering ? 1.12f : 1f;
                var flashScale = Mathf.Lerp(1f, 0.72f, clickFlash);
                ringRoot.localScale = Vector3.one * pulse * hoverScale * flashScale;
                ringRoot.Rotate(0f, 0f, (hovering ? 56f : 24f) * Time.deltaTime);

                for (var i = 0; i < rings.Count; i++)
                {
                    var image = rings[i];
                    var c = image.color;
                    c.a = Mathf.Clamp01(alpha + clickFlash * 0.65f) * (i == 0 ? 0.95f : i == 1 ? 0.62f : 0.42f);
                    image.color = c;
                }

            }

            private void AddRing(string name, float outer, float inner, Color color)
            {
                var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(ringRoot, false);
                image.raycastTarget = false;
                image.sprite = CreateRingSprite(192, inner, outer, color);
                image.color = color;
                Stretch(image.rectTransform);
                rings.Add(image);
            }

        }
    }
}
