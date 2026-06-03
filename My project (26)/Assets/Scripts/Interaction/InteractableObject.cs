using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace ChapterGame.Interaction
{
    public enum InteractType
    {
        Inspect,
        Pickup,
        Enter,
        Talk,
        Trigger
    }

    [RequireComponent(typeof(Collider2D))]
    public sealed class InteractableObject : MonoBehaviour
    {
        [Header("Interaction")]
        public string interactName;
        public InteractType interactType = InteractType.Inspect;
        public bool requireShadowView;
        public bool disableAfterInteract;
        public UnityEvent onInteract;

        [Header("Optional")]
        public HoverHighlight hoverHighlight;

        private bool hasInteracted;
        private Collider2D cachedCollider;

        public bool CanInteract(bool isShadowView)
        {
            return isActiveAndEnabled
                && !hasInteracted
                && (!requireShadowView || isShadowView);
        }

        public void Interact(bool isShadowView)
        {
            if (!CanInteract(isShadowView))
            {
                return;
            }

            StartCoroutine(InteractRoutine());
        }

        public void Interact()
        {
            var manager = InteractionManager.Instance;
            Interact(manager != null && manager.IsShadowView);
        }

        public void HoverEnter()
        {
            EnsureHighlight();
            if (hoverHighlight != null)
            {
                hoverHighlight.HoverEnter();
            }
        }

        public void HoverExit()
        {
            EnsureHighlight();
            if (hoverHighlight != null)
            {
                hoverHighlight.HoverExit();
            }
        }

        private void Awake()
        {
            cachedCollider = GetComponent<Collider2D>();
            EnsureHighlight();
        }

        private IEnumerator InteractRoutine()
        {
            hasInteracted = true;

            EnsureHighlight();
            if (hoverHighlight != null)
            {
                yield return hoverHighlight.PlayInteractFeedback();
            }

            onInteract.Invoke();

            if (disableAfterInteract)
            {
                if (cachedCollider != null)
                {
                    cachedCollider.enabled = false;
                }

                gameObject.SetActive(false);
            }
            else
            {
                hasInteracted = false;
            }
        }

        private void EnsureHighlight()
        {
            if (hoverHighlight == null)
            {
                hoverHighlight = GetComponent<HoverHighlight>();
            }
        }
    }
}
