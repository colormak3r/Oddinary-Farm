using UnityEngine;
using TMPro;

public class RadioUI : UIBehaviour
{
    [SerializeField]
    private TMP_Text radioMessageText;

    [SerializeField]
    private TMP_Text primaryShadowText;

    [SerializeField]
    private TMP_Text secondaryShadowText;

    [SerializeField] private SpriteRenderer radioSprite;

    public void SetRadioColor(Color color)
    {
        radioSprite.color = color;
    }

    private void SetRadioMessage(string message)
    {
        radioMessageText.text = message;
        primaryShadowText.text = message;
        secondaryShadowText.text = message;
    }

    public static RadioUI Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }

    public void DisplayMessage(string message)
    {
        SetRadioMessage(message);
        ShowNoFade();
        CancelInvoke(nameof(HideRadio));
        Invoke(nameof(HideRadio), 7f);
    }

    private void HideRadio()
    {
        HideNoFade();
    }
}
