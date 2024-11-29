using System.Collections;
using System.Collections.Generic;
using Steamworks.Data;
using UnityEngine;

public class LobbyUI : UIBehaviour
{
    public static LobbyUI Main;

    private void Awake()
    {
        if (Main == null)
        {
            Main = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnLobbyEntered(Lobby lobby)
    {

    }
}
