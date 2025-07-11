using UnityEngine;
using TMPro;

public class RadioUI : UIBehaviour
{
    [SerializeField]
    private TMP_Text radioMessageText;

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
        radioMessageText.text = message;
        ShowNoFade();
        CancelInvoke(nameof(HideRadio));
        Invoke(nameof(HideRadio), 7f);
    }

    private void HideRadio()
    {
        HideNoFade();
    }
}
