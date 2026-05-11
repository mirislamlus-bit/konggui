using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InteractionSystemRepair
{
    private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player_LinZhaoying.prefab";
    private const string InteractableTag = "Interactable";

    [MenuItem("JianDeng/Repair E Interaction System")]
    public static void Repair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Repair E Interaction System can only run in Edit Mode.");
            return;
        }

        EnsureInteractableTag();
        RepairPlayerPrefab();
        RepairOpenScene();

        AssetDatabase.SaveAssets();
        Debug.Log("E interaction system repair complete.");
    }

    private static void RepairPlayerPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("Player prefab not found: " + PlayerPrefabPath);
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        EnsurePlayer(root);
        PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void RepairOpenScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
        {
            Debug.LogWarning("No saved scene is open. Open a Chapter 1 scene, then run JianDeng/Repair E Interaction System.");
            return;
        }

        GameObject player = GameObject.Find("Player_LinZhaoying");
        if (player == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            player = prefab != null ? PrefabUtility.InstantiatePrefab(prefab) as GameObject : new GameObject("Player_LinZhaoying");
            player.name = "Player_LinZhaoying";
        }

        EnsurePlayer(player);
        RepairInteractablesInScene();
        CreateTestInteractable(player);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void EnsurePlayer(GameObject player)
    {
        if (player.GetComponent<PlayerController>() == null)
        {
            player.AddComponent<PlayerController>();
        }

        Rigidbody2D body = player.GetComponent<Rigidbody2D>() ?? player.AddComponent<Rigidbody2D>();
        body.freezeRotation = true;

        Collider2D bodyCollider = FindBodyCollider(player);
        if (bodyCollider == null)
        {
            CapsuleCollider2D capsule = player.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.55f, 1.65f);
            capsule.offset = new Vector2(0f, 0.82f);
            bodyCollider = capsule;
        }
        bodyCollider.isTrigger = false;

        foreach (InteractionDetector detector in player.GetComponents<InteractionDetector>())
        {
            Object.DestroyImmediate(detector, true);
        }

        Transform range = player.transform.Find("InteractionRange");
        if (range == null)
        {
            GameObject rangeObject = new GameObject("InteractionRange");
            rangeObject.transform.SetParent(player.transform, false);
            range = rangeObject.transform;
        }

        range.localPosition = Vector3.zero;
        range.localRotation = Quaternion.identity;
        range.localScale = Vector3.one;

        CircleCollider2D trigger = range.GetComponent<CircleCollider2D>() ?? range.gameObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 1.15f;
        trigger.offset = new Vector2(0f, 0.55f);

        if (range.GetComponent<InteractionDetector>() == null)
        {
            range.gameObject.AddComponent<InteractionDetector>();
        }

        Debug.Log("Player interaction setup checked: " + player.name);
    }

    private static Collider2D FindBodyCollider(GameObject player)
    {
        foreach (Collider2D collider in player.GetComponents<Collider2D>())
        {
            if (!collider.isTrigger)
            {
                return collider;
            }
        }

        Collider2D[] colliders = player.GetComponents<Collider2D>();
        return colliders.Length > 0 ? colliders[0] : null;
    }

    private static void RepairInteractablesInScene()
    {
        MonoBehaviour[] behaviours = Object.FindObjectsOfType<MonoBehaviour>(true);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (!(behaviour is IInteractable))
            {
                continue;
            }

            GameObject target = behaviour.gameObject;
            Collider2D collider = target.GetComponent<Collider2D>();
            if (collider == null)
            {
                collider = target.AddComponent<BoxCollider2D>();
            }
            collider.isTrigger = true;
            SetInteractableTag(target);
            Debug.Log("Interactable checked: " + target.name);
        }

        foreach (GameObject target in Object.FindObjectsOfType<GameObject>(true))
        {
            if (!LooksLikeInteractable(target) || target.GetComponent<IInteractable>() != null)
            {
                continue;
            }

            Collider2D collider = target.GetComponent<Collider2D>();
            if (collider == null)
            {
                collider = target.AddComponent<BoxCollider2D>();
            }
            collider.isTrigger = true;
            SetInteractableTag(target);

            InteractableObject interactable = target.AddComponent<InteractableObject>();
            SerializedObject serialized = new SerializedObject(interactable);
            serialized.FindProperty("interactionId").stringValue = target.name;
            SerializedProperty lines = serialized.FindProperty("dialogueLines");
            lines.arraySize = 1;
            lines.GetArrayElementAtIndex(0).stringValue = target.name;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("Generic interactable added: " + target.name);
        }
    }

    private static bool LooksLikeInteractable(GameObject target)
    {
        string name = target.name;
        return name.Contains("_Interactable") ||
            name.Contains("_Interact") ||
            name.Contains("BlackLantern") ||
            name.Contains("Door") ||
            name.Contains("OldWell");
    }

    private static void CreateTestInteractable(GameObject player)
    {
        GameObject test = GameObject.Find("Test_Interactable") ?? new GameObject("Test_Interactable");
        test.transform.position = player.transform.position + Vector3.right;
        test.transform.localScale = Vector3.one;
        SetInteractableTag(test);

        BoxCollider2D collider = test.GetComponent<BoxCollider2D>() ?? test.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1f, 1f);

        InteractableObject interactable = test.GetComponent<InteractableObject>() ?? test.AddComponent<InteractableObject>();
        SerializedObject serialized = new SerializedObject(interactable);
        serialized.FindProperty("interactionId").stringValue = "test";
        SerializedProperty lines = serialized.FindProperty("dialogueLines");
        lines.arraySize = 1;
        lines.GetArrayElementAtIndex(0).stringValue = "这是一个测试交互。";
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("Created/updated Test_Interactable at player + 1m.");
    }

    private static void EnsureInteractableTag()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0)
        {
            return;
        }

        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == InteractableTag)
            {
                return;
            }
        }

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = InteractableTag;
        tagManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInteractableTag(GameObject target)
    {
        if (HasTag(InteractableTag))
        {
            target.tag = InteractableTag;
        }
    }

    private static bool HasTag(string tagName)
    {
        if (string.IsNullOrEmpty(tagName))
        {
            return false;
        }

        try
        {
            GameObject.FindGameObjectsWithTag(tagName);
            return true;
        }
        catch (UnityException)
        {
            return false;
        }
    }
}
