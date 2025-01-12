using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOnAwake : MonoBehaviour
{
    private void Awake()
    {
        // Destroy the object if it is created in the first frame when the game first launches
        if (Time.time < 0.01f) Destroy(gameObject);
    }
}
