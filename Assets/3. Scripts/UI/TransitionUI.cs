using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransitionUI : UIBehaviour
{
    public static TransitionUI Main;

    [Header("Settings")]
    [SerializeField]
    private bool coverOnStart;
    [SerializeField]
    private bool revealOnStart;

    private void Awake()
    {
        if (Main == null)
        {
            Main = this;
        }
        else
        {
            Destroy(transform.parent.gameObject);
        }

        DontDestroyOnLoad(transform.parent.gameObject);


        if (revealOnStart)
        {
            StartCoroutine(RevealAfterStartCoroutine());
        }
        else
        {
            if (coverOnStart) StartCoroutine(ShowCoroutine(false));
        }
    }

    private IEnumerator RevealAfterStartCoroutine()
    {
        yield return ShowCoroutine(false);
        StartCoroutine(HideCoroutine());
    }
}
