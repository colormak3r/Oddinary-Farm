[System.Serializable]
public struct MinMaxFloat
{
    public float min;
    public float max;
    public float value => UnityEngine.Random.Range(min, max);

    public MinMaxFloat(float min, float max)
    {
        this.min = min;
        this.max = max;
    }
}
