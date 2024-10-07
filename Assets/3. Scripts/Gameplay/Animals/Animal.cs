using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Animal : NetworkBehaviour
{
    protected Rigidbody2D rbody;

    private Vector3 position_cached = Vector3.one;
    private int facing;

    private void Start()
    {

    }

    protected virtual void Update()
    {
        if (position_cached != transform.position)
        {
            facing = transform.position.x > position_cached.x ? 1 : -1;
            position_cached = transform.position;
        }

        transform.localScale = new Vector3(facing, 1, 1);
    }
}
