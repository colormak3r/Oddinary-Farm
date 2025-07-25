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

    // Moved object state such as activated/inactivated color to the actual object (Radio.cs)
    // UI script should only do UI-related tasks

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }

    // Use coroutine instead of invoke
    // Invoke is usually not recommended for performance reasons
    private Coroutine displayMessageCoroutine;
    public void DisplayMessage(string message)
    {
        if (displayMessageCoroutine != null) StopCoroutine(displayMessageCoroutine);
        displayMessageCoroutine = StartCoroutine(DisplayMessageCoroutine(message));
    }

    private IEnumerator DisplayMessageCoroutine(string message)
    {
        // Use Show instead of ShowNoFade for smoother transitions and bring attention to the text
        // Our eyes can detect motion better than static images
        Show(); 

        // Scrolling text effect
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
