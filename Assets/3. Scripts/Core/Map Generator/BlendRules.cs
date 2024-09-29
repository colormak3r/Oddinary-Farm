using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum IBool
{
    Any,
    Yes,
    No,
}

public static class IBoolExtension
{
    public static string ToSymbol(this IBool ibool, bool debug = false)
    {
        if (debug)
        {
            switch (ibool)
            {
                case IBool.Any:
                    return "A";
                case IBool.Yes:
                    return "O";
                case IBool.No:
                    return "X";
                default:
                    return "A";
            }
        }
        else
        {
            switch (ibool)
            {
                case IBool.Any:
                    return " ";
                case IBool.Yes:
                    return "✔";
                case IBool.No:
                    return "✘";
                default:
                    return " ";
            }
        }

    }
}

[System.Serializable]
public class BlendUnit
{
    public Sprite Sprite;
    public IBool[] Neighbors;
}

[CreateAssetMenu(fileName = " Rules", menuName = "Scriptable Objects/Map Generator/Blend Rules")]
public class BlendRules : ScriptableObject
{
    public BlendUnit[] blendUnits;

    public Sprite GetMatchingSprite(IBool[] neighbors)
    {
        foreach (var unit in blendUnits)
        {
            /*var builder = "Neighbor Expected:\n";
            for (int i = 0; i < unit.Neighbors.Length; i++)
            {
                builder += unit.Neighbors[i].ToSymbol(true) + " ";
                if (i == 2 || i== 5) builder += "\n";
            }
            Debug.Log(builder);*/

            bool isMatch = true;
            for (int i = 0; i < neighbors.Length; i++)
            {
                var expected = unit.Neighbors[i];
                var actual = neighbors[i];

                if (expected == IBool.Any)
                    continue;

                if (expected != actual)
                {
                    isMatch = false;
                    break;
                }
            }

            if (isMatch)
            {                
                return unit.Sprite;
            }
        }

        return null;
    }
}
