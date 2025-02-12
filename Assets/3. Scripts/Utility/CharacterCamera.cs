using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterCamera : CameraBehaviour
{
    public static CharacterCamera Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }
}
