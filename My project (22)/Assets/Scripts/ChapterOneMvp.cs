using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JianDeng
{
    public enum ChapterScene
    {
        TownGate,
        StoneBridge,
        GrandmaHouse,
        MourningHall,
        OldWell
    }

    public enum Offering
    {
        Rice,
        Wine,
        Incense
    }

    public interface IInteractable
    {
        string Prompt { get; }
        void Interact(ChapterOneGame game);
    }

    public sealed class ChapterOneGame : MonoBehaviour
    {
        [Header("World")]
        public Transform player;
        public Camera mainCamera;
        public Text locationText;
        public Text promptText;
        public Text dialogueText;
        public Text inventoryText;
        public GameObject inventoryPanel;
        public GameObject lampShadowOverlay;
        public GameObject hiddenWellName;
        public GameObject blackLampFlame;
        public GameObject endingPanel;
        public Text endingText;

        [Header("Movement")]
        public float moveSpeed = 4.2f;
        public float leftBound = -7.6f;
        public float rightBound = 7.6f;

        private readonly Dictionary<ChapterScene, GameObject> sceneRoots = new Dictionary<ChapterScene, GameObject>();
        private readonly List<Offering> offeringOrder = new List<Offering>();
        private readonly List<string> inventory = new List<string>();
        private readonly List<IInteractable> nearbyInteractables = new List<IInteractable>();

        private ChapterScene currentScene = ChapterScene.TownGate;
        private bool lampShadow;
        private bool inventoryOpen;
        private bool hallSolved;
        private bool blackLampLit;
        private bool chapterEnded;
        private float dialogueTimer;

        public bool LampShadow => lampShadow;
        public bool HallSolved => hallSolved;
        public bool BlackLampLit => blackLampLit;
        public ChapterScene CurrentScene => currentScene;

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            foreach (SceneRoot root in FindObjectsOfType<SceneRoot>(true))
            {
                sceneRoots[root.scene] = root.gameObject;
            }
        }

        private void Start()
        {
            inventory.Add("半截火柴");
            inventory.Add("旧纸条：米、酒、香");
            SwitchScene(ChapterScene.TownGate, new Vector3(-6.8f, -2.15f, 0f));
            SetDialogue("林照萤回到渡灯镇。镇口的灯牌没有风，却轻轻晃了一下。");
            RefreshInventory();
        }

        private void Update()
        {
            if (chapterEnded)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    endingPanel.SetActive(false);
                    chapterEnded = false;
                }
                return;
            }

            HandleMovement();

            if (Input.GetKeyDown(KeyCode.E))
            {
                TryInteract();
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                ToggleLampShadow();
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleInventory();
            }

            if (dialogueTimer > 0f)
            {
                dialogueTimer -= Time.deltaTime;
                if (dialogueTimer <= 0f && dialogueText != null)
                {
                    dialogueText.text = string.Empty;
                }
            }

            UpdatePrompt();
            FollowCamera();
        }

        private void HandleMovement()
        {
            float x = Input.GetAxisRaw("Horizontal");
            Vector3 position = player.position;
            position.x = Mathf.Clamp(position.x + x * moveSpeed * Time.deltaTime, leftBound, rightBound);
            player.position = position;

            if (Mathf.Abs(x) > 0.01f)
            {
                Vector3 scale = player.localScale;
                scale.x = Mathf.Abs(scale.x) * Mathf.Sign(x);
                player.localScale = scale;
            }
        }

        private void FollowCamera()
        {
            if (mainCamera == null)
            {
                return;
            }

            mainCamera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private void TryInteract()
        {
            IInteractable target = null;
            float bestDistance = float.MaxValue;

            for (int i = nearbyInteractables.Count - 1; i >= 0; i--)
            {
                Component component = nearbyInteractables[i] as Component;
                if (component == null || !component.gameObject.activeInHierarchy)
                {
                    nearbyInteractables.RemoveAt(i);
                    continue;
                }

                float distance = Vector2.Distance(player.position, component.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    target = nearbyInteractables[i];
                }
            }

            if (target == null)
            {
                SetDialogue("这里暂时没有可互动的东西。");
                return;
            }

            target.Interact(this);
        }

        private void ToggleLampShadow()
        {
            lampShadow = !lampShadow;
            if (lampShadowOverlay != null)
            {
                lampShadowOverlay.SetActive(lampShadow);
            }

            if (hiddenWellName != null)
            {
                hiddenWellName.SetActive(lampShadow && currentScene == ChapterScene.OldWell && blackLampLit);
            }

            SetDialogue(lampShadow ? "灯影视角开启：灰烬下浮出被抹去的字。" : "灯影视角关闭：镇子又恢复了沉默。");
        }

        private void ToggleInventory()
        {
            inventoryOpen = !inventoryOpen;
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(inventoryOpen);
            }
            RefreshInventory();
        }

        public void SwitchScene(ChapterScene nextScene, Vector3 spawnPosition)
        {
            currentScene = nextScene;
            foreach (KeyValuePair<ChapterScene, GameObject> pair in sceneRoots)
            {
                pair.Value.SetActive(pair.Key == currentScene);
            }

            nearbyInteractables.Clear();
            player.position = spawnPosition;

            if (locationText != null)
            {
                locationText.text = GetSceneTitle(currentScene);
            }

            if (hiddenWellName != null)
            {
                hiddenWellName.SetActive(lampShadow && currentScene == ChapterScene.OldWell && blackLampLit);
            }

            FollowCamera();
        }

        public void AddNearby(IInteractable interactable)
        {
            if (!nearbyInteractables.Contains(interactable))
            {
                nearbyInteractables.Add(interactable);
            }
        }

        public void RemoveNearby(IInteractable interactable)
        {
            nearbyInteractables.Remove(interactable);
        }

        public void AddOffering(Offering offering)
        {
            if (hallSolved)
            {
                SetDialogue("供桌上的三样供品已经归位，黑灯在等最后一簇火。");
                return;
            }

            offeringOrder.Add(offering);
            SetDialogue("你将" + GetOfferingName(offering) + "摆上供桌。当前顺序：" + BuildOfferingText());

            if (offeringOrder.Count >= 3)
            {
                bool correct = offeringOrder[0] == Offering.Rice &&
                               offeringOrder[1] == Offering.Wine &&
                               offeringOrder[2] == Offering.Incense;

                if (correct)
                {
                    hallSolved = true;
                    inventory.Add("黑灯");
                    RefreshInventory();
                    SetDialogue("米压亡名，酒渡归魂，香引灯影。供桌下弹出一盏黑灯。");
                }
                else
                {
                    offeringOrder.Clear();
                    SetDialogue("供品次序乱了。灵堂里的纸灰倒卷，供桌又恢复原样。");
                }
            }
        }

        public void LightBlackLamp()
        {
            if (!hallSolved)
            {
                SetDialogue("黑灯还没有出现。也许供桌上的顺序不对。");
                return;
            }

            if (blackLampLit)
            {
                SetDialogue("黑灯已经点燃，火光低得像一口井。");
                return;
            }

            blackLampLit = true;
            if (blackLampFlame != null)
            {
                blackLampFlame.SetActive(true);
            }

            inventory.Add("点燃的黑灯");
            RefreshInventory();
            SetDialogue("黑灯亮起，火不是暖黄，而是一点深红。老井那边传来水声。");
        }

        public void TryOldWellEnding()
        {
            if (!blackLampLit)
            {
                SetDialogue("井水黑得照不出人影。你需要先点燃黑灯。");
                return;
            }

            if (!lampShadow)
            {
                SetDialogue("井壁上似乎有字，但普通视角看不清。按 Q 切换灯影视角。");
                return;
            }

            if (endingPanel != null)
            {
                endingPanel.SetActive(true);
            }

            if (endingText != null)
            {
                endingText.text = "第一章 归镇 完\n\n井壁浮出你的名字：林照萤。\n旁边还有一行新刻的字：陈望月替死。\n\n按 Esc 关闭结尾面板。";
            }

            chapterEnded = true;
        }

        public void SetDialogue(string text, float duration = 5f)
        {
            if (dialogueText != null)
            {
                dialogueText.text = text;
                dialogueTimer = duration;
            }
        }

        private void RefreshInventory()
        {
            if (inventoryText == null)
            {
                return;
            }

            inventoryText.text = "背包\n\n" + string.Join("\n", inventory.ToArray()) + "\n\n线索：供品顺序为 米 -> 酒 -> 香";
        }

        private void UpdatePrompt()
        {
            if (promptText == null)
            {
                return;
            }

            IInteractable target = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < nearbyInteractables.Count; i++)
            {
                Component component = nearbyInteractables[i] as Component;
                if (component == null || !component.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float distance = Vector2.Distance(player.position, component.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    target = nearbyInteractables[i];
                }
            }

            promptText.text = target == null ? string.Empty : "E  " + target.Prompt;
        }

        private string BuildOfferingText()
        {
            string[] names = new string[offeringOrder.Count];
            for (int i = 0; i < offeringOrder.Count; i++)
            {
                names[i] = GetOfferingName(offeringOrder[i]);
            }
            return string.Join("、", names);
        }

        private static string GetSceneTitle(ChapterScene scene)
        {
            switch (scene)
            {
                case ChapterScene.TownGate: return "第一章：归镇 / 镇口";
                case ChapterScene.StoneBridge: return "第一章：归镇 / 石桥";
                case ChapterScene.GrandmaHouse: return "第一章：归镇 / 外婆家";
                case ChapterScene.MourningHall: return "第一章：归镇 / 灵堂";
                case ChapterScene.OldWell: return "第一章：归镇 / 老井";
                default: return "第一章：归镇";
            }
        }

        private static string GetOfferingName(Offering offering)
        {
            switch (offering)
            {
                case Offering.Rice: return "米";
                case Offering.Wine: return "酒";
                case Offering.Incense: return "香";
                default: return offering.ToString();
            }
        }
    }
}
