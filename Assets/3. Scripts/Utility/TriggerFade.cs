using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ColorMak3r.Utility;
using Unity.Netcode;

public class TriggerFade : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float fadeAmount = 0.1f;
    [SerializeField]
    private Collider2D triggerCollider;
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    private bool isEntered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.attachedRigidbody.GetComponent<NetworkObject>().IsLocalPlayer && !isEntered)
        {
            StartCoroutine(spriteRenderer.SpriteFadeCoroutine(1, fadeAmount));
            isEntered = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.attachedRigidbody.GetComponent<NetworkObject>().IsLocalPlayer && isEntered)
        {
            StartCoroutine(spriteRenderer.SpriteFadeCoroutine(fadeAmount, 1));
            isEntered = false;
        }
    }
}
