using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Main { get; private set; }

    [Header("Debugs")]
    [SerializeField]
    private UIBehaviour currentUIBehavior;
    public UIBehaviour CurrentUIBehaviour
    {
        get => currentUIBehavior;
        set => currentUIBehavior = value;
    }

    private List<UIBehaviour> allUIBehaviours;

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
    }
}
