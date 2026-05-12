using UnityEngine;

public sealed class OfferingPuzzleTableInteractable : MonoBehaviour, IInteractable, IInteractionPrompt
{
    [SerializeField] private OfferingPuzzleManager puzzleManager;

    public string PromptText => "[E \u67e5\u770b\u4f9b\u684c]";
    public bool CanShowPrompt => true;

    public void Configure(OfferingPuzzleManager manager)
    {
        puzzleManager = manager;
    }

    public void Interact(PlayerController player)
    {
        if (puzzleManager == null)
        {
            puzzleManager = FindObjectOfType<OfferingPuzzleManager>();
        }

        if (puzzleManager == null)
        {
            Debug.LogWarning("[Chapter1Offering] OfferingPuzzleManager missing.");
            return;
        }

        puzzleManager.OpenPuzzleUi();
    }
}
