using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public sealed class SceneExit : MonoBehaviour, IInteractable, IInteractionPrompt
{
    public enum SceneExitRequirement
    {
        None,
        HasBlackLantern,
        IsBlackLanternLit,
        HasSeenNamedRiverLantern,
        HasCheckedNormalRiverLantern,
        OfferingPuzzleSolved
    }

    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnPointId;
    [SerializeField] private string promptText = "[E] 离开";
    [SerializeField] private SceneExitRequirement requiredState;
    [SerializeField] private bool invertRequirement;
    [SerializeField] private string blockedPromptText = "身后的路被雾遮住了。";
    [SerializeField] private bool blocked;

    public string PromptText => blocked ? blockedPromptText : promptText;
    public bool CanShowPrompt => true;

    public void Configure(
        string sceneName,
        string spawnPointId,
        string prompt,
        SceneExitRequirement requirement = SceneExitRequirement.None,
        bool invert = false,
        bool isBlocked = false,
        string blockedMessage = "")
    {
        targetSceneName = sceneName;
        targetSpawnPointId = spawnPointId;
        promptText = prompt;
        requiredState = requirement;
        invertRequirement = invert;
        blocked = isBlocked;
        if (!string.IsNullOrEmpty(blockedMessage))
        {
            blockedPromptText = blockedMessage;
        }

        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }

    public void Interact(PlayerController player)
    {
        if (blocked)
        {
            DialogueManager.Show(blockedPromptText);
            return;
        }

        if (!MeetsRequirement())
        {
            DialogueManager.Show(blockedPromptText);
            return;
        }

        LoadTargetScene();
    }

    private void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("SceneExit has no targetSceneName: " + name);
            return;
        }

        SceneTransition2D.SetPendingSpawnPoint(targetSpawnPointId);
        SceneManager.LoadScene(targetSceneName);
    }

    private bool MeetsRequirement()
    {
        GameStateManager state = GameStateManager.Instance;
        bool result = true;

        switch (requiredState)
        {
            case SceneExitRequirement.HasBlackLantern:
                result = state != null && state.hasBlackLantern;
                break;
            case SceneExitRequirement.IsBlackLanternLit:
                result = state != null && state.isBlackLanternLit;
                break;
            case SceneExitRequirement.HasSeenNamedRiverLantern:
                result = state != null && state.hasSeenNamedRiverLantern;
                break;
            case SceneExitRequirement.HasCheckedNormalRiverLantern:
                result = state != null && state.hasCheckedNormalRiverLantern;
                break;
            case SceneExitRequirement.OfferingPuzzleSolved:
                result = state != null && state.offeringPuzzleSolved;
                break;
        }

        return invertRequirement ? !result : result;
    }
}
