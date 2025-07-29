/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/28/2025 (Khoa)
 * Notes:           <write here>
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CTabButton = ColorMak3r.UI.TabButton;

public class AppearanceUI : UIBehaviour, ITabCallback
{
    public static AppearanceUI Main { get; private set; }

    private void Awake()
    {
        if (Main == null)
        {
            Main = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("Appearance UI Settings")]
    [SerializeField]
    private GameObject rowPrefab;

    [Header("Required Component")]
    [SerializeField]
    private Transform contentTransform;
    [SerializeField]
    private CTabButton faceButton;
    [SerializeField]
    private CTabButton headButton;
    [SerializeField]
    private CTabButton hatButton;
    [SerializeField]
    private CTabButton outfitButton;
    [SerializeField]
    private Image faceImage;
    [SerializeField]
    private Image headImage;
    [SerializeField]
    private Image hatImage;
    [SerializeField]
    private Image torsoImage;
    [SerializeField]
    private Image leftArmImage;
    [SerializeField]
    private Image rightArmImage;
    [SerializeField]
    private Image leftLegImage;
    [SerializeField]
    private Image rightLegImage;

    private AppearanceManager appearanceManager;

    protected override void Start()
    {
        base.Start();

        appearanceManager = AppearanceManager.Main;

        faceButton.Initialize("Face", 0, this);
        headButton.Initialize("Head", 1, this);
        hatButton.Initialize("Hat", 2, this);
        outfitButton.Initialize("Outfit", 3, this);

        OnVisibilityChanged.AddListener(isShowing =>
        {
            if (isShowing)
            {
                Initialize(appearanceManager.CurrentFace,
                    appearanceManager.CurrentHead,
                    appearanceManager.CurrentHat,
                    appearanceManager.CurrentOutfit);
                FaceButtonClicked();
            }
        });
    }

    public void Initialize(Face face, Head head, Hat hat, Outfit outfit)
    {
        SetFace(face);
        SetHead(head);
        SetHat(hat);
        SetOutfit(outfit);
    }

    public void FaceButtonClicked()
    {
        RenderRows(appearanceManager.FaceAssets);
    }

    public void HeadButtonClicked()
    {
        RenderRows(appearanceManager.HeadAssets);
    }

    public void HatButtonClicked()
    {
        RenderRows(appearanceManager.HatAssets);
    }

    public void OutfitButtonClicked()
    {
        RenderRows(appearanceManager.OutfitAssets);
    }
    public void OnBackButtonClicked()
    {
        Hide();
        AudioManager.Main.PlayClickSound();
    }

    private void RenderRows(IReadOnlyList<AppearanceData> data)
    {
        while (contentTransform.childCount > 0)
        {
            DestroyImmediate(contentTransform.GetChild(0).gameObject);
        }

        int count = data.Count;
        for (int i = 0; i < count; i += 2)
        {
            var rowObj = Instantiate(rowPrefab, contentTransform);
            var row = rowObj.GetComponent<AppearanceRow>();
            row.Initialize(this);
            row.SetDataLeft(data[i]);
            if (i + 1 < count)
                row.SetDataRight(data[i + 1]);
            else
                row.HideRight();
        }
    }

    public void HandleButtonClicked(AppearanceData data)
    {
        switch (data)
        {
            case Face face:
                SetFace(face);
                break;
            case Head head:
                SetHead(head);
                break;
            case Hat hat:
                SetHat(hat);
                break;
            case Outfit outfit:
                SetOutfit(outfit);
                break;
        }
    }

    private void SetFace(Face face)
    {
        appearanceManager.SetFace(face);
        faceImage.sprite = face.DisplaySprite;
    }

    private void SetHead(Head head)
    {
        appearanceManager.SetHead(head);
        headImage.sprite = head.DisplaySprite;
    }

    private void SetHat(Hat hat)
    {
        appearanceManager.SetHat(hat);
        if (hat.name == "No Hat")
        {
            hatImage.enabled = false;
        }
        else
        {
            hatImage.enabled = true;
            hatImage.sprite = hat.DisplaySprite;
        }
    }

    private void SetOutfit(Outfit outfit)
    {
        appearanceManager.SetOutfit(outfit);
        torsoImage.sprite = outfit.TorsoSprite;
        leftArmImage.sprite = outfit.LeftArmSprite;
        rightArmImage.sprite = outfit.RightArmSprite;
        leftLegImage.sprite = outfit.LeftLegSprite;
        rightLegImage.sprite = outfit.RightLegSprite;
    }

    public void OnTabButton(int id)
    {
        AudioManager.Main.PlayClickSound();

        // Deselect all buttons
        faceButton.SetSelected(false);
        headButton.SetSelected(false);
        hatButton.SetSelected(false);
        outfitButton.SetSelected(false);

        switch (id)
        {
            case 0:
                faceButton.SetSelected(true);
                FaceButtonClicked();
                break;
            case 1:
                headButton.SetSelected(true);
                HeadButtonClicked();
                break;
            case 2:
                hatButton.SetSelected(true);
                HatButtonClicked();
                break;
            case 3:
                outfitButton.SetSelected(true);
                OutfitButtonClicked();
                break;
            default:
                Debug.LogWarning($"Unknown tab ID: {id}");
                break;
        }
    }
}
