/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/16/2025
 * Last Modified:   07/24/2025 (Khoa)
 * Notes:           <write here>
*/

using System.Collections.Generic;
using UnityEngine;

public class SpriteBlenderManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private bool showDebugs;

    private static HashSet<SpriteBlender> reblendQueue = new();
    public static void RequestBlend(SpriteBlender sb) => reblendQueue.Add(sb);

    private void Awake()
    {
        // Clear the reblend queue at the start to avoid any stale references
        // Effective in editor only
        reblendQueue.Clear();
    }

    private void LateUpdate()
    {
        if (showDebugs && reblendQueue.Count > 0)
            Debug.Log($"Reblending {reblendQueue.Count} sprites");
        foreach (var sb in reblendQueue) if (sb != null) sb.Blend(false);
        reblendQueue.Clear();
    }
}
