[System.Serializable]
public struct MinMaxFloat
{
    public float min;
    public float max;
    public float value => UnityEngine.Random.Range(min, max);
}
