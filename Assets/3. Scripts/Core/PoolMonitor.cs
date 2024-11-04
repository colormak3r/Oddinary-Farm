using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolMonitor : MonoBehaviour
{
    [Header("Debugs")]
    [SerializeField]
    private int highestPoolSize = 0;

    private string originalName;

    private void Start()
    {
        originalName = gameObject.name;
    }

    private void Update()
    {
        // TODO: calculate average pool size
        var childCount = transform.childCount;
        gameObject.name = originalName + " (" + childCount + ")";

        // Update highest pool size if needed
        if (childCount > highestPoolSize)
        {
            highestPoolSize = childCount;
        }
    }
}
