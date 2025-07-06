/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/05/2025 (Khoa)
 * Notes:           <write here>
*/

using TMPro;
using UnityEngine;
using System.Collections;

public class CountdownUI : UIBehaviour
{
    public static CountdownUI Main { get; private set; }

    private void Awake()
    {
        if (Main == null)
        {
            Main = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }


    [SerializeField]
    private TMP_Text countdownText;

    public IEnumerator CountdownRoutine(float time)
    {
        Show();

        while (time > 0)
        {
            countdownText.text = time.ToString("00.00");

            yield return null;
            time -= Time.deltaTime;
        }

        countdownText.text = "00.00";

        Hide();
    }
}
