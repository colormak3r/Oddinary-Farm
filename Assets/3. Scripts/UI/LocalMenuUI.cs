using UnityEngine;

public class LocalMenuUI : UIBehaviour
{
    public void MultiplayerLocalHostButtonClicked()
    {
        ConnectionManager.Main.StartGameMultiplayerLocalHost();
    }

    public void MultiplayerLocalClientButtonClicked()
    {
        ConnectionManager.Main.StartGameMultiplayerLocalClient();
    }
}
