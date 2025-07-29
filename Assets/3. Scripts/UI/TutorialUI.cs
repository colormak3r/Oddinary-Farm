using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

using CTabButton = ColorMak3r.UI.TabButton;

[System.Serializable]
public struct TutorialProperty
{
    public string Name;
    public Vector3 Coordinate;
    public TutorialController Controller;
}

public class TutorialUI : UIBehaviour, ITabCallback
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
    private GameObject displayPanel;
    [SerializeField]
    private GameObject dontShowAgainButton;
    [SerializeField]
    private AudioClip tutorialSound;
    public AudioClip TutorialSound => tutorialSound;

    [Header("Tutorial Settings")]
    [SerializeField]
    private GameObject tabPanel;
    [SerializeField]
    private GameObject tabButtonPrefab;

    [Header("Tutorial Debugs")]
    [SerializeField]
    private int currentIndex = 0;
    [SerializeField]
    private bool dontShowAgain = false;
    public bool DontShowAgain => dontShowAgain;

    private List<CTabButton> tabButtons = new List<CTabButton>();

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);

        // Load the "Don't Show Again" preference from PlayerPrefs
        dontShowAgain = bool.Parse(PlayerPrefs.GetString(DONT_SHOW_AGAIN_KEY, "false"));
        dontShowAgainButton.SetActive(false);
        tutorialCamera.gameObject.SetActive(false);

        for (int i = 0; i < tutorialProperties.Length; i++)
        {
            var tabButton = Instantiate(tabButtonPrefab, tabPanel.transform)
                .GetComponent<CTabButton>();
            tabButton.Initialize(tutorialProperties[i].Name, i, this);
            tabButton.SetSelected(i == 0 ? true : false);
            tabButtons.Add(tabButton);
        }

        OnVisibilityChanged.AddListener(HandleVisibilityChange);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        OnVisibilityChanged.RemoveListener(HandleVisibilityChange);
    }

    private void HandleVisibilityChange(bool isVisible)
    {
        if (isVisible) OnPageChange(currentIndex);
        tutorialCamera.gameObject.SetActive(isVisible);
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

    [ContextMenu("Reset Tutorial")]
    public void ResetTutorial()
    {
        SetDontShowAgain(false);
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
            OnPageChange(currentIndex);
        }
    }

    [ContextMenu("Mock Play Animation")]
    private void MockPlayAnimation()
    {
        OnPageChange(0);
    }

    private void OnPageChange(int newPageIndex)
    {
        // Disable the "Don't Show Again" button if not on the last page
        if (newPageIndex == tutorialProperties.Length - 1)
            dontShowAgainButton.SetActive(true);
        else
            dontShowAgainButton.SetActive(false);

        // Deselect all buttons and select the current one
        foreach (var button in tabButtons) button.SetSelected(false);
        tabButtons[newPageIndex].SetSelected(true);

        // Play the animation for the current tutorial property
        tutorialProperties[newPageIndex].Controller.PlayAnimation();
        tutorialCamera.transform.position = tutorialProperties[newPageIndex].Coordinate;

        // Update Next button text
        nextButtonText.text = $"{(currentIndex == tutorialProperties.Length - 1 ? "Done" : "Next")}({currentIndex + 1}/{tutorialProperties.Length})";
    }

    public void OnTabButton(int id)
    {
        tutorialProperties[currentIndex].Controller.StopAnimation();

        currentIndex = id;

        OnPageChange(id);
    }
}
