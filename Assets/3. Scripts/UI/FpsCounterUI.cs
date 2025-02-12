using TMPro;
using UnityEngine;

public class FpsCounterUI : UIBehaviour
{
    [Header("Fps Counter UI Settings")]
    [SerializeField]
    private TMP_Text fpsText;

    [Header("Fps Counter UI Debugs")]
    [SerializeField]
    private int avgFrameRate;

    private float nextRead;

    private void Update()
    {
        if (Time.unscaledTime > nextRead)
        {
            nextRead = Time.unscaledTime + 1f;
            avgFrameRate = CalculateFps();
        }

        fpsText.text = $"{avgFrameRate}";
    }

    private int CalculateFps()
    {
        float current = 0;
        float count = 0;
        for (int i = 0; i < 10; i++)
        {
            float fps = 1f / Time.deltaTime;
            current += fps;
            count++;
        }
        return Mathf.RoundToInt(current / count);
    }


}
