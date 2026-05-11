using UnityEngine;

namespace JianDeng
{
    public sealed class SceneRoot : MonoBehaviour
    {
        public ChapterScene scene;
    }

    public sealed class InteractionZone : MonoBehaviour
    {
        private IInteractable interactable;

        private IInteractable Interactable
        {
            get
            {
                if (interactable == null)
                {
                    interactable = GetComponent<IInteractable>();
                }

                return interactable;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            ChapterOneGame game = other.GetComponent<ChapterOneGame>();
            if (game != null && Interactable != null)
            {
                game.AddNearby(Interactable);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            ChapterOneGame game = other.GetComponent<ChapterOneGame>();
            if (game != null && Interactable != null)
            {
                game.RemoveNearby(Interactable);
            }
        }
    }

    public sealed class ScenePortal : MonoBehaviour, IInteractable
    {
        public ChapterScene targetScene;
        public Vector3 spawnPosition = Vector3.zero;
        public string prompt = "前往下一处";
        public string arrivalLine = string.Empty;

        public string Prompt => prompt;

        public void Interact(ChapterOneGame game)
        {
            game.SwitchScene(targetScene, spawnPosition);
            if (!string.IsNullOrEmpty(arrivalLine))
            {
                game.SetDialogue(arrivalLine);
            }
        }
    }

    public sealed class DialogueInteractable : MonoBehaviour, IInteractable
    {
        public string prompt = "查看";
        [TextArea(2, 4)] public string normalLine;
        [TextArea(2, 4)] public string lampShadowLine;
        public bool requiresLampShadow;

        public string Prompt => prompt;

        public void Interact(ChapterOneGame game)
        {
            if (requiresLampShadow && !game.LampShadow)
            {
                game.SetDialogue("普通视角下只能看见一层潮灰。按 Q 切换灯影视角。");
                return;
            }

            string line = game.LampShadow && !string.IsNullOrEmpty(lampShadowLine) ? lampShadowLine : normalLine;
            game.SetDialogue(line);
        }
    }

    public sealed class OfferingInteractable : MonoBehaviour, IInteractable
    {
        public Offering offering;
        public string prompt = "摆放供品";

        public string Prompt => prompt;

        public void Interact(ChapterOneGame game)
        {
            game.AddOffering(offering);
        }
    }

    public sealed class BlackLampInteractable : MonoBehaviour, IInteractable
    {
        public string Prompt => "点燃黑灯";

        public void Interact(ChapterOneGame game)
        {
            game.LightBlackLamp();
        }
    }

    public sealed class OldWellInteractable : MonoBehaviour, IInteractable
    {
        public string Prompt => "照井";

        public void Interact(ChapterOneGame game)
        {
            game.TryOldWellEnding();
        }
    }
}
