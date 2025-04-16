using System.Collections;
using UnityEngine;
using ColorMak3r.Utility;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIBehaviour : MonoBehaviour
{
    [Header("UI Behaviour Settings")]
    [SerializeField]
    protected GameObject container;
    [SerializeField]
    protected float fadeDuration = 0.25f;
    [SerializeField]
    protected bool delayShow;
    [Tooltip("These objects appear after others and fade out first.")]
    [SerializeField]
    protected GameObject[] delayShowFadeFirstObjects;
    [SerializeField]
    protected GameObject[] ignoreObjects;

    [Header("Persistence UI Settings")]
    [SerializeField]
    private bool excludeFromUIManager = false;
    [SerializeField]
    private Image background;

    [Header("UI Behaviour Debugs")]
    [SerializeField]
    private bool isShowing;
    public bool IsShowing => isShowing;
    [SerializeField]
    private bool isAnimating;
    public bool IsAnimating => isAnimating;
    private CanvasRenderer[] allRenderers;
    private CanvasRenderer[] dsffoRenderers;
    private CanvasRenderer[] ignoreRenderers;

    [HideInInspector]
    public UnityEvent<bool> OnVisibilityChanged;

    protected virtual void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    protected virtual void Destroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    protected virtual void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!excludeFromUIManager)
            UIManager.Main.RegisterUI(this);

        // Disable background in the main menu if it exist
        if (background) background.enabled = scene.buildIndex != 0;
    }

    protected virtual void OnEnable()
    {
        isShowing = container.activeSelf;

        var allRenderers = new List<CanvasRenderer>();
        allRenderers.AddRange(container.GetComponentsInChildren<CanvasRenderer>(true));
        var dsffoRenderers = new List<CanvasRenderer>();
        var ignoreRenderers = new List<CanvasRenderer>();

        if (delayShowFadeFirstObjects != null)
        {
            foreach (GameObject obj in delayShowFadeFirstObjects)
            {
                dsffoRenderers.AddRange(obj.GetComponentsInChildren<CanvasRenderer>(true));
            }
        }

        if (ignoreObjects != null)
        {
            foreach (GameObject obj in ignoreObjects)
            {
                ignoreRenderers.AddRange(obj.GetComponentsInChildren<CanvasRenderer>(true));
            }
        }

        allRenderers.RemoveAll(renderer => dsffoRenderers.Contains(renderer));
        allRenderers.RemoveAll(renderer => ignoreRenderers.Contains(renderer));
        this.allRenderers = allRenderers.ToArray();
        this.dsffoRenderers = dsffoRenderers.ToArray();
        this.ignoreRenderers = ignoreRenderers.ToArray();
    }

    public IEnumerator ShowCoroutine(bool fade = true)
    {
        if (isShowing) yield break;

        if (delayShow) yield return new WaitForSeconds(fadeDuration);

        if (dsffoRenderers != null && dsffoRenderers.Length > 0)
        {
            foreach (var dsffoRenderer in dsffoRenderers)
            {
                dsffoRenderer.SetAlpha(0);
            }
        }

        isShowing = true;

        container.SetActive(true);

        isAnimating = true;
        if (allRenderers != null && allRenderers.Length > 0)
            yield return allRenderers.UIFadeCoroutine(0, 1, fade ? fadeDuration : 0);
        if (dsffoRenderers != null && dsffoRenderers.Length > 0)
            yield return dsffoRenderers.UIFadeCoroutine(0, 1, fade ? fadeDuration : 0);
        isAnimating = false;

        OnVisibilityChanged?.Invoke(isShowing);
    }

    public IEnumerator HideCoroutine(bool fade = true)
    {
        if (!isShowing) yield break;

        isAnimating = true;
        if (dsffoRenderers != null && dsffoRenderers.Length > 0)
            yield return dsffoRenderers.UIFadeCoroutine(1, 0, fade ? fadeDuration : 0);
        if (allRenderers != null && allRenderers.Length > 0)
            yield return allRenderers.UIFadeCoroutine(1, 0, fade ? fadeDuration : 0);
        isAnimating = false;

        container.SetActive(false);

        isShowing = false;

        OnVisibilityChanged?.Invoke(isShowing);
    }

    public void Show()
    {
        if (IsShowing || isAnimating) return;

        if (UIManager.Main != null) UIManager.Main.CurrentUIBehaviour = this;

        StartCoroutine(ShowCoroutine());
    }

    public void Hide()
    {
        if (!IsShowing || isAnimating) return;

        StartCoroutine(HideCoroutine());
    }

    public void ShowNoFade()
    {
        if (IsShowing || isAnimating) return;
        StartCoroutine(ShowCoroutine(false));
    }

    public void HideNoFade()
    {
        if (!IsShowing || isAnimating) return;
        StartCoroutine(HideCoroutine(false));
    }

    [ContextMenu("Toggle Show")]
    public virtual void ToggleShow()
    {
        if (isAnimating) return;

        if (isShowing)
            StartCoroutine(HideCoroutine());
        else
            StartCoroutine(ShowCoroutine());
    }
}
