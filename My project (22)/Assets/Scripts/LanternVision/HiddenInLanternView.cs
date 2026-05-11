using UnityEngine;

public sealed class HiddenInLanternView : MonoBehaviour
{
    [SerializeField] private bool visibleOnlyInLanternView = true;

    private void Awake()
    {
        SetLanternVision(false);
    }

    public void SetLanternVision(bool enabled)
    {
        gameObject.SetActive(!visibleOnlyInLanternView || enabled);
    }
}
