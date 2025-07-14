/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/12/2025 (Khoa)
 * Notes:           <write here>
*/

using UnityEngine;

public class VendingMachine : Structure, IInteractable
{
    [Header("Vending Machine Settings")]
    [SerializeField]
    private ShopInventory shopInventory;

    public bool IsHoldInteractable => false;

    public void Interact(Transform source)
    {
        if (ShopUI.Main.IsShowing)
            ShopUI.Main.CloseShop();
        else
            ShopUI.Main.OpenShop(source.GetComponent<PlayerInventory>(), shopInventory, transform);
    }

    public void InteractionEnd(Transform source)
    {
        throw new System.NotImplementedException();
    }

    public void InteractionStart(Transform source)
    {
        throw new System.NotImplementedException();
    }
}
