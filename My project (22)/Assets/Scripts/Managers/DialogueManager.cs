using UnityEngine;
using UnityEngine.UI;

public sealed class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [SerializeField] private Text dialogueText;
    [SerializeField] private Text speakerNameText;
    [SerializeField] private GameObject dialogueRoot;

    private string[] currentLines;
    private int currentIndex;

    public bool IsShowing => dialogueRoot != null && dialogueRoot.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ResolveReferences();
        CloseDialogue();
    }

    private void Update()
    {
        if (!IsShowing)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            ContinueDialogue();
        }
    }

    public static void Show(string line)
    {
        if (Instance != null)
        {
            Instance.ShowDialogue(new[] { line });
        }

        Debug.Log(line);
    }

    public void ShowDialogue(string[] lines)
    {
        if (lines == null || lines.Length == 0)
        {
            return;
        }

        currentLines = lines;
        currentIndex = 0;
        ShowCurrentLine();

        foreach (string line in lines)
        {
            Debug.Log(line);
        }
    }

    public void ContinueDialogue()
    {
        if (currentLines == null || currentLines.Length == 0)
        {
            CloseDialogue();
            return;
        }

        currentIndex++;
        if (currentIndex >= currentLines.Length)
        {
            CloseDialogue();
            return;
        }

        ShowCurrentLine();
    }

    public void CloseDialogue()
    {
        currentLines = null;
        currentIndex = 0;

        ResolveReferences();
        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = string.Empty;
        }

        if (dialogueRoot != null)
        {
            dialogueRoot.SetActive(false);
        }
    }

    private void ShowCurrentLine()
    {
        ResolveReferences();

        if (dialogueRoot != null)
        {
            dialogueRoot.SetActive(true);
        }

        if (dialogueText != null && currentLines != null && currentIndex < currentLines.Length)
        {
            SetDialogueText(currentLines[currentIndex]);
        }
    }

    private void SetDialogueText(string line)
    {
        int separator = line.IndexOf('\uff1a');
        if (separator < 0)
        {
            separator = line.IndexOf(':');
        }

        if (separator > 0)
        {
            if (speakerNameText != null)
            {
                speakerNameText.text = line.Substring(0, separator);
            }

            dialogueText.text = line.Substring(separator + 1);
            return;
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = string.Empty;
        }

        dialogueText.text = line;
    }

    public void ResolveReferences()
    {
        dialogueRoot = FindObjectIncludingInactive("DialogueBox");

        GameObject textObject = FindObjectIncludingInactive("DialogueText");
        dialogueText = textObject != null ? textObject.GetComponent<Text>() : null;

        GameObject nameObject = FindObjectIncludingInactive("DialogueNameText");
        speakerNameText = nameObject != null ? nameObject.GetComponent<Text>() : null;
    }

    private static GameObject FindObjectIncludingInactive(string objectName)
    {
        foreach (GameObject item in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (item.name == objectName && item.hideFlags == HideFlags.None && item.scene.IsValid())
            {
                return item;
            }
        }

        return null;
    }
}
