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

        statText.text = "No stat available yet!";
    }

    [Header("Settings")]
    [SerializeField]
    private TMP_Text statText;

    public void UpdateStat(StatisticData data)
    {
        statText.text = string.Empty; // Clear previous stats
        var snapshot = data.ToSnapshot(0);

        statText.text += $"Time Died: {data.timeDied}";
        string formattedTimeSinceLastDeath = $"{data.timeSinceLastDeath / 60}m {data.timeSinceLastDeath % 60}s";
        statText.text += $"\nTime Since Last Death: {formattedTimeSinceLastDeath}";
        statText.text += $"\nDistance Travelled: {data.distanceTravelled}";
        statText.text += $"\nGlobal Coin Collected: {data.globalCoinsCollected}";
        statText.text += $"\nPersonal Coin Collected: {data.personalCoinsCollected}";

        if (data.itemsUsed.Count > 0)
        {
            var mostUsed = data.itemsUsed.Max();
            statText.text += $"\nItems Most Used: {mostUsed.Key.ItemName} - {mostUsed.Value} used";
        }
        else
        {
            statText.text += "\nNo items used yet.";
        }

        if (data.itemsCollected.Count > 0)
        {
            var mostCollected = data.itemsCollected.Max();
            statText.text += $"\nItems Most Collected: {mostCollected.Key.ItemName} - {mostCollected.Value} collected";
        }
        else
        {
            statText.text += "\nNo items collected yet.";
        }

        if (data.entitiesKilled.Count > 0)
        {
            var mostKilled = data.entitiesKilled.Max();
            statText.text += $"\nEntity Most Killed: {mostKilled.Key} - {mostKilled.Value} killed";
        }
        else
        {
            statText.text += "\nNo entities killed yet.";
        }

        if (data.damageDealt.Count > 0)
        {
            var mostDamageDealt = data.damageDealt.Max();
            statText.text += $"\nDamage Most Dealt To: {mostDamageDealt.Key} - {mostDamageDealt.Value} damage";
        }
        else
        {
            statText.text += "\nNo damage dealt yet.";
        }

        if (data.damageTaken.Count > 0)
        {
            var mostDamageTaken = data.damageTaken.Max();
            statText.text += $"\nDamage Most Taken From: {mostDamageTaken.Key} - {mostDamageTaken.Value} damage";
        }
        else
        {
            statText.text += "\nNo damage taken yet.";
        }

        if (data.animalsCaptured.Count > 0)
        {
            var mostCaptured = data.animalsCaptured.Max();
            statText.text += $"\nAnimal Most Captured: {mostCaptured.Key} - {mostCaptured.Value} times";
        }
        else
        {
            statText.text += "\nNo animals captured yet.";
        }

        statText.text += "\n\n\n>> Raw Stat:\n";
        foreach (var stat in snapshot.flatStats)
        {
            statText.text += $"{stat.Key}: {stat.Value}\n";
        }
    }

    public void CloseButton()
    {
        Hide();
        PauseButtonUI.Main.Show();
        AudioManager.Main.PlayClickSound();
        InputManager.Main.SwitchMap(InputMap.Gameplay);
    }
}
