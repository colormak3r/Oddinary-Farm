using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamera : CameraBehaviour
{
    public static MainCamera Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }
}
