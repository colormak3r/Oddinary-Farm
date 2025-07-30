using UnityEngine;

[System.Serializable]
public struct ControlSetting
{
    public string ActionName;
    public string DefaultKey_Keyboard;
    public string DefaultKey_Controller;
    public string CurrentKey_Keyboard;
    public string CurrentKey_Controller;
}

public class ControlsUI : UIBehaviour
{
    public static ControlsUI Main { get; private set; }

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

    [Header("Control UI Settings")]
    [SerializeField]
    private ControlSetting[] controlSettings;
    [SerializeField]
    private ControlSetting[] controlSettings_Old;
    [SerializeField]
    private Transform contentTransform;
    [SerializeField]
    private GameObject controlRowPrefab;

    protected override void Start()
    {
        base.Start();
        Initialize();
    }

    private void Initialize()
    {
        foreach (var setting in controlSettings)
        {
            GameObject rowObject = Instantiate(controlRowPrefab, contentTransform);
            ControlRowUI rowUI = rowObject.GetComponent<ControlRowUI>();
            if (rowUI != null) rowUI.Initialize(setting);
        }
    }

    public void OnResetClicked()
    {
        Debug.Log("Reseted controls to default");
        AudioManager.Main.PlayClickSound();
    }

    public void OnBackClicked()
    {
        HideNoFade();
        OptionsUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }

}