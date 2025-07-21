/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/19/2025
 * Last Modified:   07/21/2025 (Khoa)
 * Notes:           <write here>
*/

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HypnoFrog : NetworkBehaviour, IInteractable
{
    public bool IsHoldInteractable => false;

    public override void OnNetworkSpawn()
    {
        for (int x = -6; x < 6; x++)
        {
            for (int y = -5; y < 5; y++)
            {
                WorldGenerator.Main.RemoveFoliageOnClient(new Vector2(x, y));
            }
        }
    }

    public void Interact(Transform source)
    {

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
