using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransitionUI : UIBehaviour
{
    public static TransitionUI Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }
}
