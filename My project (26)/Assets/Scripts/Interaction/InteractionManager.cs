using UnityEngine;
using UnityEngine.EventSystems;

namespace ChapterGame.Interaction
{
    public sealed class InteractionManager : MonoBehaviour
    {
        public static InteractionManager Instance { get; private set; }

        [Header("Input")]
        public Camera mainCamera;
        public LayerMask interactableLayer;
        public KeyCode interactKey = KeyCode.E;

        [Header("State")]
        public InteractableObject currentHover;
        public bool isShadowView;

        public bool IsShadowView
        {
            get { return isShadowView; }
        }

        public void SetShadowView(bool enabled)
        {
            isShadowView = enabled;
        }

        private void Awake()
        {
            Instance = this;
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            UpdateHover();

            if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(interactKey)) && currentHover != null)
            {
                currentHover.Interact(isShadowView);
            }
        }

        private void UpdateHover()
        {
            var nextHover = FindHoverObject();
            if (nextHover == currentHover)
            {
                return;
            }

            if (currentHover != null)
            {
                currentHover.HoverExit();
            }

            currentHover = nextHover;

            if (currentHover != null)
            {
                currentHover.HoverEnter();
            }
        }

        private InteractableObject FindHoverObject()
        {
            if (mainCamera == null)
            {
                return null;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return null;
            }

            var world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            var hit = Physics2D.Raycast(world, Vector2.zero, 0f, interactableLayer);
            if (hit.collider == null)
            {
                return null;
            }

            var interactable = hit.collider.GetComponentInParent<InteractableObject>();
            if (interactable == null || !interactable.CanInteract(isShadowView))
            {
                return null;
            }

            return interactable;
        }
    }
}
