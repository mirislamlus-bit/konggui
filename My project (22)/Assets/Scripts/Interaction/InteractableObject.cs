using UnityEngine;

public sealed class InteractableObject : MonoBehaviour, IInteractable
{
    public string interactionId;
    [TextArea] public string[] dialogueLines;

    public void Interact(PlayerController player)
    {
        Debug.Log("Interact triggered: " + interactionId);

        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            return;
        }

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ShowDialogue(dialogueLines);
            return;
        }

        foreach (string line in dialogueLines)
        {
            Debug.Log(line);
        }
    }
}
