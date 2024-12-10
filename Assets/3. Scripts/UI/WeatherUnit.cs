using ColorMak3r.Utility;
using System.Collections;
using TMPro;
using UnityEngine;

public class WeatherUnit : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private TMP_Text timeText;
    [SerializeField]
    private TMP_Text weatherText;

    public void Initialize(string time, string weather, Vector2 startPos, Vector2 endPos, float duration)
    {
        timeText.text = time;
        weatherText.text = weather;
        StartCoroutine(DisplayCoroutine(startPos, endPos, duration));
    }

    private IEnumerator DisplayCoroutine(Vector2 startPos, Vector2 endPos, float duration)
    {
        transform.position = startPos;
        yield return transform.LerpMoveCoroutine(endPos, duration);
        Destroy(gameObject);
    }
}
