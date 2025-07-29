/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/12/2025 (Khoa)
 * Notes:           <write here>
*/

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private Image displayImage;
    [SerializeField]
    private TMP_Text priceText;
    [SerializeField]
    private TMP_Text amountText;
    [SerializeField]
    private Button shopButton;
    [SerializeField]
    private Button quickActionButton;
    [SerializeField]
    private TMP_Text quickActionText;
    [SerializeField]
    private GameObject newTextObject;

    [Header("Button Settings")]
    [SerializeField]
    private float doubleClickThreshold = 0.5f; // Time in seconds to consider a double click

    [Header("Occilate Settings")]
    [SerializeField]
    private float minAngle = -2f;
    [SerializeField]
    private float maxAngle = 2f;
    [SerializeField]
    private float minPeriod = 1f;
    [SerializeField]
    private float maxPeriod = 3f;

    private ItemProperty itemProperty;
    private ShopUI shopUI;
    private int index;

    // Oscillation variables
    private Transform oscillateTarget;
    private bool isOscillating = false;
    private float oscillateTime = 0f;
    private float oscillatePeriod = 2f;
    private Quaternion baseRotation;

    public void SetShopEntry(ItemProperty itemProperty, ShopUI shopUI, ShopMode shopMode, float multiplier, int index, uint amount, bool isNew)
    {
        this.itemProperty = itemProperty;
        this.shopUI = shopUI;
        this.index = index;

        displayImage.sprite = itemProperty.IconSprite;
        var value = (uint)Mathf.Max(Mathf.CeilToInt(itemProperty.Price * multiplier), 1);
        priceText.text = (shopMode == ShopMode.Buy ? "<color=#ae2334>" : " <color=#239063>")
            + (shopMode == ShopMode.Buy ? "-" : "+") + "$" + value;
        quickActionText.text = (shopMode == ShopMode.Buy ? "<color=#ae2334>" : " <color=#239063>")
            + (shopMode == ShopMode.Buy ? "Buy Now!" : "$ Sell $");
        amountText.text = amount == 0 ? "" : amount.ToString();

        UpdateIsNew(isNew);
    }

    private float lastClicked = 0f;
    public void OnShopButtonClicked()
    {
        if (Time.time - lastClicked < doubleClickThreshold)
            shopUI.HandleQuickActionClicked(itemProperty, this, index);
        else
            shopUI.HandleShopButtonClicked(itemProperty, this, index);
        lastClicked = Time.time;
    }

    public void OnQuickButtonClicked()
    {
        shopUI.HandleQuickActionClicked(itemProperty, this, index);
    }

    public void UpdateEntry(uint amount)
    {
        amountText.text = amount == 0 ? "" : amount.ToString();
    }

    public void UpdateIsNew(bool isNew)
    {
        newTextObject.SetActive(isNew);
        if (isNew)
        {
            StartOscillation(newTextObject.transform);
        }
        else
        {
            StopOscillation();
        }
    }

    private void StartOscillation(Transform target)
    {
        oscillateTarget = target;
        isOscillating = true;
        oscillateTime = 0f;
        oscillatePeriod = UnityEngine.Random.Range(minPeriod, maxPeriod);
        baseRotation = target.localRotation;
    }

    private void StopOscillation()
    {
        isOscillating = false;
        if (oscillateTarget != null)
            oscillateTarget.localRotation = baseRotation;
        oscillateTarget = null;
    }

    private void Update()
    {
        if (isOscillating && oscillateTarget != null && oscillateTarget.gameObject.activeInHierarchy)
        {
            oscillateTime += Time.deltaTime;
            float mid = (minAngle + maxAngle) / 2f;
            float amp = (maxAngle - minAngle) / 2f;
            float angle = mid + amp * Mathf.Sin(2f * Mathf.PI * oscillateTime / oscillatePeriod);
            oscillateTarget.localRotation = baseRotation * Quaternion.Euler(0f, 0f, angle);
        }
    }

    public void Remove()
    {
        quickActionButton.onClick.RemoveAllListeners();
        Destroy(gameObject);
    }
}
