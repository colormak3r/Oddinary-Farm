/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/02/2025 (Khoa)
 * Notes:           <write here>
*/

using ColorMak3r.Utility;
using System;
using UnityEngine;

public class WorldRenderer : MonoBehaviour
{
    private WorldGenerator worldGenerator;

    private Vector2 position_cached;

    private void Start()
    {
        worldGenerator = WorldGenerator.Main;
    }

    private void Update()
    {
        if (!GameManager.Main.IsInitialized) return;
        var currentPosition = transform.position.SnapToGrid();
        if (position_cached != currentPosition)
        {
            position_cached = currentPosition;
            worldGenerator.BuildWorld(currentPosition);
        }
    }
}
