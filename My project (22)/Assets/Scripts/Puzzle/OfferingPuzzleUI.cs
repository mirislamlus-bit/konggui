using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class OfferingPuzzleUI : MonoBehaviour
{
    private static readonly string[] OfferingIds =
    {
        "Offering_Apple",
        "Offering_Cake",
        "Offering_WineCup",
        "Offering_IncenseBurner",
        "Offering_Candle"
    };

    private static readonly string[] OfferingLabels =
    {
        "\u82f9\u679c",
        "\u7cd5\u70b9",
        "\u9152\u676f",
        "\u9999\u7089",
        "\u767d\u8721\u70db"
    };

    [SerializeField] private OfferingPuzzleManager manager;
    [SerializeField] private OfferingPuzzleSlotUI[] slots;

    public static OfferingPuzzleUI Create(OfferingPuzzleManager manager, Sprite[] sprites)
    {
        EnsureEventSystem();

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Chapter1_UI_Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject root = new GameObject("OfferingPuzzleUI");
        root.transform.SetParent(canvas.transform, false);
        OfferingPuzzleUI ui = root.AddComponent<OfferingPuzzleUI>();
        ui.manager = manager;
        ui.Build(canvas, sprites);
        root.SetActive(false);
        return ui;
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void Build(Canvas canvas, Sprite[] sprites)
    {
        Image scrim = gameObject.AddComponent<Image>();
        scrim.color = new Color(0f, 0f, 0f, 0.3f);
        scrim.raycastTarget = true;
        Stretch(GetComponent<RectTransform>());

        GameObject panel = CreatePanel(transform, "OfferingPuzzlePanel", new Color(0.18f, 0.12f, 0.075f, 0.8f), new Vector2(0f, 340f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.07f, 0f);
        panelRect.anchorMax = new Vector2(0.93f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 28f);
        panelRect.sizeDelta = new Vector2(0f, 340f);

        Text title = CreateText(panel.transform, "Title", "\u4f9b\u684c", 24, TextAnchor.UpperCenter);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -12f);
        titleRect.sizeDelta = new Vector2(-220f, 34f);

        Text hint = CreateText(panel.transform, "Hint", "\u5c06\u4f9b\u54c1\u62d6\u5230\u6b63\u786e\u7684\u4f4d\u7f6e\u3002", 18, TextAnchor.UpperCenter);
        RectTransform hintRect = hint.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0f, 1f);
        hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.pivot = new Vector2(0.5f, 1f);
        hintRect.anchoredPosition = new Vector2(0f, -48f);
        hintRect.sizeDelta = new Vector2(-220f, 28f);

        RectTransform itemArea = CreateArea(panel.transform, "OfferingItemsArea", new Vector2(0f, 1f), new Vector2(0.48f, 1f), new Vector2(28f, -92f), new Vector2(-42f, 118f), new Vector2(0f, 1f));
        RectTransform slotArea = CreateArea(panel.transform, "OfferingSlotsArea", new Vector2(0.52f, 1f), new Vector2(1f, 1f), new Vector2(14f, -92f), new Vector2(-42f, 118f), new Vector2(0f, 1f));
        RectTransform dragLayer = CreateArea(panel.transform, "DragLayer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));

        slots = new OfferingPuzzleSlotUI[OfferingIds.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            GameObject slotObject = CreatePanel(slotArea, "OfferingSlot", new Color(1f, 1f, 1f, 0.18f), new Vector2(96f, 96f));
            RectTransform slotRect = slotObject.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0f, 1f);
            slotRect.anchorMax = new Vector2(0f, 1f);
            slotRect.pivot = new Vector2(0f, 1f);
            slotRect.anchoredPosition = new Vector2(i * 112f, 0f);
            slots[i] = slotObject.AddComponent<OfferingPuzzleSlotUI>();
            slots[i].Configure(i);
        }

        for (int i = 0; i < OfferingIds.Length; i++)
        {
            GameObject itemObject = new GameObject("Draggable_" + OfferingIds[i]);
            itemObject.transform.SetParent(itemArea, false);
            itemObject.AddComponent<RectTransform>();
            itemObject.AddComponent<CanvasGroup>();
            OfferingPuzzleItemUI item = itemObject.AddComponent<OfferingPuzzleItemUI>();
            Sprite sprite = sprites != null && i < sprites.Length ? sprites[i] : null;
            item.Configure(OfferingIds[i], canvas, itemArea, dragLayer, sprite, OfferingLabels[i]);
            item.SetHomePosition(new Vector2(40f + i * 104f, -8f));
        }

        Button confirm = CreateButton(panel.transform, "ConfirmButton", "\u786e\u8ba4", new Vector2(0f, 26f));
        RectTransform confirmRect = confirm.GetComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.5f, 0f);
        confirmRect.anchorMax = new Vector2(0.5f, 0f);
        confirm.onClick.AddListener(Confirm);

        Button close = CreateButton(panel.transform, "CloseButton", "\u5173\u95ed", new Vector2(-18f, -16f));
        RectTransform closeRect = close.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        close.onClick.AddListener(Close);
    }

    private void Confirm()
    {
        string[] order = new string[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            order[i] = slots[i].OfferingId;
        }

        manager.TrySolveUiOrder(order);
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color, Vector2 size)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        return panel;
    }

    private static RectTransform CreateArea(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot)
    {
        GameObject area = new GameObject(name);
        area.transform.SetParent(parent, false);
        RectTransform rect = area.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Text CreateText(Transform parent, string name, string value, int size, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = size;
        text.alignment = alignment;
        text.color = new Color(0.95f, 0.9f, 0.76f, 1f);
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 position)
    {
        GameObject buttonObject = CreatePanel(parent, name, new Color(0.34f, 0.23f, 0.14f, 0.92f), new Vector2(108f, 42f));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        Button button = buttonObject.AddComponent<Button>();
        Text text = CreateText(buttonObject.transform, "Text", label, 20, TextAnchor.MiddleCenter);
        Stretch(text.GetComponent<RectTransform>());
        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }
}
