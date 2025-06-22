using System.Collections;
using UnityEngine;
using ColorMak3r.Utility;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UIBehaviour : MonoBehaviour
{
    [Header("UI Behaviour Settings")]
    [SerializeField]
    protected GameObject container;
    [SerializeField]
    protected GameObject firstElement;
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
    protected bool showDebugs = false;
    [SerializeField]
    private bool isShowing;
    public bool IsShowing => isShowing;
    [SerializeField]
    private bool isAnimating;
    public bool IsAnimating => isAnimating;
    private CanvasRenderer[] allRenderers;
    private CanvasRenderer[] dsffoRenderers;
    private CanvasRenderer[] ignoreRenderers;

    private Coroutine showCoroutine;
    private Coroutine hideCoroutine;

    [HideInInspector]
    public UnityEvent<bool> OnVisibilityChanged;

    protected virtual void Start()
    {
        if (this == null) return;

        if (!excludeFromUIManager)
            UIManager.Main.RegisterUI(this);
    }

    protected virtual void OnDestroy()
    {
        if (this == null) return;

        if (!excludeFromUIManager)
            UIManager.Main.UnregisterUI(this);
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

    public virtual void OnSceneChanged(Scene scene)
    {
        // Disable background in the main menu if it exist
        if (background)
        {
            background.enabled = SceneManager.GetActiveScene().buildIndex != 0;
            // Debug.Log($"{name} background.enabled: {background.enabled}");
        }
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

        OnVisibilityChanged?.Invoke(isShowing);

        container.SetActive(true);

        isAnimating = true;
        if (allRenderers != null && allRenderers.Length > 0)
            yield return allRenderers.UIFadeCoroutine(0, 1, fade ? fadeDuration : 0);
        if (dsffoRenderers != null && dsffoRenderers.Length > 0)
            yield return dsffoRenderers.UIFadeCoroutine(0, 1, fade ? fadeDuration : 0);
        isAnimating = false;

        var gamepads = Gamepad.all;
        if (gamepads.Count > 0)
        {
            if (firstElement)
            {
                EventSystem.current.SetSelectedGameObject(firstElement);
            }
            else
            {
                if (showDebugs) Debug.LogWarning($"{name} has no firstElement set");
            }
        }
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
        if (IsShowing) return;

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        showCoroutine = StartCoroutine(ShowCoroutine());
    }

    public void Hide()
    {
        if (!IsShowing) return;

        if (showCoroutine != null) StopCoroutine(showCoroutine);
        hideCoroutine = StartCoroutine(HideCoroutine());
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
