/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/28/2025 (Khoa)
 * Notes:           <write here>
*/

using ColorMak3r.Utility;
using Unity.VisualScripting;
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
        var cache = cachedBgmVolume;
        if (bgmSlider.value == cachedBgmVolume) return;
        value = value.Snap();
        cachedBgmVolume = value;

        bgmSlider.value = value;
        AudioManager.Main.BgmVolume = value;

        if (cache != cachedBgmVolume)
            AudioManager.Main.PlaySoundEffect(SoundEffect.UIHover);
    }

    float cachedSfxVolume;
    public void HandleSfxValueChange(float value)
    {
        var cache = cachedSfxVolume;
        if (sfxSlider.value == cachedSfxVolume) return;
        value = value.Snap();
        cachedSfxVolume = value;

        sfxSlider.value = value;
        AudioManager.Main.SfxVolume = value;

        if (cache != cachedSfxVolume)
            AudioManager.Main.PlaySoundEffect(SoundEffect.UIHover);
    }

    float cachedAbmVolume;
    public void HandleAmbientValueChange(float value)
    {
        var cache = cachedAbmVolume;
        if (abmSlider.value == cachedAbmVolume) return;
        value = value.Snap();
        cachedAbmVolume = value;

        abmSlider.value = value;
        AudioManager.Main.AbmVolume = value;

        if (cache != cachedAbmVolume)
            AudioManager.Main.PlaySoundEffect(SoundEffect.UIHover);
    }

    public void BackButtonClicked()
    {
        HideNoFade();
        OptionsUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }
}
