using UnityEngine;

public sealed class SceneBackgroundSet : MonoBehaviour
{
    public GameObject realityBackground;
    public GameObject lanternVisionBackground;
    public GameObject hiddenObjects;

    public void SetLanternVision(bool enabled)
    {
        if (realityBackground != null)
        {
            realityBackground.SetActive(!enabled);
        }

        if (lanternVisionBackground != null)
        {
            lanternVisionBackground.SetActive(enabled);
        }

        if (hiddenObjects != null)
        {
            hiddenObjects.SetActive(enabled);
        }

        HiddenInLanternView[] hiddenViews = FindObjectsOfType<HiddenInLanternView>(true);
        foreach (HiddenInLanternView hiddenView in hiddenViews)
        {
            hiddenView.SetLanternVision(enabled);
        }

        ApplyPuzzleGatedHiddenObjects(enabled);
    }

    private void ApplyPuzzleGatedHiddenObjects(bool lanternVisionEnabled)
    {
        if (hiddenObjects == null)
        {
            return;
        }

        bool oldWellSolved = GameStateManager.Instance != null && GameStateManager.Instance.oldWellPuzzleSolved;
        bool blackLanternLit = GameStateManager.Instance != null && GameStateManager.Instance.isBlackLanternLit;
        bool hasSeenNamedRiverLantern = GameStateManager.Instance != null && GameStateManager.Instance.hasSeenNamedRiverLantern;
        bool offeringPuzzleSolved = GameStateManager.Instance != null && GameStateManager.Instance.offeringPuzzleSolved;
        foreach (Transform child in hiddenObjects.GetComponentsInChildren<Transform>(true))
        {
            string objectName = child.name;
            if (objectName.Contains("NameInWellEffect") ||
                objectName.Contains("WaterReflection_Effect") ||
                objectName.Contains("WaterReflection_OldWell") ||
                objectName.Contains("Grandmother_Afterimage_OldWell") ||
                objectName.Contains("GrandmaAfterimage_OldWell"))
            {
                child.gameObject.SetActive(lanternVisionEnabled && oldWellSolved);
            }

            if (objectName.Contains("AfterimageFlash_MourningHall"))
            {
                child.gameObject.SetActive(lanternVisionEnabled && offeringPuzzleSolved);
            }
            else if (objectName.Contains("AfterimageFlash_OldWell"))
            {
                child.gameObject.SetActive(lanternVisionEnabled && oldWellSolved);
            }
            else if (objectName.Contains("AfterimageFlash") && !oldWellSolved)
            {
                child.gameObject.SetActive(false);
            }

            if (objectName.Contains("GrandmaAfterimage_MourningHall"))
            {
                child.gameObject.SetActive(lanternVisionEnabled && offeringPuzzleSolved);
            }

            if (objectName.Contains("RiverLantern_Named"))
            {
                child.gameObject.SetActive(lanternVisionEnabled && blackLanternLit);
            }

            if (objectName.Contains("RiverLantern_Ghost") ||
                objectName.Contains("WaterReflection_LanternOnly"))
            {
                child.gameObject.SetActive(lanternVisionEnabled && blackLanternLit && hasSeenNamedRiverLantern);
            }
        }
    }
}
