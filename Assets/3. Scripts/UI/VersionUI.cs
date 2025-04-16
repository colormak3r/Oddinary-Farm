using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VersionUI : UIBehaviour
{
    [SerializeField]
    private TMP_Text versionText;

    protected override void Start()
    {
        base.Start();

        versionText.text = "v" + VersionUtility.VERSION;
    }
}
