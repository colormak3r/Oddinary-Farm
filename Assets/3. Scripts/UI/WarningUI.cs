using System.Collections;
using TMPro;
using UnityEngine;

public class WarningUI : UIBehaviour
{
    public static WarningUI Main { get; private set; }
    [Header("Warning UI")]
    [SerializeField]
    private float waitDuration = 1f;
    [SerializeField]
    private AudioClip warningSound;

    private void Awake()
    {
        if (Main == null)
        {
            Main = this;
        }
    }

    public void ShowWarning(string message)
    {
        StopAllCoroutines();
        StartCoroutine(ShowWarningCoroutine(message));
    }

    private IEnumerator ShowWarningCoroutine(string message)
    {
        for (int i = 0; i < 3; i++)
        {
            yield return ShowCoroutine();
            AudioManager.Main.PlayOneShot(warningSound);
            yield return new WaitForSeconds(waitDuration);
            yield return HideCoroutine();
        }
    }

    [ContextMenu("Show Warning")]
    private void ShowBreachDetected()
    {
        ShowWarning("Breach Detected");
    }
}
