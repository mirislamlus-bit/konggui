using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public sealed class Interactable : MonoBehaviour, IInteractable, IInteractionPrompt
{
    [SerializeField] private string promptText = "[E] 互动";
    [SerializeField] private string[] normalDialogue;
    [SerializeField] private string[] weakLampDialogue;
    [SerializeField] private string[] fullLampDialogue;
    [SerializeField] private bool canRepeat = true;
    [SerializeField] private bool requiresCondition;
    [SerializeField] private string blockedMessage;
    [SerializeField] private UnityEvent onInteract;

    private bool hasInteracted;
    private Func<bool> condition;
    private Action<PlayerController> runtimeAction;

    public string PromptText => promptText;
    public bool CanShowPrompt => !requiresCondition || condition == null || condition();

    public void Configure(string prompt, Action<PlayerController> action, bool repeat = true, Func<bool> canInteract = null)
    {
        promptText = prompt;
        runtimeAction = action;
        canRepeat = repeat;
        condition = canInteract;
        requiresCondition = canInteract != null;

        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }

    public void SetDialogues(string[] normalLines, string[] weakLines = null, string[] fullLines = null)
    {
        normalDialogue = normalLines;
        weakLampDialogue = weakLines;
        fullLampDialogue = fullLines;
    }

    public void SetBlockedMessage(string message)
    {
        blockedMessage = message;
    }

    public void Interact(PlayerController player)
    {
        if (!canRepeat && hasInteracted)
        {
            return;
        }

        if (requiresCondition && condition != null && !condition())
        {
            if (!string.IsNullOrEmpty(blockedMessage))
            {
                DialogueManager.Show(blockedMessage);
            }
            return;
        }

        hasInteracted = true;
        ShowConfiguredDialogue();
        runtimeAction?.Invoke(player);
        onInteract?.Invoke();
    }

    private void ShowConfiguredDialogue()
    {
        string[] lines = ResolveDialogueForCurrentState();
        if (lines != null && lines.Length > 0 && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ShowDialogue(lines);
        }
    }

    private string[] ResolveDialogueForCurrentState()
    {
        LampViewController controller = FindObjectOfType<LampViewController>();
        LampViewController.LampViewState state = controller != null ? controller.CurrentState : LampViewController.LampViewState.None;

        if (state == LampViewController.LampViewState.Full && fullLampDialogue != null && fullLampDialogue.Length > 0)
        {
            return fullLampDialogue;
        }

        if (state == LampViewController.LampViewState.Weak && weakLampDialogue != null && weakLampDialogue.Length > 0)
        {
            return weakLampDialogue;
        }

        return normalDialogue;
    }
}
