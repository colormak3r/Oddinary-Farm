using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AppearanceManager : MonoBehaviour
{
    private const string FACE_KEY = "Face Appearance";
    private const string HEAD_KEY = "Head Appearance";
    private const string HAT_KEY = "Hat Appearance";
    private const string OUTFIT_KEY = "Outfit Appearance";

    public static AppearanceManager Main { get; private set; }

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
#if UNITY_EDITOR
        FetchAssets();
#endif
        DontDestroyOnLoad(gameObject);
    }

    [Header("Settings")]
    [SerializeField]
    private Face defaultFace;
    [SerializeField]
    private Head defaultHead;
    [SerializeField]
    private Hat defaultHat;
    [SerializeField]
    private Outfit defaultOutfit;

    [Header("Asset Path")]
    [SerializeField]
    private string facePath = "Assets/3. Scripts/Appearance/Faces";
    [SerializeField]
    private string headPath = "Assets/3. Scripts/Appearance/Heads";
    [SerializeField]
    private string hatPath = "Assets/3. Scripts/Appearance/Hats";
    [SerializeField]
    private string outfitPath = "Assets/3. Scripts/Appearance/Outfits";

    [Header("Assets")]
    [SerializeField]
    private List<Face> faceAssets = new List<Face>();
    public IReadOnlyList<Face> FaceAssets => faceAssets;
    [SerializeField]
    private List<Head> headAssets = new List<Head>();
    public IReadOnlyList<Head> HeadAssets => headAssets;
    [SerializeField]
    private List<Hat> hatAssets = new List<Hat>();
    public IReadOnlyList<Hat> HatAssets => hatAssets;
    [SerializeField]
    private List<Outfit> outfitAssets = new List<Outfit>();
    public IReadOnlyList<Outfit> OutfitAssets => outfitAssets;

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

#if UNITY_EDITOR
    [ContextMenu("Fetch Assets")]
    public void FetchAssets()
    {
        faceAssets.Clear();
        headAssets.Clear();
        hatAssets.Clear();
        outfitAssets.Clear();

        faceAssets.AddRange(AssetManager.LoadAllScriptableObjectsInFolder<Face>(facePath));
        headAssets.AddRange(AssetManager.LoadAllScriptableObjectsInFolder<Head>(headPath));
        hatAssets.AddRange(AssetManager.LoadAllScriptableObjectsInFolder<Hat>(hatPath));
        outfitAssets.AddRange(AssetManager.LoadAllScriptableObjectsInFolder<Outfit>(outfitPath));

        Hat noHat = hatAssets.Find(h => h.name == "No Hat");
        hatAssets.Remove(noHat);
        hatAssets.Insert(0, noHat);

        EditorUtility.SetDirty(this);
    }
#endif

    private void Start()
    {
        currentFace = faceAssets.Find(h => h.name == PlayerPrefs.GetString(FACE_KEY, defaultFace.name));
        currentHead = headAssets.Find(h => h.name == PlayerPrefs.GetString(HEAD_KEY, defaultHead.name));
        currentHat = hatAssets.Find(h => h.name == PlayerPrefs.GetString(HAT_KEY, defaultHat.name));
        currentOutfit = outfitAssets.Find(h => h.name == PlayerPrefs.GetString(OUTFIT_KEY, defaultOutfit.name));
    }

    public void SetFace(Face face)
    {
        if (face == null) return;

        currentFace = face;
        PlayerPrefs.SetString(FACE_KEY, face.name);
        PlayerPrefs.Save();

        if (PlayerAppearance.Owner) PlayerAppearance.Owner.UpdateFace(face);
    }

    public void SetHead(Head head)
    {
        if (head == null) return;

        currentHead = head;
        PlayerPrefs.SetString(HEAD_KEY, head.name);
        PlayerPrefs.Save();

        if (PlayerAppearance.Owner) PlayerAppearance.Owner.UpdateHead(head);
    }


    public void SetHat(Hat hat)
    {
        if (hat == null) return;

        currentHat = hat;
        PlayerPrefs.SetString(HAT_KEY, hat.name);
        PlayerPrefs.Save();

        if (PlayerAppearance.Owner) PlayerAppearance.Owner.UpdateHat(hat);
    }

    public void SetOutfit(Outfit outfit)
    {
        if (outfit == null) return;

        currentOutfit = outfit;
        PlayerPrefs.SetString(OUTFIT_KEY, outfit.name);
        PlayerPrefs.Save();

        if (PlayerAppearance.Owner) PlayerAppearance.Owner.UpdateOutfit(outfit);
    }

    [ContextMenu("Reset Preferences")]
    private void ResetPreference()
    {
        PlayerPrefs.SetString(FACE_KEY, defaultFace.name);
        PlayerPrefs.SetString(HEAD_KEY, defaultHead.name);
        PlayerPrefs.SetString(HAT_KEY, defaultHat.name);
        PlayerPrefs.SetString(OUTFIT_KEY, defaultOutfit.name);
    }
}
