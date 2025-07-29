/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/22/2025
 * Last Modified:   07/22/2025 (Khoa)
 * Notes:           Vibecoded using chatgpt
*/

using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class WaveTextEffect : MonoBehaviour
{
    [Header("Wave Text Effect Settings")]
    [SerializeField]
    private float amplitude = 5f;     // Height of the wave
    [SerializeField]
    private float frequency = 2f;     // Speed of the wave
    [SerializeField]
    private float waveSpeed = 1f;     // How fast the wave moves
    [SerializeField]
    private TMP_Text textComponent;

    private TMP_TextInfo textInfo;
    private Vector3[][] originalVertices;

    private void Awake()
    {
        if (textComponent == null)
        {
            textComponent = GetComponent<TMP_Text>();
        }
    }

    private void OnEnable()
    {
        HandleTextChanged();
    }

    [ContextMenu("Force Update Text")]
    public void HandleTextChanged()
    {
        textComponent.ForceMeshUpdate();
        textInfo = textComponent.textInfo;

        originalVertices = new Vector3[textInfo.meshInfo.Length][];
        for (int i = 0; i < originalVertices.Length; i++)
        {
            originalVertices[i] = textInfo.meshInfo[i].vertices.Clone() as Vector3[];
        }
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        textComponent.ForceMeshUpdate();
        textInfo = textComponent.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible)
                continue;

            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;

            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            Vector3 offset = new Vector3(
                0,
                Mathf.Sin(Time.time * waveSpeed + i * frequency) * amplitude,
                0
            );

            for (int j = 0; j < 4; j++)
            {
                vertices[vertexIndex + j] = originalVertices[materialIndex][vertexIndex + j] + offset;
            }
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}
