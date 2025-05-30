using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
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
    private Dictionary<UIBehaviour, bool> sceneUIBehaviours = new Dictionary<UIBehaviour, bool>();
    [SerializeField]
    private List<UIBehaviour> sceneUIList = new List<UIBehaviour>();

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
        foreach (var ui in sceneUIBehaviours.Keys)
        {
            if (ui != null)
            {
                ui.OnSceneChanged(arg1);
            }
        }
    }

    public void RegisterUI(UIBehaviour behaviour)
    {
        sceneUIBehaviours.Add(behaviour, behaviour.IsShowing);
        sceneUIList.Add(behaviour);
        behaviour.OnSceneChanged(SceneManager.GetActiveScene());
        if (showDebugs) Debug.Log($"Register UI: {behaviour.name}, isShowing: {behaviour.IsShowing}");
    }

    public void UnregisterUI(UIBehaviour behaviour)
    {
        if (sceneUIBehaviours.ContainsKey(behaviour))
        {
            sceneUIList.Remove(behaviour);
            sceneUIBehaviours.Remove(behaviour);
            if (showDebugs) Debug.Log($"Unregister UI: {behaviour.name}");
        }
    }

    [ContextMenu("Hide All UI")]
    public void HideUI()
    {
        StartCoroutine(HideUI(false, false));
    }

    public IEnumerator HideUI(bool showCursor, bool showPlayerName)
    {
        isShowing = false;
        var keys = new List<UIBehaviour>(sceneUIBehaviours.Keys);
        var coroutines = new List<Coroutine>();
        for (int i = 0; i < keys.Count; i++)
        {
            var ui = keys[i];
            if (ui.IsShowing)
            {
                coroutines.Add(StartCoroutine(ui.HideCoroutine()));
                sceneUIBehaviours[ui] = true;
                if (showDebugs) Debug.Log($"Hide UI: {ui.name}");
            }
            else
            {
                sceneUIBehaviours[ui] = false;
            }
        }

        foreach (var coroutine in coroutines)
        {
            yield return coroutine;
        }

        Cursor.visible = showCursor;

        foreach (var obj in NetworkManager.Singleton.ConnectedClientsList)
        {
            obj.PlayerObject.GetComponent<PlayerStatus>().PlayerNameUI.SetShowPlayerName(showPlayerName);
        }
    }

    [ContextMenu("Show All UI")]
    public void ShowUI()
    {
        StartCoroutine(ShowUI(true, true));
    }

    public IEnumerator ShowUI(bool showCursor, bool showPlayerName)
    {
        isShowing = true;
        var keys = new List<UIBehaviour>(sceneUIBehaviours.Keys);
        var coroutines = new List<Coroutine>();
        for (int i = 0; i < keys.Count; i++)
        {
            var ui = keys[i];
            if (sceneUIBehaviours[ui])
            {
                if (ui.gameObject.activeInHierarchy)
                {
                    coroutines.Add(StartCoroutine(ui.ShowCoroutine()));
                    if (showDebugs) Debug.Log($"Show UI: {ui.name}");
                }
            }
        }

        foreach (var coroutine in coroutines)
        {
            yield return coroutine;
        }

        Cursor.visible = showCursor;

        foreach (var obj in NetworkManager.Singleton.ConnectedClientsList)
        {
            obj.PlayerObject.GetComponent<PlayerStatus>().PlayerNameUI.SetShowPlayerName(showPlayerName);
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
