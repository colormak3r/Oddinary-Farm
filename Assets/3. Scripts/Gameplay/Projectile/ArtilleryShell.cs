/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/05/2025 (Khoa)
 * Notes:           <write here>
*/

using System.Collections;
using UnityEngine;
using ColorMak3r.Utility;
using Unity.Netcode;

public class ArtilleryShell : NetworkBehaviour
{
    [SerializeField]
    private GameObject markerObject;
    [SerializeField]
    private GameObject indicatorObject;
    [SerializeField]
    private GameObject explosionObject;
    [SerializeField]
    private AudioClip windupClip;
    [SerializeField]
    private AudioClip explosionClip;
    [SerializeField]
    private AudioElement audioElement;

    public override void OnNetworkSpawn()
    {
        StartShell();
    }

    [ContextMenu("Start Shell Coroutine")]
    private void StartShell()
    {
        StartCoroutine(ShellCoroutine(2, DamageType.Blunt, Hostility.Absolute, null));
    }

    private IEnumerator ShellCoroutine(uint damage, DamageType damageType, Hostility projectileHostility, Transform attacker)
    {
        markerObject.SetActive(true);
        explosionObject.SetActive(false);

        audioElement.PlayOneShot(windupClip, false);
        StartCoroutine(indicatorObject.transform.PopCoroutine(0, 1, 2f));
        yield return markerObject.transform.RotateCoroutine(720, 2f);

        if (IsServer)
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, 3f);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out EntityStatus entityStatus))
                {
                    entityStatus.TakeDamage(damage, damageType, projectileHostility, attacker); // Example damage value
                }
            }
        }

        audioElement.PlayOneShot(explosionClip, true);

        markerObject.SetActive(false);
        explosionObject.SetActive(true);

        yield return new WaitForSeconds(4f);
        if (IsServer) Destroy(gameObject);
    }
}
