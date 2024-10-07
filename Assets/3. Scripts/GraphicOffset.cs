using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphicOffset : MonoBehaviour
{
    [SerializeField]
    private Transform graphicTransform;

    public Vector2 OffsetTransfrom => transform.position  + graphicTransform.localPosition;
}
