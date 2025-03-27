using System.Globalization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AppearanceUI : UIBehaviour
{
    public static AppearanceUI Main { get; private set; }

    private const string FACE_KEY = "Face Appearance";
    private const string HEAD_KEY = "Head Appearance";
    private const string HAT_KEY = "Hat Appearance";
    private const string OUTFIT_KEY = "Outfit Appearance";

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
    private Face[] faces;
    [SerializeField]
    private Head[] heads;
    [SerializeField]
    private Hat[] hats;
    [SerializeField]
    private Outfit[] outfits;

    [Header("Settings")]
    [SerializeField]
    private Face defaultFace;
    [SerializeField]
    private Head defaultHead;
    [SerializeField]
    private Hat defaultHat;
    [SerializeField]
    private Outfit defaultOutfit;

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
    private Image leftArmImage;
    [SerializeField]
    private Image rightArmImage;
    [SerializeField]
    private Image leftLegImage;
    [SerializeField]
    private Image rightLegImage;


    [Header("Debugs")]
    [SerializeField]
    private Face currentFace;
    [SerializeField]
    private Head currentHead;
    [SerializeField]
    private Hat currentHat;
    [SerializeField]
    private Outfit currentOutfit;

    public Face CurrentFace => currentFace;
    public Head CurrentHead => currentHead;
    public Hat CurrentHat => currentHat;
    public Outfit CurrentOutfit => currentOutfit;

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
        currentFace = defaultFace;
        currentHead = defaultHead;
        currentHat = defaultHat;
        currentOutfit = defaultOutfit;

        // TODO: Seperate AssetManager into a networked component and a local component
        /*currentFace = AssetManager.Main.GetScriptableObjectByName<Face>(PlayerPrefs.GetString(FACE_KEY, defaultFace.name));
        currentHead = AssetManager.Main.GetScriptableObjectByName<Head>(PlayerPrefs.GetString(HEAD_KEY, defaultHead.name));
        currentHat = AssetManager.Main.GetScriptableObjectByName<Hat>(PlayerPrefs.GetString(HAT_KEY, defaultHat.name));
        currentHat = AssetManager.Main.GetScriptableObjectByName<Hat>(PlayerPrefs.GetString(HAT_KEY, defaultHat.name));*/

        Initialize(defaultFace, defaultHead, defaultHat, defaultOutfit);
        FaceButtonClicked();
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
        currentFace = face;
        faceImage.sprite = face.DisplaySprite;

        if (PlayerAppearance.Owner) PlayerAppearance.Owner.UpdateFace(face);
    }

    private void SetHead(Head head)
    {
        currentHead = head;
        headImage.sprite = head.DisplaySprite;

        if (PlayerAppearance.Owner) PlayerAppearance.Owner.UpdateHead(head);
    }

    private void SetHat(Hat hat)
    {
        currentHat = hat;
        if (hat.name == "No Hat")
        {
            hatImage.enabled = false;
        }
        else
        {
            hatImage.enabled = true;
            hatImage.sprite = hat.DisplaySprite;
        }

        if (PlayerAppearance.Owner) PlayerAppearance.Owner.UpdateHat(hat);
    }

    private void SetOutfit(Outfit outfit)
    {
        currentOutfit = outfit;
        torsoImage.sprite = outfit.TorsoSprite;
        leftArmImage.sprite = outfit.LeftArmSprite;
        rightArmImage.sprite = outfit.RightArmSprite;
        leftLegImage.sprite = outfit.LeftLegSprite;
        rightLegImage.sprite = outfit.RightLegSprite;

        if (PlayerAppearance.Owner) PlayerAppearance.Owner.UpdateOutfit(outfit);
    }
}
