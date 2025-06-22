using TMPro;
using UnityEngine;

public class StatUI : UIBehaviour
{
    public static StatUI Main;

    private void Awake()
    {
        if (Main == null)
        {
            Main = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("Settings")]
    [SerializeField]
    private TMP_Text statText;

    public void UpdateStat(StatisticData data)
    {
        statText.text = string.Empty; // Clear previous stats
        var snapshot = data.ToSnapshot(0);
        foreach (var stat in snapshot.flatStats)
        {
            statText.text += $"{stat.Key}: {stat.Value}\n";
        }
    }
}
