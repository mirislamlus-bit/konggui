public interface IInteractable
{
    void Interact(PlayerController player);
}

public interface IInteractionPrompt
{
    string PromptText { get; }
    bool CanShowPrompt { get; }
}
