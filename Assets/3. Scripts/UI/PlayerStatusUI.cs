using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerStatusUI : UIBehaviour
{
    public static PlayerStatusUI Main;

    private void Awake()
    {
        if (Main != null)
            Destroy(Main.gameObject);
        else
            Main = this;
    }

    [SerializeField]
    private TMP_Text healthText;

    public void UpdateHealth(uint health)
    {
        healthText.text = $"Health: {health}";
    }
}
