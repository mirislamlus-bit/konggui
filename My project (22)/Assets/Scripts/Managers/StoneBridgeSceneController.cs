using UnityEngine;

public sealed class StoneBridgeSceneController : MonoBehaviour
{
    public static void ShowMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ShowDialogue(message.Split('\n'));
            return;
        }

        Debug.Log(message);
    }
}
