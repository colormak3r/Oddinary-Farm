/*
 * Created By:      Emily Tsai
 * Date Created:    --/--/----
 * Last Modified:   08/06/2025 (Emily)
 * Notes:           <write here>
*/

using UnityEngine;
using System;

public class SlotMachine : Structure, IInteractable
{

public void Interact(transform Source)
{
    if (SlotMachineUI.Main.IsShowing)
        ShopUI.Main.CloseShop();
    else
        ShopUI.Main.OpenShop(source.GetComponent<PlayerInventory>(), shopInventory, transform);
}

}
