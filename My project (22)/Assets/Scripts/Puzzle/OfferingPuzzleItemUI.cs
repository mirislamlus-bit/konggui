using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class OfferingPuzzleItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private string offeringId;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform homeParent;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private OfferingPuzzleSlotUI currentSlot;

    private RectTransform rectTransform;
    private Vector2 homePosition;
    private Transform dragParent;

    public string OfferingId => offeringId;
    public OfferingPuzzleSlotUI CurrentSlot => currentSlot;

    public void Configure(string id, Canvas rootCanvas, RectTransform home, Transform dragLayer, Sprite sprite, string label)
    {
        offeringId = id;
        canvas = rootCanvas;
        homeParent = home;
        dragParent = dragLayer;
        EnsureComponents();

        Image image = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = sprite != null ? Color.white : new Color(0.9f, 0.84f, 0.68f, 1f);
        image.raycastTarget = true;

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(transform, false);
        Text text = labelObject.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 18;
        text.alignment = TextAnchor.LowerCenter;
        text.color = new Color(0.96f, 0.91f, 0.76f, 1f);
        text.raycastTarget = false;
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(0f, -22f);
        labelRect.offsetMax = new Vector2(0f, 0f);

        ReturnHome();
    }

    public void ReturnHome()
    {
        EnsureComponents();

        if (currentSlot != null)
        {
            currentSlot.Clear();
            currentSlot = null;
        }

        transform.SetParent(homeParent, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = homePosition;
        rectTransform.sizeDelta = new Vector2(88f, 88f);
    }

    public void SetHomePosition(Vector2 position)
    {
        homePosition = position;
        ReturnHome();
    }

    public void SetSlot(OfferingPuzzleSlotUI slot)
    {
        currentSlot = slot;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        EnsureComponents();

        if (currentSlot != null)
        {
            currentSlot.Clear();
            currentSlot = null;
        }

        transform.SetParent(dragParent, true);
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        EnsureComponents();

        float scaleFactor = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        rectTransform.anchoredPosition += eventData.delta / scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EnsureComponents();

        canvasGroup.blocksRaycasts = true;
        if (currentSlot == null)
        {
            ReturnHome();
        }
    }

    private void Awake()
    {
        EnsureComponents();
    }

    private void EnsureComponents()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            Debug.Log("[Chapter1Offering] Added missing CanvasGroup to " + name);
        }
    }
}
