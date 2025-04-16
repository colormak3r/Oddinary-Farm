using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Main { get; private set; }

    [Header("Debugs")]
    [SerializeField]
    private bool isShowing = true;
    [SerializeField]
    private bool showDebugs = false;
    [SerializeField]
    private UIBehaviour currentUIBehavior;
    public UIBehaviour CurrentUIBehaviour
    {
        get => currentUIBehavior;
        set => currentUIBehavior = value;
    }

    [SerializeField]
    private Dictionary<UIBehaviour, bool> sceneUIBehaviours = new Dictionary<UIBehaviour, bool>();

    private void Awake()
    {
        if (Main == null)
        {
            Main = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene arg0, Scene arg1)
    {
        if (showDebugs) Debug.Log($"Clear sceneUIBehaviour");
        sceneUIBehaviours.Clear();
    }

    public void RegisterUI(UIBehaviour behaviour)
    {
        sceneUIBehaviours.Add(behaviour, behaviour.IsShowing);
        if (showDebugs) Debug.Log($"Register UI: {behaviour.name}, isShowing: {behaviour.IsShowing}");
    }

    [ContextMenu("Hide All UI")]
    public void HideUI()
    {
        isShowing = false;
        var keys = new List<UIBehaviour>(sceneUIBehaviours.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            var ui = keys[i];
            if (ui.IsShowing)
            {
                ui.Hide();
                sceneUIBehaviours[ui] = true;
                if (showDebugs) Debug.Log($"Hide UI: {ui.name}");
            }
            else
            {
                sceneUIBehaviours[ui] = false;
            }
        }
    }

    [ContextMenu("Show All UI")]
    public void ShowUI()
    {
        isShowing = true;
        var keys = new List<UIBehaviour>(sceneUIBehaviours.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            var ui = keys[i];
            if (sceneUIBehaviours[ui])
            {
                if (ui.gameObject.activeInHierarchy)
                {
                    ui.Show();
                    if (showDebugs) Debug.Log($"Show UI: {ui.name}");
                }
            }
        }
    }

    [ContextMenu("Toggle All UI")]
    public void ToggleUI()
    {
        isShowing = !isShowing;
        if (isShowing)
        {
            ShowUI();
        }
        else
        {
            HideUI();
        }
    }
}
