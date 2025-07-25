using UnityEngine;
using TMPro;
using System.Collections;

public class RadioUI : UIBehaviour
{
    [Header("Radio UI")]
    [SerializeField]
    private float messageDisplayTime = 5f;
    [SerializeField]
    private TMP_Text radioMessageText;
    [SerializeField]
    private TMP_Text primaryShadowText;
    [SerializeField]
    private TMP_Text secondaryShadowText;

    public static RadioUI Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }

    private Coroutine displayMessageCoroutine;
    public void DisplayMessage(string message)
    {
        if (displayMessageCoroutine != null) StopCoroutine(displayMessageCoroutine);
        displayMessageCoroutine = StartCoroutine(DisplayMessageCoroutine(message));
    }

    private IEnumerator DisplayMessageCoroutine(string message)
    {
        Show();

        string currentText;
        for (int i = 0; i < message.Length; i++)
        {
            currentText = message.Substring(0, i + 1);
            SetRadioMessage(currentText);
            yield return new WaitForSeconds(0.01f);
        }

        yield return new WaitForSeconds(messageDisplayTime);
        Hide();
    }

    private void SetRadioMessage(string message)
    {
        radioMessageText.text = message;
        primaryShadowText.text = message;
        secondaryShadowText.text = message;
    }
}
