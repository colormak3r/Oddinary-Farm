using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Create a public reference for a transform of an object
[DisallowMultipleComponent]     // Component cannot be added twice as a component of the same game object
public class TransformReference : MonoBehaviour
{
    // NOTE: Consider using get and set instead; the 'field' keyword can be used to set variables that use
    // get and set to be editable in the inspector
    // [field: SerializeField]
    // public Transform TargetTransform { get; private set; }

    [SerializeField]
    private Transform targetTransform;

    public Transform Get() => targetTransform;
}
