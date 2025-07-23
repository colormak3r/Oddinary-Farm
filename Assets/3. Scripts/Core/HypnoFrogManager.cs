/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/22/2025
 * Last Modified:   07/22/2025 (Khoa)
 * Notes:           <write here>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class HypnoFrogManager : NetworkBehaviour
{
    public static HypnoFrogManager Main { get; private set; }

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

    [Header("Settings")]
    [SerializeField]
    private int curseChangeHour = 19; // Hour when the frog's curse changes
    public int CurseChangeHour => curseChangeHour;
    [SerializeField]
    private int appearanceHour = 20; // Hour when the frog appears
    public int AppearanceHour => appearanceHour;
    [SerializeField]
    private int disappearanceHour = 6; // Hour when the frog disappears
    public int DisappearanceHour => disappearanceHour;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;

    private Dictionary<Vector2, bool> hypnoFrogLocations = new Dictionary<Vector2, bool>();

    public override void OnNetworkSpawn()
    {
        if (IsServer) TimeManager.Main.OnHourChanged.AddListener(HandleOnHourChanged);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer) TimeManager.Main.OnHourChanged.RemoveListener(HandleOnHourChanged);
    }

    private int currentDate_cached = -1;
    private void HandleOnHourChanged(int currentHour)
    {
        if (currentHour == curseChangeHour && currentDate_cached != TimeManager.Main.CurrentDate)
        {
            currentDate_cached = TimeManager.Main.CurrentDate;
            int index = UnityEngine.Random.Range(0, hypnoFrogLocations.Count);
            var keys = hypnoFrogLocations.Keys.ToList();
            for (int i = 0; i < hypnoFrogLocations.Count; i++)
            {
                if (i == index)
                {
                    if (showDebugs) Debug.Log($"HypnoFrogManager: Setting curse for frog at position {keys[i]}");
                    hypnoFrogLocations[keys[i]] = true; // Set the frog at this position to have a curse
                }
                else
                {
                    if (showDebugs) Debug.Log($"HypnoFrogManager: Resetting frog at position {keys[i]}");
                    hypnoFrogLocations[keys[i]] = false; // Reset other frogs
                }
            }
        }
    }

    public void RequestAddToList(Vector2 position)
    {
        if (!hypnoFrogLocations.ContainsKey(position))
        {
            hypnoFrogLocations[position] = false;
        }
    }

    public bool GetHasFrog(Vector2 position)
    {
        foreach (var key in hypnoFrogLocations.Keys)
        {
            if (Vector2.Distance(position, key) < 1)
            {
                return hypnoFrogLocations[key];
            }
        }

        Debug.LogWarning($"HypnoFrogManager: No frog found at position {position}");
        return false;
    }
}
