using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class OldWellPuzzle : MonoBehaviour, IInteractable, IInteractionPrompt
{
    [SerializeField] private GameObject nameInWellEffect;
    [SerializeField] private GameObject waterReflectionEffect;
    [SerializeField] private GameObject grandmaAfterimage;
    [SerializeField] private GameObject afterimageFlash;
    [SerializeField] private GameObject chapterEndingRoot;
    [SerializeField] private Text chapterEndingText;
    [SerializeField] private string endingText = "\u7b2c\u4e00\u7ae0\u7ed3\u675f\uff1a\u5f52\u9547";
    [SerializeField] private string fallbackNextChapterText = "";
    [SerializeField] private string nextChapterSceneName = "";

    public string PromptText => "[E] \u67e5\u770b\u8001\u4e95";
    public bool CanShowPrompt => true;

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
        bool lanternVision = state.isLanternVision ||
            (lampView != null && lampView.IsLampViewEnabled && lampView.CurrentState == LampViewController.LampViewState.Full);

        if (!state.isBlackLanternLit)
        {
            DialogueManager.Show("\u9700\u8981\u70b9\u71c3\u9ed1\u706f\u3002");
            return;
        }

        if (!lanternVision)
        {
            DialogueManager.Show("\u4e5f\u8bb8\u8981\u7528\u9ed1\u706f\u7167\u4e00\u7167\u3002");
            return;
        }

        if (!state.hasSeenNamedRiverLantern)
        {
            DialogueManager.Show("\u6211\u603b\u89c9\u5f97\uff0c\u8fd8\u6f0f\u770b\u4e86\u4ec0\u4e48\u3002");
            return;
        }

        Solve(state);
    }

    private void Solve(GameStateManager state)
    {
        if (state.chapterOneEndingTriggered)
        {
            ShowChapterEnding();
            return;
        }

        SetActiveIfAssigned(nameInWellEffect, true);
        SetActiveIfAssigned(waterReflectionEffect, true);
        SetActiveIfAssigned(grandmaAfterimage, true);
        SetActiveIfAssigned(afterimageFlash, true);

        state.chapterOneEndingTriggered = true;
        Debug.Log("[Chapter1State] chapterOneEndingTriggered = true");

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ShowDialogue(new[]
            {
                "\u6797\u7167\u8424\uff1a\u6c34\u9762\u4e0a\u6d6e\u51fa\u7684\u540d\u5b57\uff0c\u4e0d\u6b62\u4e00\u4e2a\u3002",
                "\u6797\u7167\u8424\uff1a\u8fd9\u4e9b\u540d\u5b57\u2026\u2026\u4e3a\u4ec0\u4e48\u4f1a\u88ab\u85cf\u5728\u4e95\u91cc\uff1f",
                "\u5916\u5a46\u6b8b\u5f71\uff1a\u88ab\u5c01\u4f4f\u7684\uff0c\u4e0d\u662f\u4ea1\u9b42\u3002",
                "\u5916\u5a46\u6b8b\u5f71\uff1a\u662f\u771f\u76f8\u3002"
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

        if (chapterEndingText != null && !string.IsNullOrEmpty(fallbackNextChapterText))
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
