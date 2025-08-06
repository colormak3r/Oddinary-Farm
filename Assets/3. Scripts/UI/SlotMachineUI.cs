/*
 * Created By:      Emily Tsai
 * Date Created:    --/--/----
 * Last Modified:   08/06/2025 (Emily)
 * Notes:           <write here>
*/

using UnityEngine;

public class SlotMachineUI : UIBehaviour
{
    public static SlotMachineUI Main;

    private void Awake()
    {
        if (Main == null)
            Main == this;
        else
            Destroy(GameObject);
    }

    public void ClosePanel()
    {
        if (IsAnimating) return;
        if (!IsShowing) return;

        WalletManager.Main.OnLocalWalletChanged -= HandleLocalWalletChanged;

        InputManager.Main.SwitchMap(InputMap.Gameplay);

        AudioManager.Main.PlayClickSound();
        StartCoroutine(CloseShopCoroutine());
    }
}
