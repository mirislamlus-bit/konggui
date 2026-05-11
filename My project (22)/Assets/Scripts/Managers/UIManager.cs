using UnityEngine;
using UnityEngine.UI;

public sealed class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private GameObject lanternVisionOverlay;
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject chapterEndingPanel;
    [SerializeField] private Text qHintText;

    private void Awake()
    {
        Instance = this;
        ResolveReferences();
    }

    public void Configure(GameObject prompt, GameObject overlay, GameObject dialogue, GameObject inventory, GameObject ending, Text qHint)
    {
        interactionPrompt = prompt;
        lanternVisionOverlay = overlay;
        dialogueBox = dialogue;
        inventoryPanel = inventory;
        chapterEndingPanel = ending;
        qHintText = qHint;
    }

    public void ShowInteractionPrompt()
    {
        ResolveReferences();
        SetActiveIfAssigned(interactionPrompt, true);
    }

    public void HideInteractionPrompt()
    {
        ResolveReferences();
        SetActiveIfAssigned(interactionPrompt, false);
    }

    public void ShowQHint(string message)
    {
        ResolveReferences();
        if (qHintText != null)
        {
            qHintText.text = message;
            qHintText.gameObject.SetActive(true);
        }
    }

    public void HideQHint()
    {
        ResolveReferences();
        if (qHintText != null)
        {
            qHintText.gameObject.SetActive(false);
        }
    }

    private void ResolveReferences()
    {
        interactionPrompt = interactionPrompt != null ? interactionPrompt : GameObject.Find("InteractionPrompt");
        lanternVisionOverlay = lanternVisionOverlay != null ? lanternVisionOverlay : GameObject.Find("LanternVisionOverlay_UI");
        dialogueBox = dialogueBox != null ? dialogueBox : GameObject.Find("DialogueBox");
        inventoryPanel = inventoryPanel != null ? inventoryPanel : GameObject.Find("InventoryPanel");
        chapterEndingPanel = chapterEndingPanel != null ? chapterEndingPanel : GameObject.Find("ChapterEndingPanel");

        if (qHintText == null)
        {
            GameObject found = GameObject.Find("QHintText");
            qHintText = found != null ? found.GetComponent<Text>() : null;
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
