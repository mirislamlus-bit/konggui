using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class InteractionDetector : MonoBehaviour
{
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private Text promptText;
    [SerializeField] private string promptMessage = "[E] \u4e92\u52a8";
    [SerializeField] private Vector3 worldPromptOffset = new Vector3(0f, 1.15f, 0f);

    private readonly List<Collider2D> nearbyColliders = new List<Collider2D>();
    private PlayerController player;
    private IInteractable currentInteractable;
    private Collider2D currentCollider;
    private RectTransform promptRectTransform;
    private Canvas promptCanvas;
    private Camera cachedCamera;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        if (player == null)
        {
            player = GetComponentInParent<PlayerController>();
        }

        ResolvePrompt();
        HidePrompt();
    }

    private void Update()
    {
        RefreshCurrentInteractable();
        UpdatePromptPosition();

        if (!Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsShowing)
        {
            DialogueManager.Instance.ContinueDialogue();
            return;
        }

        if (currentInteractable == null)
        {
            return;
        }

        IInteractionPrompt prompt = currentInteractable as IInteractionPrompt;
        if (prompt != null && !prompt.CanShowPrompt)
        {
            return;
        }

        currentInteractable.Interact(player);
        RefreshCurrentInteractable();
        UpdatePromptPosition();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (FindInteractable(other) == null)
        {
            return;
        }

        if (!nearbyColliders.Contains(other))
        {
            nearbyColliders.Add(other);
        }

        RefreshCurrentInteractable();
        UpdatePromptPosition();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        nearbyColliders.Remove(other);
        if (currentCollider == other)
        {
            currentCollider = null;
            currentInteractable = null;
        }

        RefreshCurrentInteractable();
        UpdatePromptPosition();
    }

    private void RefreshCurrentInteractable()
    {
        PruneInvalidColliders();

        Collider2D bestCollider = null;
        IInteractable bestInteractable = null;
        float bestDistance = float.MaxValue;

        foreach (Collider2D candidate in nearbyColliders)
        {
            IInteractable interactable = FindInteractable(candidate);
            if (interactable == null)
            {
                continue;
            }

            float distance = GetDistanceTo(candidate);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestCollider = candidate;
                bestInteractable = interactable;
            }
        }

        currentCollider = bestCollider;
        currentInteractable = bestInteractable;

        if (currentInteractable == null)
        {
            HidePrompt();
            return;
        }

        IInteractionPrompt prompt = currentInteractable as IInteractionPrompt;
        if (prompt != null && !prompt.CanShowPrompt)
        {
            HidePrompt();
            return;
        }

        ShowPrompt();
    }

    private void PruneInvalidColliders()
    {
        for (int i = nearbyColliders.Count - 1; i >= 0; i--)
        {
            Collider2D collider = nearbyColliders[i];
            if (collider == null || !collider.gameObject.activeInHierarchy)
            {
                nearbyColliders.RemoveAt(i);
            }
        }
    }

    private float GetDistanceTo(Collider2D candidate)
    {
        Transform origin = player != null ? player.transform : transform;
        Vector2 closestPoint = candidate.ClosestPoint(origin.position);
        return Vector2.Distance(origin.position, closestPoint);
    }

    private void UpdatePromptPosition()
    {
        if (promptRoot == null || promptRectTransform == null || currentCollider == null || currentInteractable == null)
        {
            return;
        }

        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
        }

        if (cachedCamera == null)
        {
            return;
        }

        Vector3 worldPosition = currentCollider.bounds.center + worldPromptOffset;
        Vector3 screenPosition = cachedCamera.WorldToScreenPoint(worldPosition);
        if (screenPosition.z < 0f)
        {
            promptRoot.SetActive(false);
            return;
        }

        promptRoot.SetActive(true);

        if (promptCanvas == null || promptCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            promptRectTransform.position = screenPosition;
            return;
        }

        RectTransform canvasRect = promptCanvas.transform as RectTransform;
        if (canvasRect == null)
        {
            promptRectTransform.position = screenPosition;
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            promptCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cachedCamera,
            out Vector2 localPoint);
        promptRectTransform.localPosition = localPoint;
    }

    private static IInteractable FindInteractable(Collider2D other)
    {
        if (other == null)
        {
            return null;
        }

        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null)
        {
            return interactable;
        }

        return other.GetComponentInParent<IInteractable>();
    }

    private void ShowPrompt()
    {
        ResolvePrompt();

        if (promptText != null)
        {
            promptText.text = GetPromptMessage();
        }

        if (promptRoot != null)
        {
            promptRoot.SetActive(true);
        }
    }

    private string GetPromptMessage()
    {
        IInteractionPrompt prompt = currentInteractable as IInteractionPrompt;
        if (prompt != null && prompt.CanShowPrompt && !string.IsNullOrEmpty(prompt.PromptText))
        {
            return prompt.PromptText;
        }

        return promptMessage;
    }

    private void HidePrompt()
    {
        ResolvePrompt();

        if (promptRoot != null)
        {
            promptRoot.SetActive(false);
        }
    }

    private void ResolvePrompt()
    {
        if (promptRoot == null)
        {
            promptRoot = FindObjectIncludingInactive("InteractionPrompt");
        }

        if (promptText == null && promptRoot != null)
        {
            promptText = promptRoot.GetComponentInChildren<Text>(true);
        }

        if (promptRectTransform == null && promptRoot != null)
        {
            promptRectTransform = promptRoot.GetComponent<RectTransform>();
        }

        if (promptCanvas == null && promptRoot != null)
        {
            promptCanvas = promptRoot.GetComponentInParent<Canvas>(true);
        }

        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
        }
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
