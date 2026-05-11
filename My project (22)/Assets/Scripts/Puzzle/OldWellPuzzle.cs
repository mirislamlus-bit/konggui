using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class OldWellPuzzle : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject nameInWellEffect;
    [SerializeField] private GameObject waterReflectionEffect;
    [SerializeField] private GameObject grandmaAfterimage;
    [SerializeField] private GameObject afterimageFlash;
    [SerializeField] private GameObject chapterEndingRoot;
    [SerializeField] private Text chapterEndingText;
    [SerializeField] private string endingText = "第一章结束：归镇";
    [SerializeField] private string fallbackNextChapterText = "下一章：纸马铺";
    [SerializeField] private string nextChapterSceneName = "Chapter2_PaperShop";

    private void Awake()
    {
        SetActiveIfAssigned(nameInWellEffect, false);
        SetActiveIfAssigned(waterReflectionEffect, false);
        SetActiveIfAssigned(grandmaAfterimage, false);
        SetActiveIfAssigned(afterimageFlash, false);
        SetActiveIfAssigned(chapterEndingRoot, false);
    }

    public void Configure(GameObject nameEffect, GameObject reflectionEffect, GameObject afterimage, GameObject flash, GameObject endingRoot, Text endingLabel)
    {
        nameInWellEffect = nameEffect != null ? nameEffect : nameInWellEffect;
        waterReflectionEffect = reflectionEffect != null ? reflectionEffect : waterReflectionEffect;
        grandmaAfterimage = afterimage != null ? afterimage : grandmaAfterimage;
        afterimageFlash = flash != null ? flash : afterimageFlash;
        chapterEndingRoot = endingRoot != null ? endingRoot : chapterEndingRoot;
        chapterEndingText = endingLabel != null ? endingLabel : chapterEndingText;

        SetActiveIfAssigned(nameInWellEffect, false);
        SetActiveIfAssigned(waterReflectionEffect, false);
        SetActiveIfAssigned(grandmaAfterimage, false);
        SetActiveIfAssigned(afterimageFlash, false);
        SetActiveIfAssigned(chapterEndingRoot, false);
    }

    public void Interact(PlayerController player)
    {
        GameStateManager state = GameStateManager.EnsureInstance();
        LampViewController lampView = FindObjectOfType<LampViewController>();

        if (!state.isBlackLanternLit)
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ShowDialogue(new[]
                {
                    "林照萤：井里太暗，什么也看不见。",
                    "需要点燃黑灯。"
                });
            }
            return;
        }

        if (lampView == null || !lampView.IsLampViewEnabled || lampView.CurrentState != LampViewController.LampViewState.Full)
        {
            DialogueManager.Show("林照萤：也许要用黑灯照一照。");
            return;
        }

        Solve(state);
    }

    private void Solve(GameStateManager state)
    {
        SetActiveIfAssigned(nameInWellEffect, true);
        SetActiveIfAssigned(waterReflectionEffect, true);
        SetActiveIfAssigned(grandmaAfterimage, true);
        SetActiveIfAssigned(afterimageFlash, true);

        state.hasCompletedWellEnding = true;

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ShowDialogue(new[]
            {
                "林照萤：水面上浮出的名字，不止一个。",
                "林照萤：这些名字……为什么会被藏在井里？",
                "外婆残影：被封住的，不是亡魂。",
                "外婆残影：是真相。"
            });
        }

        ShowChapterEnding();
    }

    private void ShowChapterEnding()
    {
        ResolveEndingUi();

        if (chapterEndingText != null)
        {
            chapterEndingText.text = endingText;
        }

        SetActiveIfAssigned(chapterEndingRoot, true);

        if (!string.IsNullOrEmpty(nextChapterSceneName) && Application.CanStreamedLevelBeLoaded(nextChapterSceneName))
        {
            SceneManager.LoadScene(nextChapterSceneName);
            return;
        }

        if (chapterEndingText != null)
        {
            chapterEndingText.text = endingText + "\n" + fallbackNextChapterText;
        }
    }

    private void ResolveEndingUi()
    {
        if (chapterEndingRoot == null)
        {
            chapterEndingRoot = GameObject.Find("ChapterEndingPanel");
        }

        if (chapterEndingText == null)
        {
            GameObject textObject = GameObject.Find("ChapterEndingText");
            chapterEndingText = textObject != null ? textObject.GetComponent<Text>() : null;
        }
    }

    private static void SetActiveIfAssigned(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
