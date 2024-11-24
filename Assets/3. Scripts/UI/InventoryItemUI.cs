using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;
using UnityEngine.UI;

public class InventoryItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Settings")]
    [SerializeField]
    private Image itemImage;

    private int index;
    public int Index => index;

    private bool isInteractable;
    public bool IsInteractable => isInteractable;

    private Transform originalParent;
    private Vector2 originalPosition;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;
    }

    public void Initialize(int index, bool isInteractable)
    {
        this.index = index;
        this.isInteractable = isInteractable;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isInteractable) return;

        canvasGroup.blocksRaycasts = false;
        transform.SetParent(transform.root);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isInteractable) return;

        rectTransform.anchoredPosition += eventData.delta / GetComponentInParent<Canvas>().scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isInteractable) return;

        canvasGroup.blocksRaycasts = true;
        transform.SetParent(originalParent);
        rectTransform.anchoredPosition = originalPosition;        
    }

    public void UpdateImage(Sprite sprite)
    {
        itemImage.sprite = sprite;
        itemImage.enabled = sprite != null;
    }
}