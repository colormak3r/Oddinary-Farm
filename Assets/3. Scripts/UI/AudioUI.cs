using ColorMak3r.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AudioUI : UIBehaviour
{
    public static AudioUI Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(transform.parent.gameObject);
    }

    [Header("Audio UI Settings")]
    [SerializeField]
    private Slider bgmSlider;
    [SerializeField]
    private Slider sfxSlider;
    [SerializeField]
    private Slider abmSlider;

    public void Initialize()
    {
        bgmSlider.value = AudioManager.Main.BgmVolume;
        sfxSlider.value = AudioManager.Main.SfxVolume;
        abmSlider.value = AudioManager.Main.AbmVolume;
    }

    float cachedBgmVolume;
    public void HandleBackgroundValueChange(float value)
    {
        if (bgmSlider.value == cachedBgmVolume) return;
        value = value.Snap();
        cachedBgmVolume = value;

        bgmSlider.value = value;
        AudioManager.Main.BgmVolume = value;
    }

    float cachedSfxVolume;
    public void HandleSfxValueChange(float value)
    {
        if (sfxSlider.value == cachedSfxVolume) return;
        value = value.Snap();
        cachedSfxVolume = value;

        sfxSlider.value = value;
        AudioManager.Main.SfxVolume = value;
    }

    float cachedAbmVolume;
    public void HandleAmbientValueChange(float value)
    {
        if (abmSlider.value == cachedAbmVolume) return;
        value = value.Snap();
        cachedAbmVolume = value;

        abmSlider.value = value;
        AudioManager.Main.AbmVolume = value;
    }

    public void BackButtonClicked()
    {
        HideNoFade();
        OptionsUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }
}
