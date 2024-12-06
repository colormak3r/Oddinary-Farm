using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AppearanceUI : UIBehaviour
{
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
    private AppearanceData[] outfit;

    [Header("Required Component")]
    [SerializeField]
    private Transform contentTransform;
    [SerializeField]
    private Image background;

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
        RenderRows(outfit);
    }

    private void RenderRows(AppearanceData[] data)
    {
        while (contentTransform.childCount > 0)
        {
            DestroyImmediate(contentTransform.GetChild(0).gameObject);
        }

        int count = data.Length / 2;
        for (int i = 0; i < count; i++)
        {
            var rowObj = Instantiate(rowPrefab, contentTransform);
            var row = rowObj.GetComponent<AppearanceRow>();
            row.Initialize(this);
            row.SetDataLeft(data[i]);
            if (i + 1 < data.Length)
                row.SetDataRight(data[i + 1]);
        }
    }

    public void HandleButtonClicked(AppearanceData data)
    {
        if (data is Face)
            PlayerAppearance.Owner.UpdateFace(data as Face);
        else if (data is Head)
            PlayerAppearance.Owner.UpdateHead(data as Head);
        else if (data is Hat)
            PlayerAppearance.Owner.UpdateHat(data as Hat);
        else if (data is Outfit)
            PlayerAppearance.Owner.UpdateOutfit(data as Outfit);
    }
}
