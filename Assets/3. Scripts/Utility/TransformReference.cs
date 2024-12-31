using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TransformReference : MonoBehaviour
{
    [SerializeField]
    private Transform targetTransform;

    public Transform Get() => targetTransform;
}
