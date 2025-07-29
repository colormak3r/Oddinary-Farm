using UnityEngine;
using TMPro;
using System.Collections;

public class RadioUI : UIBehaviour
{
    public static RadioUI Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }

    [Header("Radio UI")]
    [SerializeField]
    private float messageDisplayTime = 5f;
    [SerializeField]
    private TMP_Text radioMessageText;

    [Header("Debugs")]
    [SerializeField]
    private bool isSkipped;

    // Moved object state such as activated/inactivated color to the actual object (Radio.cs)
    // UI script should only do UI-related tasks

    [ContextMenu("Display Random Message")]
    private void DisplayRandomMessage()
    {
        DisplayMessage("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec pretium sit amet lacus ut laoreet. Ut ac ornare lorem. Ut sit amet porta ex. Fusce efficitur urna eu turpis iaculis, a convallis purus tempor. Aliquam volutpat laoreet lorem, ac dictum diam mattis nec. Praesent ipsum arcu, ullamcorper a dapibus consectetur, lobortis sit amet turpis. Donec sit amet aliquet libero, at vulputate elit. Donec ultrices, sapien id suscipit ultricies, felis erat tempor ante, quis porta est orci id mauris. Etiam blandit tortor non massa mollis, ac bibendum nulla gravida. Vivamus porttitor fringilla velit, eu cursus ante ultricies sit amet. Quisque efficitur rhoncus vehicula. Morbi accumsan sodales arcu, laoreet finibus nulla iaculis eget.");
    }

    // Use coroutine instead of invoke
    // Invoke is usually not recommended for performance reasons
    private Coroutine displayMessageCoroutine;
    public void DisplayMessage(string message)
    {
        isSkipped = false; // Reset skip state when a new message is displayed
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
            if (isSkipped)
            {
                SetRadioMessage(message);
                break;
            }

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
    }


    public void OnSkipClicked()
    {
        var previous = isSkipped;
        isSkipped = !isSkipped;

        if (previous == true)
        {
            if (displayMessageCoroutine != null) StopCoroutine(displayMessageCoroutine);
            Hide();
        }
    }
}
