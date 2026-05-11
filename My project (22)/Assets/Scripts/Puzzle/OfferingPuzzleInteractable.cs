using UnityEngine;

public sealed class OfferingPuzzleInteractable : MonoBehaviour, IInteractable, IInteractionPrompt
{
    public enum OfferingInteractionMode
    {
        PickUp,
        Place
    }

    [SerializeField] private string offeringId;
    [SerializeField] private int slotIndex;
    [SerializeField] private OfferingInteractionMode interactionMode;
    [SerializeField] private OfferingPuzzleManager puzzleManager;
    [SerializeField] private GameObject visualToHideOnPickUp;
    [SerializeField] private GameObject visualToShowOnPlace;
    [SerializeField] private GameObject alternateInteractable;
    [SerializeField] private Sprite inventoryIcon;

    public string PromptText => interactionMode == OfferingInteractionMode.PickUp ? "[E] 拾取" : "[E] 放置";
    public bool CanShowPrompt => ResolveCanShowPrompt();

    public void Configure(
        string id,
        int slot,
        OfferingInteractionMode mode,
        OfferingPuzzleManager manager,
        GameObject hideOnPickUp = null,
        GameObject showOnPlace = null,
        Sprite icon = null)
    {
        offeringId = id;
        slotIndex = slot;
        interactionMode = mode;
        puzzleManager = manager;
        visualToHideOnPickUp = hideOnPickUp;
        visualToShowOnPlace = showOnPlace;
        inventoryIcon = icon;
    }

    public void Interact(PlayerController player)
    {
        if (puzzleManager == null)
        {
            puzzleManager = FindObjectOfType<OfferingPuzzleManager>();
        }

        if (puzzleManager == null)
        {
            return;
        }

        if (interactionMode == OfferingInteractionMode.PickUp)
        {
            if (puzzleManager.TryPickUpOffering(slotIndex, offeringId, inventoryIcon) && visualToHideOnPickUp != null)
            {
                visualToHideOnPickUp.SetActive(false);
                gameObject.SetActive(false);
                if (alternateInteractable != null)
                {
                    alternateInteractable.SetActive(true);
                }
            }
            return;
        }

        if (puzzleManager.TryPlaceOffering(slotIndex, offeringId) && visualToShowOnPlace != null)
        {
            visualToShowOnPlace.SetActive(true);
            gameObject.SetActive(false);
            if (alternateInteractable != null)
            {
                alternateInteractable.SetActive(true);
            }
        }
    }

    private bool ResolveCanShowPrompt()
    {
        if (puzzleManager == null)
        {
            puzzleManager = FindObjectOfType<OfferingPuzzleManager>();
        }

        if (puzzleManager == null)
        {
            return false;
        }

        if (interactionMode == OfferingInteractionMode.PickUp)
        {
            return puzzleManager.SlotHasOffering(slotIndex, offeringId) && puzzleManager.CanPickUpMore();
        }

        return puzzleManager.SlotIsEmpty(slotIndex) && puzzleManager.IsHoldingOffering(offeringId);
    }

    public void SetAlternateInteractable(GameObject target)
    {
        alternateInteractable = target;
    }
}
