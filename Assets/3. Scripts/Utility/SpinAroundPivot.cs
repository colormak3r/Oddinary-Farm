using UnityEngine;

public class SpinAroundPivot : MonoBehaviour
{
    [Header("Spin Settings")]
    [SerializeField]
    private float rotationSpeed = 20f;

    private bool spin = true;

    private void OnEnable()
    {
        spin = true;
    }

    private void OnDisable()
    {
        spin = false;
    }

    private void FixedUpdate()
    {
        if (!spin) return;

        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }
}
