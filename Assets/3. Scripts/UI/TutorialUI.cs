using System;
using System.Collections;
using TMPro;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

[System.Serializable]
public struct TutorialProperty
{
    public string Name;
    public Vector3 Coordinate;
    public TutorialController Controller;
}

public class TutorialUI : UIBehaviour
{
    private static string DONT_SHOW_AGAIN_KEY = "DontShowTutorialAgain";

    public static TutorialUI Main;

    [Header("Tutorial Settings")]
    [SerializeField]
    private TutorialProperty[] tutorialProperties;
    [SerializeField]
    private Camera tutorialCamera;
    [SerializeField]
    private TMP_Text nextButtonText;
    [SerializeField]
    private TMP_Text titleText;
    [SerializeField]
    private AudioClip tutorialSound;
    public AudioClip TutorialSound => tutorialSound;

    [Header("Tutorial Debugs")]
    [SerializeField]
    private int currentIndex = 0;
    [SerializeField]
    private bool dontShowAgain = false;
    public bool DontShowAgain => dontShowAgain;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);

        // Load the "Don't Show Again" preference from PlayerPrefs
        dontShowAgain = bool.Parse(PlayerPrefs.GetString(DONT_SHOW_AGAIN_KEY, "false"));

        OnVisibilityChanged.AddListener(HandleVisibilityChange);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        OnVisibilityChanged.RemoveListener(HandleVisibilityChange);
    }

    private void HandleVisibilityChange(bool isVisible)
    {
        if (isVisible) OnPageChange();
    }

    public void OnDontShowAgainButton()
    {
        SetDontShowAgain(true);
        tutorialProperties[currentIndex].Controller.StopAnimation();
        Hide();
    }

    private void SetDontShowAgain(bool value)
    {
        dontShowAgain = value;
        PlayerPrefs.SetString(DONT_SHOW_AGAIN_KEY, value.ToString());
        PlayerPrefs.Save();
    }

    [ContextMenu("Update Don't Show Again")]
    private void UpdateDontShowAgain()
    {
        SetDontShowAgain(dontShowAgain);
    }

    private Coroutine pageChangeCoroutine;
    public void OnNextButton()
    {
        tutorialProperties[currentIndex].Controller.StopAnimation();
        if (currentIndex + 1 >= tutorialProperties.Length)
        {
            Hide();
            return;
        }
        else
        {
            currentIndex++;
            if (pageChangeCoroutine != null) StopCoroutine(pageChangeCoroutine);
            pageChangeCoroutine = StartCoroutine(PageChangeCoroutine());
        }
    }

    private IEnumerator PageChangeCoroutine()
    {
        yield return HideCoroutine();
        yield return ShowCoroutine();
    }

    public void UpdateNextButtonText() => nextButtonText.text = $"{(currentIndex == tutorialProperties.Length - 1 ? "Done" : "Next")}({currentIndex + 1}/{tutorialProperties.Length})";

    [ContextMenu("Mock Play Animation")]
    private void MockPlayAnimation()
    {
        OnPageChange();
    }

    private void OnPageChange()
    {
        titleText.text = tutorialProperties[currentIndex].Name;
        tutorialProperties[currentIndex].Controller.PlayAnimation();
        tutorialCamera.transform.position = tutorialProperties[currentIndex].Coordinate;
        UpdateNextButtonText();
    }
}
