using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AppearanceRow : MonoBehaviour
{
    [Header("Required Components")]
    [SerializeField]
    private Image borderImageLeft;
    [SerializeField]
    private Image borderImageRight;
    [SerializeField]
    private Image itemImageLeft;
    [SerializeField]
    private Image itemImageRight;
    [SerializeField]
    private TMP_Text displayTextLeft;
    [SerializeField]
    private TMP_Text displayTextRight;
    [SerializeField]
    private Button buttonLeft;
    [SerializeField]
    private Button buttonRight;

    private AppearanceData leftData;
    private AppearanceData rightData;
    private AppearanceUI ui;

    public void Initialize(AppearanceUI ui)
    {
        this.ui = ui;
    }

    public void SetDataLeft(AppearanceData data)
    {
        leftData = data;
        itemImageLeft.sprite = data.DisplaySprite;
        displayTextLeft.text = data.name;
    }

    public void SetDataRight(AppearanceData data)
    {
        rightData = data;
        itemImageRight.sprite = data.DisplaySprite;
        displayTextRight.text = data.name;
    }

    public void HideRight()
    {
        borderImageRight.color = Color.clear;
        itemImageRight.color = Color.clear;
        displayTextRight.color = Color.clear;
        buttonRight.enabled = false;
    }

    public void ButtonLeftClicked()
    {
        ui.HandleButtonClicked(leftData);
    }

    public void ButtonRightClicked()
    {
        ui.HandleButtonClicked(rightData);
    }
}
