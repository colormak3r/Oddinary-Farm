using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TransformUtility
{
    private static Vector3 HALF_UP = new Vector3 (0, 0.5f, 0);

    public static Vector3 PositionHalfUp(this Transform transform)
    {
        return transform.position + HALF_UP;
    }
}