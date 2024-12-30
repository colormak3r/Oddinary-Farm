using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct UpgradeStage
{
    [TextArea(3, 10)]
    public string prompt;
    public ulong cost;
    public Sprite sprite;
}

[CreateAssetMenu(fileName = "UpgradeStages", menuName = "Scriptable Objects/Upgrade Stages")]
public class UpgradeStages : ScriptableObject
{
    [SerializeField]
    private UpgradeStage[] stages;

    public UpgradeStage GetStage(int index)
    {
        return stages[index];
    }

    public int GetStageCount()
    {
        return stages.Length;
    }
}
