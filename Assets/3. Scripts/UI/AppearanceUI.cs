using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AppearanceUI : UIBehaviour
{
    public static AppearanceUI Main { get; private set; }

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(transform.parent.gameObject);
    }

    [Header("Appearance UI Settings")]
    [SerializeField]
    private GameObject rowPrefab;
    [SerializeField]
    private AppearanceData[] faces;
    [SerializeField]
    private AppearanceData[] heads;
    [SerializeField]
    private AppearanceData[] hats;
    [SerializeField]
    private AppearanceData[] outfits;

    [Header("Required Component")]
    [SerializeField]
    private Transform contentTransform;
    [SerializeField]
    private Image background;

    [SerializeField]
    private Image faceImage;
    [SerializeField]
    private Image headImage;
    [SerializeField]
    private Image hatImage;
    [SerializeField]
    private Image torsoImage;
    [SerializeField]
    private Image rightArmImage;
    [SerializeField]
    private Image leftArmImage;
    [SerializeField]
    private Image rightLegImage;
    [SerializeField]
    private Image leftLegImage;

    protected override void OnEnable()
    {
        base.OnEnable();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 0)
        {
            background.enabled = false;
        }
        else
        {
            background.enabled = true;
        }
    }

    private void Start()
    {
        FaceButtonClicked();
    }

    public void Initialize(Face face, Head head, Hat hat, Outfit outfit)
    {
        faceImage.sprite = face.DisplaySprite;
        headImage.sprite = head.DisplaySprite;
        if (hat.name == "No Hat")
            hatImage.sprite = null;
        else
            hatImage.sprite = hat.DisplaySprite;
        torsoImage.sprite = outfit.TorsoSprite;
        rightArmImage.sprite = outfit.RightArmSprite;
        leftArmImage.sprite = outfit.LeftArmSprite;
        rightLegImage.sprite = outfit.RightLegSprite;
        leftLegImage.sprite = outfit.LeftLegSprite;
    }

    public void FaceButtonClicked()
    {
        RenderRows(faces);
    }

    public void HeadButtonClicked()
    {
        RenderRows(heads);
    }

    public void HatButtonClicked()
    {
        RenderRows(hats);
    }

    public void OutfitButtonClicked()
    {
        RenderRows(outfits);
    }

    private void RenderRows(AppearanceData[] data)
    {
        while (contentTransform.childCount > 0)
        {
            DestroyImmediate(contentTransform.GetChild(0).gameObject);
        }

        int count = data.Length;
        for (int i = 0; i < count; i += 2)
        {
            var rowObj = Instantiate(rowPrefab, contentTransform);
            var row = rowObj.GetComponent<AppearanceRow>();
            row.Initialize(this);
            row.SetDataLeft(data[i]);
            if (i + 1 < data.Length)
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
                PlayerAppearance.Owner.UpdateFace(face);
                faceImage.sprite = face.DisplaySprite;
                break;
            case Head head:
                PlayerAppearance.Owner.UpdateHead(head);
                headImage.sprite = head.DisplaySprite;
                break;
            case Hat hat:
                PlayerAppearance.Owner.UpdateHat(hat);
                if (hat.name == "No Hat")
                {
                    hatImage.sprite = null;
                    hatImage.color = Color.clear;
                }                    
                else
                {
                    hatImage.sprite = hat.DisplaySprite;
                    hatImage.color = Color.white;
                }
                break;
            case Outfit outfit:
                PlayerAppearance.Owner.UpdateOutfit(outfit);
                torsoImage.sprite = outfit.TorsoSprite;
                rightArmImage.sprite = outfit.RightArmSprite;
                leftArmImage.sprite = outfit.LeftArmSprite;
                rightLegImage.sprite = outfit.RightLegSprite;
                leftLegImage.sprite = outfit.LeftLegSprite;
                break;
        }
    }
}
